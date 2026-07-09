using Microsoft.Data.SqlClient;
using System.Data;

internal static class OrdenTrabajoManualEndpoints
{
    public static WebApplication MapOrdenTrabajoManualEndpoints(this WebApplication app, string connectionString)
    {
        app.MapPost("/api/ordenes-trabajo/manual/validar", async (OtManualApiRequest request) =>
        {
            string? mensajeValidacion = ValidarRequest(request);
            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
                return Results.BadRequest(new { mensaje = mensajeValidacion });

            await using SqlConnection conexion = new(connectionString);
            await using SqlCommand cmd = new("USP_PRO_OT_MANUAL_VALIDAR_INSUMOS", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
            {
                TypeName = "dbo.TipoOTManualPlanificacion",
                Value = CrearTablaManual(request.Detalles)
            });

            List<object> productos = [];
            await conexion.OpenAsync();
            await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
            while (await dr.ReadAsync())
            {
                productos.Add(new
                {
                    idProducto = Convert.ToInt32(dr["IdProducto"]),
                    codigoProducto = LeerString(dr, "CodigoProducto"),
                    nombreProducto = LeerString(dr, "NombreProducto"),
                    observacion = LeerString(dr, "Observacion"),
                    cantidadRequerida = LeerDecimal(dr, "CantidadRequerida"),
                    idFichaTecnica = dr["IdFichaTecnica"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["IdFichaTecnica"]),
                    stockAlmacen = LeerDecimal(dr, "StockAlmacen"),
                    stockCorte = LeerDecimal(dr, "StockCorte"),
                    stockConfeccion = LeerDecimal(dr, "StockConfeccion"),
                    stockAcabado = LeerDecimal(dr, "StockAcabado"),
                    stockTotal = LeerDecimal(dr, "StockTotal"),
                    deficit = LeerDecimal(dr, "Deficit"),
                    estadoInsumos = LeerString(dr, "EstadoInsumos")
                });
            }

            return Results.Ok(new
            {
                puedeGenerar = true,
                mensaje = "Validacion informativa lista. Puede continuar para generar la OT Manual.",
                productos
            });
        });

        app.MapPost("/api/ordenes-trabajo/manual", async (OtManualApiRequest request) =>
        {
            string? mensajeValidacion = ValidarRequest(request);
            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
                return Results.BadRequest(new { mensaje = mensajeValidacion });

            string usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "Android" : request.Usuario.Trim();
            string observacion = request.Observacion?.Trim() ?? string.Empty;
            int idUsuario = request.IdUsuario;

            await using SqlConnection conexion = new(connectionString);
            await conexion.OpenAsync();

            if (idUsuario <= 0)
                idUsuario = await ObtenerIdUsuarioPorNombreAsync(conexion, usuario);

            if (idUsuario <= 0)
                return Results.BadRequest(new { mensaje = "No se pudo identificar al usuario de sesion." });

            await using SqlCommand cmd = new("USP_PRO_OT_MANUAL_CREAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
            cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = observacion;
            cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
            {
                TypeName = "dbo.TipoOTManualPlanificacion",
                Value = CrearTablaManual(request.Detalles)
            });

            SqlParameter idOt = new("@IdOrdenTrabajo", SqlDbType.Int) { Direction = ParameterDirection.Output };
            SqlParameter numeroOt = new("@NumeroOT", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(idOt);
            cmd.Parameters.Add(numeroOt);
            await cmd.ExecuteNonQueryAsync();

            string numero = numeroOt.Value?.ToString() ?? string.Empty;
            int idOrdenTrabajo = idOt.Value == DBNull.Value ? 0 : Convert.ToInt32(idOt.Value);
            if (idOrdenTrabajo > 0)
            {
                try { await AppNotificationStore.AddOtGeneradaAsync(conexion, idOrdenTrabajo, "MANUAL", usuario); } catch { }
            }

            return Results.Ok(new
            {
                mensaje = $"OT Manual {numero} generada correctamente por Abastecimiento de Stock.",
                idOrdenTrabajo,
                numeroOT = numero
            });
        });

        return app;
    }

    private static string? ValidarRequest(OtManualApiRequest request)
    {
        if (request.Detalles.Count == 0)
            return "Agregue al menos un producto.";

        if (request.Detalles.Any(x => x.IdProducto <= 0 || x.CantidadPlanificada <= 0))
            return "Todas las cantidades deben ser mayores que cero.";

        return null;
    }

    private static DataTable CrearTablaManual(IEnumerable<OtManualDetalleApiRequest> detalles)
    {
        DataTable tabla = new();
        tabla.Columns.Add("IdProducto", typeof(int));
        tabla.Columns.Add("CantidadPlanificada", typeof(decimal));

        foreach (OtManualDetalleApiRequest detalle in detalles)
            tabla.Rows.Add(detalle.IdProducto, detalle.CantidadPlanificada);

        return tabla;
    }

    private static async Task<int> ObtenerIdUsuarioPorNombreAsync(SqlConnection conexion, string usuario)
    {
        await using SqlCommand cmd = new("SELECT TOP (1) IdUsuario FROM dbo.Usuarios WHERE NombreUsuario = @Usuario AND Estado = 1", conexion);
        cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
        object? value = await cmd.ExecuteScalarAsync();
        return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
    }

    private static string LeerString(SqlDataReader dr, string columna) => dr[columna] == DBNull.Value ? string.Empty : dr[columna]?.ToString() ?? string.Empty;
    private static decimal LeerDecimal(SqlDataReader dr, string columna) => dr[columna] == DBNull.Value ? 0 : Convert.ToDecimal(dr[columna]);
}

internal sealed record OtManualApiRequest(
    int IdUsuario,
    string? Usuario,
    string? Motivo,
    string? Observacion,
    List<OtManualDetalleApiRequest> Detalles);

internal sealed record OtManualDetalleApiRequest(
    int IdProducto,
    decimal CantidadPlanificada);
