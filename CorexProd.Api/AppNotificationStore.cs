using Microsoft.Data.SqlClient;
using System.Data;

internal static class AppNotificationStore
{
    private static readonly object Sync = new();
    private static readonly List<AppNotificationApi> Items = [];
    private static long _lastId;

    public static async Task<IReadOnlyList<AppNotificationApi>> ListAsync(
        string connectionString,
        long desdeId,
        int take = 50)
    {
        await Task.CompletedTask;
        lock (Sync)
        {
            return Items
                .Where(x => x.IdNotificacion > desdeId)
                .OrderBy(x => x.IdNotificacion)
                .Take(Math.Clamp(take, 1, 100))
                .ToList();
        }
    }

    public static async Task AddOtGeneradaAsync(
        SqlConnection conexion,
        int idOrdenTrabajo,
        string origen,
        string usuario)
    {
        OtResumen resumen = await ObtenerOtResumenAsync(conexion, idOrdenTrabajo);
        string titulo = origen.Equals("REGULARIZACION", StringComparison.OrdinalIgnoreCase)
            ? $"OT regularizacion {resumen.NumeroOT}"
            : $"Nueva OT {resumen.NumeroOT}";
        string mensaje = $"Cliente: {resumen.Cliente}. OC: {Blank(resumen.OrdenCompraCliente)}. Tipo: {Blank(resumen.TipoOT)}. Cantidad: {resumen.TotalPlanificado:N2}. Usuario: {Blank(usuario)}.";

        await InsertAsync(conexion, "OT_NUEVA", titulo, mensaje, idOrdenTrabajo, resumen.NumeroOT);
    }

    public static async Task AddTransferenciaAsync(
        SqlConnection conexion,
        int idOrdenTrabajo,
        long idOperacion,
        bool esTerminacion)
    {
        MovimientoResumen resumen = esTerminacion
            ? await ObtenerTerminacionResumenAsync(conexion, idOrdenTrabajo, idOperacion)
            : await ObtenerTransferenciaResumenAsync(conexion, idOrdenTrabajo, idOperacion);

        string accion = esTerminacion ? "terminacion" : "transferencia";
        string titulo = esTerminacion ? $"OT {resumen.NumeroOT}: producto terminado" : $"OT {resumen.NumeroOT}: transferencia";
        string mensaje = $"Movimiento de {accion}. De {Blank(resumen.Origen)} a {Blank(resumen.Destino)}. Cantidad: {resumen.Cantidad:N2}. Productos: {Blank(resumen.Productos)}. Usuario: {Blank(resumen.Usuario)}.";

        await InsertAsync(conexion, esTerminacion ? "OT_TERMINACION" : "OT_TRANSFERENCIA", titulo, mensaje, idOrdenTrabajo, resumen.NumeroOT);
    }

    public static async Task AddMermaAsync(
        SqlConnection conexion,
        int idOrdenTrabajo,
        long idDetalleArea,
        decimal cantidad,
        string? motivo,
        string? observacion)
    {
        AreaProductoResumen resumen = await ObtenerAreaProductoResumenAsync(conexion, idOrdenTrabajo, idDetalleArea);
        string detalle = string.Join(" - ", new[] { motivo?.Trim(), observacion?.Trim() }.Where(x => !string.IsNullOrWhiteSpace(x)));
        string titulo = $"OT {resumen.NumeroOT}: merma registrada";
        string mensaje = $"Area: {Blank(resumen.Area)}. Producto: {Blank(resumen.Producto)}. Cantidad merma: {cantidad:N2}. Detalle: {Blank(detalle)}.";

        await InsertAsync(conexion, "OT_MERMA", titulo, mensaje, idOrdenTrabajo, resumen.NumeroOT);
    }

    public static async Task AddReservaAsync(
        SqlConnection conexion,
        int idOrdenTrabajo,
        long idDetalleArea,
        decimal cantidad,
        string? observacion)
    {
        AreaProductoResumen resumen = await ObtenerAreaProductoResumenAsync(conexion, idOrdenTrabajo, idDetalleArea);
        string titulo = $"OT {resumen.NumeroOT}: reserva de proceso";
        string mensaje = $"Area: {Blank(resumen.Area)}. Producto: {Blank(resumen.Producto)}. Cantidad reservada: {cantidad:N2}. Observacion: {Blank(observacion)}.";

        await InsertAsync(conexion, "OT_RESERVA", titulo, mensaje, idOrdenTrabajo, resumen.NumeroOT);
    }

    private static Task InsertAsync(
        SqlConnection conexion,
        string tipo,
        string titulo,
        string mensaje,
        int? idOrdenTrabajo,
        string numeroOT)
    {
        lock (Sync)
        {
            Items.Add(new AppNotificationApi(
                ++_lastId,
                tipo,
                Truncate(titulo, 120),
                Truncate(mensaje, 900),
                idOrdenTrabajo,
                numeroOT,
                DateTime.Now));

            if (Items.Count > 500)
                Items.RemoveRange(0, Items.Count - 500);
        }

        return Task.CompletedTask;
    }

    private static async Task<OtResumen> ObtenerOtResumenAsync(SqlConnection conexion, int idOrdenTrabajo)
    {
        await using SqlCommand cmd = new("USP_PRO_OT_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = idOrdenTrabajo;

        string numero = string.Empty;
        string cliente = string.Empty;
        string ocCliente = string.Empty;
        string tipo = string.Empty;
        decimal total = 0;

        await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
        if (await dr.ReadAsync())
        {
            numero = ReadString(dr, "NumeroOT");
            cliente = ReadString(dr, "NombreCliente");
            ocCliente = ReadString(dr, "OrdenCompraCliente");
            tipo = ReadString(dr, "TipoOT");
        }

        if (await dr.NextResultAsync())
        {
            while (await dr.ReadAsync())
                total += dr["CantidadPlanificada"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["CantidadPlanificada"]);
        }

        return new OtResumen(numero, cliente, ocCliente, tipo, total);
    }

    private static async Task<MovimientoResumen> ObtenerTransferenciaResumenAsync(SqlConnection conexion, int idOrdenTrabajo, long idOperacion)
    {
        const string sql = """
SELECT
    ot.NumeroOT,
    ao.NombreArea Origen,
    ad.NombreArea Destino,
    SUM(td.CantidadEnviada) Cantidad,
    STRING_AGG(CONCAT(d.CodigoProducto, ' - ', d.NombreProducto), '; ') Productos,
    ISNULL(ua.NombreUsuario, us.NombreUsuario) Usuario
FROM dbo.OrdenTrabajoTransferencia t
JOIN dbo.OrdenTrabajo ot ON ot.IdOrdenTrabajo = t.IdOrdenTrabajo
JOIN dbo.OrdenTrabajoTransferenciaDetalle td ON td.IdOperacionTransferencia = t.IdOperacionTransferencia
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT = td.IdDetalleOT
JOIN dbo.AreaProduccion ao ON ao.IdAreaProduccion = t.IdAreaOrigen
JOIN dbo.AreaProduccion ad ON ad.IdAreaProduccion = t.IdAreaDestino
JOIN dbo.Usuarios us ON us.IdUsuario = t.IdUsuarioSesion
LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario = t.IdUsuarioAutoriza
WHERE t.IdOrdenTrabajo = @IdOrdenTrabajo
  AND t.IdOperacionTransferencia = @IdOperacion
GROUP BY ot.NumeroOT, ao.NombreArea, ad.NombreArea, ISNULL(ua.NombreUsuario, us.NombreUsuario);
""";

        return await ObtenerMovimientoResumenSqlAsync(conexion, sql, idOrdenTrabajo, idOperacion);
    }

    private static async Task<MovimientoResumen> ObtenerTerminacionResumenAsync(SqlConnection conexion, int idOrdenTrabajo, long idOperacion)
    {
        const string sql = """
SELECT
    ot.NumeroOT,
    a.NombreArea Origen,
    'Producto terminado' Destino,
    SUM(td.Cantidad) Cantidad,
    STRING_AGG(CONCAT(d.CodigoProducto, ' - ', d.NombreProducto), '; ') Productos,
    ISNULL(ua.NombreUsuario, us.NombreUsuario) Usuario
FROM dbo.OrdenTrabajoTerminacion t
JOIN dbo.OrdenTrabajo ot ON ot.IdOrdenTrabajo = t.IdOrdenTrabajo
JOIN dbo.OrdenTrabajoTerminacionDetalle td ON td.IdOperacionTerminacion = t.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT = td.IdDetalleOT
JOIN dbo.AreaProduccion a ON a.IdAreaProduccion = t.IdAreaTermino
JOIN dbo.Usuarios us ON us.IdUsuario = t.IdUsuarioSesion
LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario = t.IdUsuarioAutoriza
WHERE t.IdOrdenTrabajo = @IdOrdenTrabajo
  AND t.IdOperacionTerminacion = @IdOperacion
GROUP BY ot.NumeroOT, a.NombreArea, ISNULL(ua.NombreUsuario, us.NombreUsuario);
""";

        return await ObtenerMovimientoResumenSqlAsync(conexion, sql, idOrdenTrabajo, idOperacion);
    }

    private static async Task<MovimientoResumen> ObtenerMovimientoResumenSqlAsync(
        SqlConnection conexion,
        string sql,
        int idOrdenTrabajo,
        long idOperacion)
    {
        await using SqlCommand cmd = new(sql, conexion);
        cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = idOrdenTrabajo;
        cmd.Parameters.Add("@IdOperacion", SqlDbType.BigInt).Value = idOperacion;

        await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
        if (await dr.ReadAsync())
        {
            return new MovimientoResumen(
                ReadString(dr, "NumeroOT"),
                ReadString(dr, "Origen"),
                ReadString(dr, "Destino"),
                dr["Cantidad"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Cantidad"]),
                ReadString(dr, "Productos"),
                ReadString(dr, "Usuario"));
        }

        return new MovimientoResumen(string.Empty, string.Empty, string.Empty, 0, string.Empty, string.Empty);
    }

    private static async Task<AreaProductoResumen> ObtenerAreaProductoResumenAsync(SqlConnection conexion, int idOrdenTrabajo, long idDetalleArea)
    {
        const string sql = """
SELECT TOP (1)
    ot.NumeroOT,
    a.NombreArea,
    CONCAT(d.CodigoProducto, ' - ', d.NombreProducto) Producto
FROM dbo.OrdenTrabajoDetalleArea da
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT = da.IdDetalleOT
JOIN dbo.OrdenTrabajo ot ON ot.IdOrdenTrabajo = d.IdOrdenTrabajo
JOIN dbo.AreaProduccion a ON a.IdAreaProduccion = da.IdAreaProduccion
WHERE da.IdDetalleArea = @IdDetalleArea
  AND ot.IdOrdenTrabajo = @IdOrdenTrabajo;
""";

        await using SqlCommand cmd = new(sql, conexion);
        cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = idOrdenTrabajo;
        cmd.Parameters.Add("@IdDetalleArea", SqlDbType.BigInt).Value = idDetalleArea;

        await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
        if (await dr.ReadAsync())
            return new AreaProductoResumen(ReadString(dr, "NumeroOT"), ReadString(dr, "NombreArea"), ReadString(dr, "Producto"));

        return new AreaProductoResumen(string.Empty, string.Empty, string.Empty);
    }

    private static string ReadString(SqlDataReader dr, string column)
        => dr[column] == DBNull.Value ? string.Empty : dr[column]?.ToString() ?? string.Empty;

    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private sealed record OtResumen(string NumeroOT, string Cliente, string OrdenCompraCliente, string TipoOT, decimal TotalPlanificado);
    private sealed record MovimientoResumen(string NumeroOT, string Origen, string Destino, decimal Cantidad, string Productos, string Usuario);
    private sealed record AreaProductoResumen(string NumeroOT, string Area, string Producto);
}

internal sealed record AppNotificationApi(
    long IdNotificacion,
    string Tipo,
    string Titulo,
    string Mensaje,
    int? IdOrdenTrabajo,
    string NumeroOT,
    DateTime FechaRegistro);
