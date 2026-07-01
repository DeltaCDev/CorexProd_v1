using CorexProd.Entidad.Utilidades;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("RedLocal", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

WebApplication app = builder.Build();
app.UseCors("RedLocal");

string connectionString = builder.Configuration.GetConnectionString("CorexProdDB")
    ?? throw new InvalidOperationException("No se configuró ConnectionStrings:CorexProdDB.");
string rutaFichas = builder.Configuration["FichasTecnicas:RutaBase"]
    ?? throw new InvalidOperationException("No se configuró FichasTecnicas:RutaBase.");

app.MapGet("/", () => Results.Ok(new
{
    servicio = "CorexProd API",
    estado = "Activo",
    fechaHora = DateTime.Now,
    endpoints = new[]
    {
        "/api/health",
        "/api/auth/login",
        "/api/stock/productos",
        "/api/stock/insumos",
        "/api/fichas-tecnicas/{codigoProducto}/info",
        "/api/fichas-tecnicas/{codigoProducto}"
    }
}));

app.MapPost("/api/auth/login", async (LoginRequest request) =>
{
    string nombreUsuario = request.Usuario?.Trim() ?? string.Empty;
    string clave = request.Clave?.Trim() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(clave))
    {
        return Results.BadRequest(new { mensaje = "Ingrese usuario y contraseña." });
    }

    const string sqlUsuario = "EXEC dbo.USP_SEG_USUARIO_LOGIN @Usuario;";

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sqlUsuario, conexion);
    cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 50).Value = nombreUsuario;

    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
    {
        return Results.Unauthorized();
    }

    string hash = dr["Clave"]?.ToString() ?? string.Empty;
    bool activo = Convert.ToBoolean(dr["Estado"]);
    if (!activo || !BCrypt.Net.BCrypt.Verify(clave, hash))
    {
        return Results.Unauthorized();
    }

    LoginUserResponse usuario = new(
        Convert.ToInt32(dr["IdUsuario"]),
        dr["NombreUsuario"]?.ToString() ?? string.Empty,
        dr["NombreCompleto"]?.ToString() ?? string.Empty,
        Convert.ToInt32(dr["IdRol"]),
        dr["NombreRol"]?.ToString() ?? string.Empty);

    await dr.CloseAsync();

    List<string> menus = [];
    await using SqlCommand menusCmd = new("dbo.USP_SEG_MENU_OBTENERPORROL", conexion)
    {
        CommandType = CommandType.StoredProcedure
    };
    menusCmd.Parameters.Add("@IdRol", SqlDbType.Int).Value = usuario.IdRol;

    await using SqlDataReader menusReader = await menusCmd.ExecuteReaderAsync();
    while (await menusReader.ReadAsync())
    {
        menus.Add(menusReader["NombreMenu"]?.ToString() ?? string.Empty);
    }

    return Results.Ok(new LoginResponse(
        usuario,
        menus,
        DateTime.Now,
        "Inicio de sesión correcto."));
});

app.MapGet("/api/health", async () =>
{
    try
    {
        await using SqlConnection conexion = new(connectionString);
        await conexion.OpenAsync();
        return Results.Ok(new
        {
            estado = "OK",
            baseDatos = conexion.Database,
            servidor = conexion.DataSource,
            fechaHora = DateTime.Now
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "No se pudo conectar con CorexProdDB",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/empresa/actual", async () =>
{
    const string sql = @"
SELECT TOP (1)
    IdEmpresa,
    Ruc,
    Nombre,
    Telefono,
    Correo,
    Direccion,
    Logo
FROM dbo.Empresas
WHERE Estado = 1
ORDER BY EsPredeterminada DESC, IdEmpresa ASC;";

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
    {
        return Results.Ok(new
        {
            idEmpresa = 0,
            ruc = string.Empty,
            nombre = "CorexProd",
            telefono = string.Empty,
            correo = string.Empty,
            direccion = string.Empty,
            logoBase64 = string.Empty
        });
    }

    byte[]? logo = dr["Logo"] == DBNull.Value ? null : (byte[])dr["Logo"];
    return Results.Ok(new
    {
        idEmpresa = Convert.ToInt32(dr["IdEmpresa"]),
        ruc = dr["Ruc"]?.ToString() ?? string.Empty,
        nombre = dr["Nombre"]?.ToString() ?? string.Empty,
        telefono = dr["Telefono"]?.ToString() ?? string.Empty,
        correo = dr["Correo"]?.ToString() ?? string.Empty,
        direccion = dr["Direccion"]?.ToString() ?? string.Empty,
        logoBase64 = logo == null || logo.Length == 0 ? string.Empty : Convert.ToBase64String(logo)
    });
});

app.MapGet("/api/stock/productos", async (string? buscar, string? etiqueta) =>
{
    const string sql = @"
SELECT
    P.IdProducto,
    P.Codigo,
    P.NombreProducto,
    ISNULL(P.EtiquetaCliente, '') AS EtiquetaCliente,
    ISNULL(CP.NombreCategoria, '') AS Categoria,
    CAST(ISNULL(SP.StockActual, 0) AS DECIMAL(18,3)) AS StockActual
FROM dbo.Productos P
LEFT JOIN dbo.CategoriasProducto CP
    ON CP.IdCategoriaProducto = P.IdCategoriaProducto
LEFT JOIN dbo.StockProductos SP
    ON SP.IdProducto = P.IdProducto
WHERE P.Estado = 1
  AND
  (
      @Buscar = ''
      OR P.Codigo LIKE '%' + @Buscar + '%'
      OR P.NombreProducto LIKE '%' + @Buscar + '%'
      OR ISNULL(P.EtiquetaCliente, '') LIKE '%' + @Buscar + '%'
  )
  AND
  (
      @Etiqueta = ''
      OR P.Codigo LIKE '%' + @Etiqueta + '%'
      OR P.NombreProducto LIKE '%' + @Etiqueta + '%'
      OR ISNULL(P.EtiquetaCliente, '') LIKE '%' + @Etiqueta + '%'
  )
ORDER BY P.Codigo;";

    List<object> productos = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    cmd.Parameters.Add("@Buscar", SqlDbType.VarChar, 150).Value = buscar?.Trim() ?? string.Empty;
    cmd.Parameters.Add("@Etiqueta", SqlDbType.VarChar, 150).Value = etiqueta?.Trim() ?? string.Empty;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        string codigo = dr["Codigo"]?.ToString() ?? string.Empty;
        productos.Add(new
        {
            idProducto = Convert.ToInt32(dr["IdProducto"]),
            codigo,
            codigoModelo = CodigoModeloProducto.Obtener(codigo),
            producto = dr["NombreProducto"]?.ToString() ?? string.Empty,
            etiquetaCliente = dr["EtiquetaCliente"]?.ToString() ?? string.Empty,
            categoria = dr["Categoria"]?.ToString() ?? string.Empty,
            stockActual = Convert.ToDecimal(dr["StockActual"])
        });
    }

    return Results.Ok(new { total = productos.Count, items = productos });
});

app.MapGet("/api/stock/insumos", async (string? buscar) =>
{
    const string sql = @"
SELECT
    I.IdInsumo,
    I.Codigo,
    I.NombreInsumo,
    ISNULL(CI.NombreCategoria, '') AS Categoria,
    ISNULL(UM.Abreviatura, '') AS Unidad,
    CAST(ISNULL(SI.StockActual, 0) AS DECIMAL(18,3)) AS StockActual
FROM dbo.Insumos I
LEFT JOIN dbo.CategoriasInsumo CI
    ON CI.IdCategoriaInsumo = I.IdCategoriaInsumo
LEFT JOIN dbo.UnidadesMedida UM
    ON UM.IdUnidadMedida = I.IdUnidadMedida
LEFT JOIN dbo.StockInsumos SI
    ON SI.IdInsumo = I.IdInsumo
WHERE I.Estado = 1
  AND
  (
      @Buscar = ''
      OR I.Codigo LIKE '%' + @Buscar + '%'
      OR I.NombreInsumo LIKE '%' + @Buscar + '%'
  )
ORDER BY I.Codigo;";

    List<object> insumos = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    cmd.Parameters.Add("@Buscar", SqlDbType.VarChar, 150).Value = buscar?.Trim() ?? string.Empty;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        insumos.Add(new
        {
            idInsumo = Convert.ToInt32(dr["IdInsumo"]),
            codigo = dr["Codigo"]?.ToString() ?? string.Empty,
            insumo = dr["NombreInsumo"]?.ToString() ?? string.Empty,
            categoria = dr["Categoria"]?.ToString() ?? string.Empty,
            unidad = dr["Unidad"]?.ToString() ?? string.Empty,
            stockActual = Convert.ToDecimal(dr["StockActual"])
        });
    }

    return Results.Ok(new { total = insumos.Count, items = insumos });
});

app.MapGet("/api/proformas", async (string? buscar) =>
{
    const string sql = @"
SELECT
    P.IdProforma,
    P.SerieNumero,
    P.FechaEmision,
    P.FechaVencimiento,
    P.OrdenCompraCliente,
    C.NombreRazonSocial AS NombreCliente,
    P.Total,
    P.Estado,
    P.TieneOrdenCompraInterna
FROM dbo.Proformas P
INNER JOIN dbo.Clientes C ON C.IdCliente = P.IdCliente
WHERE @Buscar = ''
   OR P.SerieNumero LIKE '%' + @Buscar + '%'
   OR ISNULL(P.OrdenCompraCliente, '') LIKE '%' + @Buscar + '%'
   OR C.NombreRazonSocial LIKE '%' + @Buscar + '%'
ORDER BY P.FechaEmision DESC, P.IdProforma DESC;";

    List<object> items = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    cmd.Parameters.Add("@Buscar", SqlDbType.VarChar, 150).Value = buscar?.Trim() ?? string.Empty;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        items.Add(new
        {
            idProforma = Convert.ToInt32(dr["IdProforma"]),
            serieNumero = dr["SerieNumero"]?.ToString() ?? string.Empty,
            fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
            fechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]),
            ordenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
            nombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
            total = Convert.ToDecimal(dr["Total"]),
            estado = dr["Estado"]?.ToString() ?? string.Empty,
            tieneOrdenCompraInterna = Convert.ToBoolean(dr["TieneOrdenCompraInterna"])
        });
    }

    return Results.Ok(new { total = items.Count, items });
});

app.MapGet("/api/proformas/{id:int}", async (int id) =>
{
    const string cabeceraSql = @"
SELECT TOP (1)
    P.IdProforma,
    P.SerieNumero,
    P.FechaEmision,
    P.FechaVencimiento,
    P.OrdenCompraCliente,
    C.NombreRazonSocial AS NombreCliente,
    P.Observacion,
    P.Subtotal,
    P.Descuento,
    P.Igv,
    P.Total,
    P.Estado
FROM dbo.Proformas P
INNER JOIN dbo.Clientes C ON C.IdCliente = P.IdCliente
WHERE P.IdProforma = @IdProforma;";

    const string detalleSql = @"
SELECT
    PR.Codigo AS CodigoProducto,
    PR.NombreProducto,
    D.Cantidad,
    D.PrecioUnitario,
    D.Descuento,
    D.Importe,
    D.Observacion
FROM dbo.ProformaDetalle D
INNER JOIN dbo.Productos PR ON PR.IdProducto = D.IdProducto
WHERE D.IdProforma = @IdProforma
ORDER BY D.IdProformaDetalle;";

    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();
    await using SqlCommand cabeceraCmd = new(cabeceraSql, conexion);
    cabeceraCmd.Parameters.Add("@IdProforma", SqlDbType.Int).Value = id;
    await using SqlDataReader dr = await cabeceraCmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return Results.NotFound(new { mensaje = "Proforma no encontrada." });

    var cabecera = new
    {
        idProforma = Convert.ToInt32(dr["IdProforma"]),
        serieNumero = dr["SerieNumero"]?.ToString() ?? string.Empty,
        fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
        fechaVencimiento = Convert.ToDateTime(dr["FechaVencimiento"]),
        ordenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
        nombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
        observacion = dr["Observacion"]?.ToString() ?? string.Empty,
        subtotal = Convert.ToDecimal(dr["Subtotal"]),
        descuento = Convert.ToDecimal(dr["Descuento"]),
        igv = Convert.ToDecimal(dr["Igv"]),
        total = Convert.ToDecimal(dr["Total"]),
        estado = dr["Estado"]?.ToString() ?? string.Empty
    };
    await dr.CloseAsync();

    List<object> detalles = [];
    await using SqlCommand detalleCmd = new(detalleSql, conexion);
    detalleCmd.Parameters.Add("@IdProforma", SqlDbType.Int).Value = id;
    await using SqlDataReader detalleReader = await detalleCmd.ExecuteReaderAsync();
    while (await detalleReader.ReadAsync())
    {
        detalles.Add(new
        {
            codigoProducto = detalleReader["CodigoProducto"]?.ToString() ?? string.Empty,
            nombreProducto = detalleReader["NombreProducto"]?.ToString() ?? string.Empty,
            cantidad = Convert.ToDecimal(detalleReader["Cantidad"]),
            precioUnitario = Convert.ToDecimal(detalleReader["PrecioUnitario"]),
            descuento = Convert.ToDecimal(detalleReader["Descuento"]),
            importe = Convert.ToDecimal(detalleReader["Importe"]),
            observacion = detalleReader["Observacion"]?.ToString() ?? string.Empty
        });
    }

    return Results.Ok(new { cabecera, detalles });
});

app.MapGet("/api/proformas/preparar", async () =>
{
    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();

    List<object> clientes = [];
    await using (SqlCommand cmd = new(@"
SELECT IdCliente, NumeroDocumento, NombreRazonSocial
FROM dbo.Clientes
WHERE Estado = 1
ORDER BY NombreRazonSocial;", conexion))
    await using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
    {
        while (await dr.ReadAsync())
        {
            clientes.Add(new
            {
                idCliente = Convert.ToInt32(dr["IdCliente"]),
                numeroDocumento = dr["NumeroDocumento"]?.ToString() ?? string.Empty,
                nombreRazonSocial = dr["NombreRazonSocial"]?.ToString() ?? string.Empty
            });
        }
    }

    List<object> productos = [];
    await using (SqlCommand cmd = new(@"
SELECT IdProducto, Codigo, NombreProducto, ISNULL(EtiquetaCliente, '') AS EtiquetaCliente, IdUnidadMedida, '' AS NombreUnidad
FROM dbo.Productos
WHERE Estado = 1
ORDER BY Codigo, NombreProducto;", conexion))
    await using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
    {
        while (await dr.ReadAsync())
        {
            productos.Add(new
            {
                idProducto = Convert.ToInt32(dr["IdProducto"]),
                codigo = dr["Codigo"]?.ToString() ?? string.Empty,
                nombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                etiquetaCliente = dr["EtiquetaCliente"]?.ToString() ?? string.Empty,
                idUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                nombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty
            });
        }
    }

    string siguienteNumero = "Sin serie configurada";
    await using (SqlCommand cmd = new(@"
SELECT TOP (1) Serie, UltimoCorrelativo, CantidadDigitos
FROM dbo.SeriesCorrelativos
WHERE CodigoTipoDocumento = 'PROFORMA' AND Activa = 1
ORDER BY Predeterminada DESC, IdSerieCorrelativo;", conexion))
    await using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
    {
        if (await dr.ReadAsync())
        {
            string serie = dr["Serie"]?.ToString() ?? string.Empty;
            long correlativo = Convert.ToInt64(dr["UltimoCorrelativo"]) + 1;
            int digitos = Convert.ToInt32(dr["CantidadDigitos"]);
            siguienteNumero = $"{serie}-{correlativo.ToString().PadLeft(digitos, '0')}";
        }
    }

    return Results.Ok(new { siguienteNumero, clientes, productos });
});

app.MapPost("/api/proformas", async (ProformaGuardarApiRequest request) =>
{
    if (request.IdCliente <= 0)
        return Results.BadRequest(new { mensaje = "Seleccione un cliente." });

    if (request.Detalles.Count == 0 || request.Detalles.Any(x => x.IdProducto <= 0 || x.Cantidad <= 0))
        return Results.BadRequest(new { mensaje = "Agregue productos con cantidad mayor a cero." });

    decimal subtotal = request.Detalles.Sum(x => Math.Round((x.Cantidad * x.PrecioUnitario) - x.Descuento, 2));
    if (subtotal < 0)
        subtotal = 0;

    decimal igvPorcentaje = request.IgvPorcentaje <= 0 ? 18 : request.IgvPorcentaje;
    decimal descuento = 0;
    decimal igv = request.CondicionTributaria.Equals("INAFECTO", StringComparison.OrdinalIgnoreCase)
        ? 0
        : Math.Round(subtotal * (igvPorcentaje / 100), 2);
    decimal total = subtotal + igv;

    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();
    await ConfigurarOpcionesInsertAsync(conexion);

    await using SqlCommand cmd = new("USP_VEN_PROFORMA_GUARDAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdProforma", SqlDbType.Int).Value = 0;
    cmd.Parameters.Add("@FechaEmision", SqlDbType.Date).Value = DateTime.Today;
    cmd.Parameters.Add("@FechaVencimiento", SqlDbType.Date).Value = request.FechaVencimiento.Date;
    cmd.Parameters.Add("@OrdenCompraCliente", SqlDbType.VarChar, 80).Value = request.OrdenCompraCliente?.Trim() ?? string.Empty;
    cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = request.IdCliente;
    cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = request.Observacion?.Trim() ?? string.Empty;
    cmd.Parameters.Add("@Subtotal", SqlDbType.Decimal).Value = subtotal;
    cmd.Parameters.Add("@Descuento", SqlDbType.Decimal).Value = descuento;
    cmd.Parameters.Add("@Igv", SqlDbType.Decimal).Value = igv;
    cmd.Parameters.Add("@IgvPorcentaje", SqlDbType.Decimal).Value = igvPorcentaje;
    cmd.Parameters.Add("@CondicionTributaria", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(request.CondicionTributaria) ? "GRAVADO" : request.CondicionTributaria.Trim().ToUpperInvariant();
    cmd.Parameters.Add("@Total", SqlDbType.Decimal).Value = total;
    cmd.Parameters.Add("@DetallesXml", SqlDbType.Xml).Value = CrearDetallesProformaXml(request.Detalles);
    cmd.Parameters.Add("@UsuarioGenerador", SqlDbType.VarChar, 80).Value = string.IsNullOrWhiteSpace(request.Usuario) ? "Android" : request.Usuario.Trim();

    SqlParameter idGenerado = new("@IdGenerado", SqlDbType.Int) { Direction = ParameterDirection.Output };
    SqlParameter serieNumero = new("@SerieNumero", SqlDbType.VarChar, 40) { Direction = ParameterDirection.Output };
    SqlParameter resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
    SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(idGenerado);
    cmd.Parameters.Add(serieNumero);
    cmd.Parameters.Add(resultado);
    cmd.Parameters.Add(mensajeParam);
    await cmd.ExecuteNonQueryAsync();

    string mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
    if (resultado.Value is bool ok && !ok)
        return Results.BadRequest(new { mensaje });

    return Results.Ok(new
    {
        mensaje,
        idProforma = idGenerado.Value == DBNull.Value ? 0 : Convert.ToInt32(idGenerado.Value),
        serieNumero = serieNumero.Value?.ToString() ?? string.Empty,
        subtotal,
        igv,
        total
    });
});

app.MapPost("/api/proformas/{id:int}/generar-oci", async (int id, DocumentoAccionApiRequest request) =>
{
    string usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "Android" : request.Usuario.Trim();
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_OCI_GENERAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdProforma", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@UsuarioGenerador", SqlDbType.VarChar, 80).Value = usuario;
    cmd.Parameters.Add(new SqlParameter("@IdGenerado", SqlDbType.Int) { Direction = ParameterDirection.Output });
    cmd.Parameters.Add(new SqlParameter("@NumeroOci", SqlDbType.VarChar, 40) { Direction = ParameterDirection.Output });
    SqlParameter resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
    SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(resultado);
    cmd.Parameters.Add(mensajeParam);
    await conexion.OpenAsync();
    await cmd.ExecuteNonQueryAsync();
    string mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
    return resultado.Value is bool ok && !ok ? Results.BadRequest(new { mensaje }) : Results.Ok(new { mensaje });
});

app.MapPost("/api/proformas/{id:int}/anular", async (int id, DocumentoAccionApiRequest request) =>
{
    string usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "Android" : request.Usuario.Trim();
    string motivo = string.IsNullOrWhiteSpace(request.Motivo) ? "Anulado desde Android" : request.Motivo.Trim();
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_PROFORMA_ANULAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdProforma", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@MotivoAnulacion", SqlDbType.VarChar, 200).Value = motivo;
    cmd.Parameters.Add("@UsuarioAnulacion", SqlDbType.VarChar, 80).Value = usuario;
    SqlParameter resultado = new("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
    SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(resultado);
    cmd.Parameters.Add(mensajeParam);
    await conexion.OpenAsync();
    await cmd.ExecuteNonQueryAsync();
    string mensaje = mensajeParam.Value?.ToString() ?? string.Empty;
    return resultado.Value is bool ok && !ok ? Results.BadRequest(new { mensaje }) : Results.Ok(new { mensaje });
});

app.MapGet("/api/oci", async (string? buscar) =>
{
    const string sql = @"
SELECT
    O.IdOrdenCompraInterna,
    O.NumeroOci,
    P.SerieNumero AS NumeroProforma,
    O.FechaEmision,
    O.OrdenCompraCliente,
    O.NombreCliente,
    O.Total,
    O.Estado,
    O.TieneGuiaSalida,
    O.TieneOrdenTrabajo,
    CAST(CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.OrdenTrabajo OT
        WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
          AND OT.Estado IN ('PENDIENTE', 'EMITIDA', 'EN_PROCESO', 'PARCIAL')
    ) THEN 1 ELSE 0 END AS BIT) AS TieneOtActiva
FROM dbo.OrdenesCompraInterna O
INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
WHERE @Buscar = ''
   OR O.NumeroOci LIKE '%' + @Buscar + '%'
   OR P.SerieNumero LIKE '%' + @Buscar + '%'
   OR ISNULL(O.OrdenCompraCliente, '') LIKE '%' + @Buscar + '%'
   OR O.NombreCliente LIKE '%' + @Buscar + '%'
ORDER BY O.FechaEmision DESC, O.IdOrdenCompraInterna DESC;";

    List<object> items = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    cmd.Parameters.Add("@Buscar", SqlDbType.VarChar, 150).Value = buscar?.Trim() ?? string.Empty;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        items.Add(new
        {
            idOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
            numeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
            numeroProforma = dr["NumeroProforma"]?.ToString() ?? string.Empty,
            fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
            ordenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
            nombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
            total = Convert.ToDecimal(dr["Total"]),
            estado = dr["Estado"]?.ToString() ?? string.Empty,
            tieneGuiaSalida = Convert.ToBoolean(dr["TieneGuiaSalida"]),
            tieneOrdenTrabajo = Convert.ToBoolean(dr["TieneOrdenTrabajo"]),
            tieneOtActiva = Convert.ToBoolean(dr["TieneOtActiva"])
        });
    }

    return Results.Ok(new { total = items.Count, items });
});

app.MapGet("/api/oci/{id:int}", async (int id) =>
{
    const string cabeceraSql = @"
SELECT TOP (1)
    O.IdOrdenCompraInterna,
    O.NumeroOci,
    P.SerieNumero AS NumeroProforma,
    O.FechaEmision,
    O.OrdenCompraCliente,
    O.NombreCliente,
    O.Subtotal,
    O.Descuento,
    O.Igv,
    O.Total,
    O.Estado
FROM dbo.OrdenesCompraInterna O
INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;";

    const string detalleSql = @"
SELECT
    D.IdOrdenCompraInternaDetalle,
    D.IdProducto,
    D.CodigoProducto,
    D.NombreProducto,
    D.Cantidad,
    CAST(ISNULL(S.StockActual, 0) AS DECIMAL(18,2)) AS StockActual,
    D.CantidadDespachada,
    D.PrecioUnitario,
    D.Descuento,
    D.Importe,
    D.Observacion
FROM dbo.OrdenCompraInternaDetalle D
LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
ORDER BY D.IdOrdenCompraInternaDetalle;";

    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();
    await using SqlCommand cabeceraCmd = new(cabeceraSql, conexion);
    cabeceraCmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
    await using SqlDataReader dr = await cabeceraCmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return Results.NotFound(new { mensaje = "OCI no encontrada." });

    var cabecera = new
    {
        idOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
        numeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
        numeroProforma = LeerString(dr, "NumeroProforma"),
        fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
        ordenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
        nombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
        subtotal = Convert.ToDecimal(dr["Subtotal"]),
        descuento = Convert.ToDecimal(dr["Descuento"]),
        igv = Convert.ToDecimal(dr["Igv"]),
        total = Convert.ToDecimal(dr["Total"]),
        estado = dr["Estado"]?.ToString() ?? string.Empty
    };
    await dr.CloseAsync();

    List<object> detalles = [];
    await using SqlCommand detalleCmd = new(detalleSql, conexion);
    detalleCmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
    await using SqlDataReader detalleReader = await detalleCmd.ExecuteReaderAsync();
    while (await detalleReader.ReadAsync())
    {
        detalles.Add(new
        {
            idOrdenCompraInternaDetalle = Convert.ToInt32(detalleReader["IdOrdenCompraInternaDetalle"]),
            idProducto = Convert.ToInt32(detalleReader["IdProducto"]),
            codigoProducto = detalleReader["CodigoProducto"]?.ToString() ?? string.Empty,
            nombreProducto = detalleReader["NombreProducto"]?.ToString() ?? string.Empty,
            cantidad = Convert.ToDecimal(detalleReader["Cantidad"]),
            stockActual = Convert.ToDecimal(detalleReader["StockActual"]),
            cantidadDespachada = Convert.ToDecimal(detalleReader["CantidadDespachada"]),
            precioUnitario = Convert.ToDecimal(detalleReader["PrecioUnitario"]),
            descuento = Convert.ToDecimal(detalleReader["Descuento"]),
            importe = Convert.ToDecimal(detalleReader["Importe"]),
            observacion = detalleReader["Observacion"]?.ToString() ?? string.Empty
        });
    }

    return Results.Ok(new { cabecera, detalles });
});

app.MapPost("/api/oci/{id:int}/generar-ot", async (int id, DocumentoAccionApiRequest request) =>
{
    string usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "Android" : request.Usuario.Trim();
    string observacion = string.IsNullOrWhiteSpace(request.Motivo) ? "OT generada desde Android" : request.Motivo.Trim();

    const string validarSql = @"
SELECT TOP (1) Estado
FROM dbo.OrdenesCompraInterna
WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;";

    const string detalleSql = @"
SELECT
    D.IdOrdenCompraInternaDetalle,
    CAST(D.Cantidad - CASE
        WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada
        ELSE ISNULL(PROD.CantidadAplicada, 0)
    END AS DECIMAL(18,2)) AS CantidadPendiente
FROM dbo.OrdenCompraInternaDetalle D
OUTER APPLY
(
    SELECT SUM(OD.CantidadAplicada) AS CantidadAplicada
    FROM dbo.OrdenTrabajoDetalle OD
    INNER JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
    WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
      AND OT.Estado <> 'ANULADA'
      AND OD.Estado <> 'ANULADO'
) PROD
WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
  AND CAST(D.Cantidad - CASE
        WHEN D.CantidadDespachada > ISNULL(PROD.CantidadAplicada, 0) THEN D.CantidadDespachada
        ELSE ISNULL(PROD.CantidadAplicada, 0)
    END AS DECIMAL(18,2)) > 0;";

    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();

    await using (SqlCommand validarCmd = new(validarSql, conexion))
    {
        validarCmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
        await using SqlDataReader dr = await validarCmd.ExecuteReaderAsync();
        if (!await dr.ReadAsync())
            return Results.NotFound(new { mensaje = "OCI no encontrada." });

        string estado = dr["Estado"]?.ToString() ?? string.Empty;
        if (estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { mensaje = "No se puede generar OT de una OCI anulada." });
    }

    List<OrdenTrabajoPlanificacionApiRequest> detalles = [];
    await using (SqlCommand detalleCmd = new(detalleSql, conexion))
    {
        detalleCmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
        await using SqlDataReader dr = await detalleCmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            detalles.Add(new(
                Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
                Convert.ToDecimal(dr["CantidadPendiente"])));
        }
    }

    if (detalles.Count == 0)
        return Results.BadRequest(new { mensaje = "La OCI no tiene cantidades pendientes para producir." });

    List<OtValidacionProductoApi> validaciones = await ValidarOtInsumosAsync(conexion, id);
    if (validaciones.Count == 0)
        return Results.BadRequest(new { mensaje = "La OCI no tiene productos faltantes para producir." });
    if (validaciones.Any(x => !OtValidacionOk(x)))
        return Results.BadRequest(new { mensaje = "No se puede generar la OT. Revise ficha tecnica y stock de insumos antes de confirmar." });

    int idUsuario;
    await using (SqlCommand usuarioCmd = new("SELECT TOP (1) IdUsuario FROM dbo.Usuarios WHERE NombreUsuario = @Usuario", conexion))
    {
        usuarioCmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = usuario;
        object? idUsuarioObj = await usuarioCmd.ExecuteScalarAsync();
        idUsuario = idUsuarioObj == null || idUsuarioObj == DBNull.Value ? 0 : Convert.ToInt32(idUsuarioObj);
    }

    await using SqlCommand cmd = new("USP_PRO_OT_CREAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
    cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = observacion;
    cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
    {
        TypeName = "dbo.TipoOTPlanificacion",
        Value = CrearTablaPlanificacion(detalles)
    });
    SqlParameter idOt = new("@IdOrdenTrabajo", SqlDbType.Int) { Direction = ParameterDirection.Output };
    SqlParameter numeroOt = new("@NumeroOT", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(idOt);
    cmd.Parameters.Add(numeroOt);
    await cmd.ExecuteNonQueryAsync();

    return Results.Ok(new
    {
        mensaje = $"OT {numeroOt.Value} generada correctamente.",
        idOrdenTrabajo = Convert.ToInt32(idOt.Value),
        numeroOT = numeroOt.Value?.ToString() ?? string.Empty
    });
});

app.MapGet("/api/oci/{id:int}/orden-trabajo/validacion", async (int id) =>
{
    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();

    List<OtValidacionProductoApi> productos = await ValidarOtInsumosAsync(conexion, id);
    if (productos.Count == 0)
        return Results.Ok(new { puedeGenerar = false, mensaje = "No hay productos faltantes para producir.", productos });

    bool puedeGenerar = productos.All(OtValidacionOk);
    string mensaje = puedeGenerar
        ? "Todos los productos tienen ficha tecnica y stock de insumos suficiente."
        : "Hay productos sin ficha tecnica o con insumos insuficientes.";

    return Results.Ok(new { puedeGenerar, mensaje, productos });
});

app.MapGet("/api/oci/detalles/{idDetalle:int}/orden-trabajo/insumos", async (int idDetalle) =>
{
    List<OtValidacionInsumoApi> insumos = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_PRO_OT_DETALLE_INSUMOS", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenCompraInternaDetalle", SqlDbType.Int).Value = idDetalle;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        insumos.Add(new OtValidacionInsumoApi(
            Convert.ToInt32(dr["IdInsumo"]),
            LeerString(dr, "CodigoInsumo"),
            LeerString(dr, "NombreInsumo"),
            LeerString(dr, "UnidadMedida"),
            Convert.ToDecimal(dr["ConsumoUnitario"]),
            Convert.ToDecimal(dr["CantidadProduccion"]),
            Convert.ToDecimal(dr["CantidadNecesaria"]),
            Convert.ToDecimal(dr["StockActual"]),
            Convert.ToDecimal(dr["StockProyectado"]),
            Convert.ToDecimal(dr["CantidadFaltante"]),
            LeerString(dr, "Estado")));
    }

    return Results.Ok(new { total = insumos.Count, items = insumos });
});

app.MapPost("/api/oci/{id:int}/generar-guia-interna", async (int id, DocumentoAccionApiRequest request) =>
{
    string usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "Android" : request.Usuario.Trim();
    string observacion = string.IsNullOrWhiteSpace(request.Motivo) ? "Guía interna generada desde Android" : request.Motivo.Trim();

    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();

    int idAlmacen = 0;
    List<GuiaInternaDetalleApiRequest> detalles = [];
    await using (SqlCommand prepararCmd = new("USP_VEN_GUIA_INTERNA_PREPARAR", conexion) { CommandType = CommandType.StoredProcedure })
    {
        prepararCmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
        prepararCmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = DBNull.Value;
        await using SqlDataReader dr = await prepararCmd.ExecuteReaderAsync();
        if (!await dr.ReadAsync())
            return Results.NotFound(new { mensaje = "OCI no encontrada." });

        idAlmacen = Convert.ToInt32(dr["IdAlmacen"]);
        if (await dr.NextResultAsync())
        {
            while (await dr.ReadAsync())
            {
                decimal cantidad = Convert.ToDecimal(dr["CantidadSugerida"]);
                if (cantidad > 0)
                {
                    detalles.Add(new(
                        Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
                        cantidad,
                        dr["Observacion"]?.ToString() ?? string.Empty));
                }
            }
        }
    }

    if (detalles.Count == 0)
        return Results.BadRequest(new { mensaje = "No hay stock disponible para generar Guía Interna. Genere una OT para producción." });

    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_EMITIR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = idAlmacen;
    cmd.Parameters.Add("@FechaEmision", SqlDbType.Date).Value = DateTime.Today;
    cmd.Parameters.Add("@UsuarioEmisor", SqlDbType.VarChar, 80).Value = usuario;
    cmd.Parameters.Add("@UsuarioAutorizador", SqlDbType.VarChar, 80).Value = usuario;
    cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = observacion;
    cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
    {
        TypeName = "dbo.GuiaInternaDetalleType",
        Value = CrearTablaGuiaInterna(detalles)
    });
    SqlParameter numero = new("@NumeroGuia", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
    SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(numero);
    cmd.Parameters.Add(mensaje);
    await cmd.ExecuteNonQueryAsync();

    return Results.Ok(new
    {
        mensaje = mensaje.Value?.ToString() ?? $"Guía interna {numero.Value} generada correctamente.",
        idGuiaInterna = await ObtenerIdGuiaInternaPorNumeroAsync(connectionString, numero.Value?.ToString() ?? string.Empty),
        numeroGuia = numero.Value?.ToString() ?? string.Empty
    });
});

app.MapGet("/api/oci/{id:int}/guia-interna/preparar", async (int id, int? idAlmacen) =>
{
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_PREPARAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = idAlmacen.HasValue && idAlmacen.Value > 0 ? idAlmacen.Value : DBNull.Value;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return Results.NotFound(new { mensaje = "OCI no encontrada." });

    var cabecera = new
    {
        origen = "OCI",
        idOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
        numeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
        numeroProforma = LeerString(dr, "NumeroProforma"),
        ordenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
        idAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
        nombreAlmacen = LeerString(dr, "NombreAlmacen"),
        rucEmisor = LeerString(dr, "RucEmisor"),
        empresaEmisora = LeerString(dr, "EmpresaEmisora"),
        rucDestino = LeerString(dr, "RucDestino"),
        empresaDestino = LeerString(dr, "EmpresaDestino"),
        fechaEmision = DateTime.Today
    };

    List<object> detalles = [];
    if (await dr.NextResultAsync())
    {
        while (await dr.ReadAsync())
        {
            detalles.Add(new
            {
                idOrdenCompraInternaDetalle = Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
                idProducto = Convert.ToInt32(dr["IdProducto"]),
                codigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                nombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                idUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                nombreUnidad = LeerString(dr, "NombreUnidad"),
                cantidadRequerida = Convert.ToDecimal(dr["CantidadRequerida"]),
                cantidadEntregada = Convert.ToDecimal(dr["CantidadEntregada"]),
                cantidadPendiente = Convert.ToDecimal(dr["CantidadPendiente"]),
                stockActual = Convert.ToDecimal(dr["StockActual"]),
                precioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
                cantidadDespachar = Convert.ToDecimal(dr["CantidadSugerida"]),
                observacion = dr["Observacion"]?.ToString() ?? string.Empty
            });
        }
    }

    return Results.Ok(new { cabecera, detalles });
});

app.MapPost("/api/oci/{id:int}/guia-interna/emitir", async (int id, GuiaInternaOciEmitirApiRequest request) =>
{
    if (request.IdAlmacen <= 0)
        return Results.BadRequest(new { mensaje = "Seleccione almacÃ©n." });
    if (request.Detalles.Count == 0 || request.Detalles.All(x => x.CantidadDespachar <= 0))
        return Results.BadRequest(new { mensaje = "Ingrese al menos un producto con cantidad a despachar." });

    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();

    Dictionary<int, decimal> maximos = [];
    await using (SqlCommand prepararCmd = new("USP_VEN_GUIA_INTERNA_PREPARAR", conexion) { CommandType = CommandType.StoredProcedure })
    {
        prepararCmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
        prepararCmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = request.IdAlmacen;
        await using SqlDataReader dr = await prepararCmd.ExecuteReaderAsync();
        if (!await dr.ReadAsync())
            return Results.NotFound(new { mensaje = "OCI no encontrada." });
        if (await dr.NextResultAsync())
        {
            while (await dr.ReadAsync())
            {
                decimal pendiente = Convert.ToDecimal(dr["CantidadPendiente"]);
                decimal stock = Convert.ToDecimal(dr["StockActual"]);
                maximos[Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"])] = Math.Max(0, Math.Min(pendiente, stock));
            }
        }
    }

    List<GuiaInternaDetalleApiRequest> detalles = [];
    foreach (GuiaInternaOciDetalleApiRequest detalle in request.Detalles.Where(x => x.CantidadDespachar > 0))
    {
        if (!maximos.TryGetValue(detalle.IdOrdenCompraInternaDetalle, out decimal maximo))
            return Results.BadRequest(new { mensaje = "Uno de los productos no pertenece a la OCI." });
        if (detalle.CantidadDespachar > maximo)
            return Results.BadRequest(new { mensaje = $"La cantidad maxima para un producto es {maximo:N2}." });

        detalles.Add(new(detalle.IdOrdenCompraInternaDetalle, detalle.CantidadDespachar, detalle.Observacion?.Trim() ?? string.Empty));
    }

    if (detalles.Count == 0)
        return Results.BadRequest(new { mensaje = "No hay stock disponible para generar Guia Interna." });

    string usuario = string.IsNullOrWhiteSpace(request.UsuarioEmisor) ? "Android" : request.UsuarioEmisor.Trim();
    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_EMITIR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = request.IdAlmacen;
    cmd.Parameters.Add("@FechaEmision", SqlDbType.Date).Value = request.FechaEmision == default ? DateTime.Today : request.FechaEmision.Date;
    cmd.Parameters.Add("@UsuarioEmisor", SqlDbType.VarChar, 80).Value = usuario;
    cmd.Parameters.Add("@UsuarioAutorizador", SqlDbType.VarChar, 80).Value = string.IsNullOrWhiteSpace(request.UsuarioAutorizador) ? usuario : request.UsuarioAutorizador.Trim();
    cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = request.Observacion?.Trim() ?? string.Empty;
    cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
    {
        TypeName = "dbo.GuiaInternaDetalleType",
        Value = CrearTablaGuiaInterna(detalles)
    });
    SqlParameter numero = new("@NumeroGuia", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
    SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(numero);
    cmd.Parameters.Add(mensaje);
    await cmd.ExecuteNonQueryAsync();

    string numeroGuia = numero.Value?.ToString() ?? string.Empty;
    return Results.Ok(new
    {
        mensaje = mensaje.Value?.ToString() ?? $"Guia interna {numeroGuia} generada correctamente.",
        idGuiaInterna = await ObtenerIdGuiaInternaPorNumeroAsync(connectionString, numeroGuia),
        numeroGuia
    });
});

app.MapPost("/api/oci/{id:int}/anular", async (int id, DocumentoAccionApiRequest request) =>
{
    string usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "Android" : request.Usuario.Trim();
    string motivo = string.IsNullOrWhiteSpace(request.Motivo) ? "Anulado desde Android" : request.Motivo.Trim();
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_OCI_ANULAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@MotivoAnulacion", SqlDbType.VarChar, 200).Value = motivo;
    cmd.Parameters.Add("@UsuarioAnulacion", SqlDbType.VarChar, 80).Value = usuario;
    SqlParameter mensajeParam = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(mensajeParam);
    await conexion.OpenAsync();
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensaje = mensajeParam.Value?.ToString() ?? string.Empty });
});

app.MapGet("/api/guias-internas", async (string? buscar, string? estado, string? origen) =>
{
    List<object> items = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = DBNull.Value;
    cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = DBNull.Value;
    cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = DBNull.Value;
    cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(estado) || estado == "Todos" ? DBNull.Value : estado.Trim();
    cmd.Parameters.Add("@Origen", SqlDbType.VarChar, 30).Value = string.IsNullOrWhiteSpace(origen) || origen == "Todos" ? DBNull.Value : origen.Trim();
    cmd.Parameters.Add("@Texto", SqlDbType.VarChar, 150).Value = string.IsNullOrWhiteSpace(buscar) ? DBNull.Value : buscar.Trim();
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
        items.Add(MapearGuiaInternaCabecera(dr));

    return Results.Ok(new { total = items.Count, items });
});

app.MapGet("/api/guias-internas/{id:int}", async (int id) =>
{
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdGuiaInterna", SqlDbType.Int).Value = id;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return Results.NotFound(new { mensaje = "Guia interna no encontrada." });

    object cabecera = MapearGuiaInternaCabecera(dr);
    List<object> detalles = [];
    if (await dr.NextResultAsync())
    {
        while (await dr.ReadAsync())
            detalles.Add(MapearGuiaInternaDetalle(dr));
    }

    return Results.Ok(new { cabecera, detalles });
});

app.MapGet("/api/guias-internas/{id:int}/pdf", async (int id) =>
{
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdGuiaInterna", SqlDbType.Int).Value = id;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return Results.NotFound(new { mensaje = "Guia interna no encontrada." });

    GuiaInternaPdfCabecera cabecera = LeerGuiaInternaPdfCabecera(dr);
    List<GuiaInternaPdfDetalle> detalles = [];
    if (await dr.NextResultAsync())
    {
        while (await dr.ReadAsync())
            detalles.Add(LeerGuiaInternaPdfDetalle(dr));
    }

    EmpresaPdfInfo empresa = await ObtenerEmpresaPdfAsync(connectionString);
    byte[] pdf = CrearGuiaInternaPdf(empresa, cabecera, detalles);
    string fileName = string.IsNullOrWhiteSpace(cabecera.NumeroGuia) ? $"guia-interna-{id}.pdf" : $"{cabecera.NumeroGuia}.pdf";
    return Results.File(pdf, "application/pdf", fileName);
});

app.MapGet("/api/guias-internas/manual/preparar", async (int? idAlmacen) =>
{
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_MANUAL_PREPARAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = idAlmacen.HasValue && idAlmacen.Value > 0 ? idAlmacen.Value : DBNull.Value;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return Results.NotFound(new { mensaje = "No existe un almacen activo para emitir la guia." });

    var cabecera = new
    {
        origen = "Manual",
        idAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
        nombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
        rucEmisor = dr["RucEmisor"]?.ToString() ?? string.Empty,
        empresaEmisora = dr["EmpresaEmisora"]?.ToString() ?? string.Empty,
        fechaEmision = DateTime.Today
    };

    List<object> productos = [];
    if (await dr.NextResultAsync())
    {
        while (await dr.ReadAsync())
        {
            productos.Add(new
            {
                idProducto = Convert.ToInt32(dr["IdProducto"]),
                codigo = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                nombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                etiquetaCliente = string.Empty,
                idUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
                nombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                stockActual = Convert.ToDecimal(dr["StockActual"])
            });
        }
    }

    return Results.Ok(new { cabecera, productos });
});

app.MapPost("/api/guias-internas/manual/emitir", async (GuiaInternaManualApiRequest request) =>
{
    if (request.IdAlmacen <= 0)
        return Results.BadRequest(new { mensaje = "Seleccione almacen." });
    if (string.IsNullOrWhiteSpace(request.MotivoEmisionManual))
        return Results.BadRequest(new { mensaje = "Seleccione el motivo de emision." });
    if (request.Detalles.Count == 0 || request.Detalles.Any(x => x.IdProducto <= 0 || x.CantidadDespachar <= 0))
        return Results.BadRequest(new { mensaje = "Agregue productos con cantidad mayor a cero." });

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_MANUAL_EMITIR", conexion) { CommandType = CommandType.StoredProcedure };
    string usuario = string.IsNullOrWhiteSpace(request.UsuarioEmisor) ? "Android" : request.UsuarioEmisor.Trim();
    cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = request.IdAlmacen;
    cmd.Parameters.Add("@FechaEmision", SqlDbType.Date).Value = request.FechaEmision == default ? DateTime.Today : request.FechaEmision.Date;
    cmd.Parameters.Add("@UsuarioEmisor", SqlDbType.VarChar, 100).Value = usuario;
    cmd.Parameters.Add("@UsuarioAutorizador", SqlDbType.VarChar, 100).Value = string.IsNullOrWhiteSpace(request.UsuarioAutorizador) ? usuario : request.UsuarioAutorizador.Trim();
    cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = request.IdCliente.HasValue && request.IdCliente.Value > 0 ? request.IdCliente.Value : DBNull.Value;
    cmd.Parameters.Add("@MotivoEmisionManual", SqlDbType.VarChar, 150).Value = request.MotivoEmisionManual.Trim();
    cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = request.Observacion?.Trim() ?? string.Empty;
    cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
    {
        TypeName = "dbo.GuiaInternaManualDetalleType",
        Value = CrearTablaGuiaInternaManual(request.Detalles)
    });
    SqlParameter numero = new("@NumeroGuia", SqlDbType.VarChar, 30) { Direction = ParameterDirection.Output };
    SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(numero);
    cmd.Parameters.Add(mensaje);
    await conexion.OpenAsync();
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensaje = mensaje.Value?.ToString() ?? "Guia interna emitida.", numeroGuia = numero.Value?.ToString() ?? string.Empty });
});

app.MapPost("/api/guias-internas/{id:int}/anular", async (int id, DocumentoAccionApiRequest request) =>
{
    string motivo = request.Motivo?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(motivo))
        return Results.BadRequest(new { mensaje = "Ingrese el motivo de anulacion." });

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_VEN_GUIA_INTERNA_ANULAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdGuiaInterna", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 100).Value = request.Usuario?.Trim() ?? "Android";
    cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;
    SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(mensaje);
    await conexion.OpenAsync();
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensaje = mensaje.Value?.ToString() ?? string.Empty });
});

app.MapGet("/api/ordenes-trabajo", async (string? buscar) =>
{
    List<object> items = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_PRO_OT_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    string filtro = (buscar ?? string.Empty).Trim();

    while (await dr.ReadAsync())
    {
        string numero = dr["NumeroOT"]?.ToString() ?? string.Empty;
        string cliente = dr["NombreCliente"]?.ToString() ?? string.Empty;
        string oci = dr["NumeroOci"]?.ToString() ?? string.Empty;
        string estado = dr["Estado"]?.ToString() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(filtro)
            && !numero.Contains(filtro, StringComparison.OrdinalIgnoreCase)
            && !cliente.Contains(filtro, StringComparison.OrdinalIgnoreCase)
            && !oci.Contains(filtro, StringComparison.OrdinalIgnoreCase)
            && !estado.Contains(filtro, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        decimal totalPlanificado = Convert.ToDecimal(dr["TotalPlanificado"]);
        decimal totalLanzado = Convert.ToDecimal(dr["TotalLanzado"]);
        int idOrdenTrabajo = Convert.ToInt32(dr["IdOrdenTrabajo"]);
        decimal totalProducido = await ObtenerTotalProducidoOtAsync(connectionString, idOrdenTrabajo);
        decimal avance = estado.Equals("TERMINADA", StringComparison.OrdinalIgnoreCase)
            ? 1
            : totalPlanificado <= 0 ? 0 : Math.Min(1, totalProducido / totalPlanificado);

        items.Add(new
        {
            idOrdenTrabajo,
            numeroOT = numero,
            numeroOci = oci,
            ordenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
            tipoOT = dr["TipoOT"]?.ToString() ?? string.Empty,
            nombreCliente = cliente,
            fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
            estado,
            cantidadProductos = Convert.ToInt32(dr["CantidadProductos"]),
            totalPlanificado,
            totalLanzado,
            avance
        });
    }

    return Results.Ok(new { total = items.Count, items });
});

app.MapGet("/api/ordenes-trabajo/{id:int}", async (int id) =>
{
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_PRO_OT_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = id;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return Results.NotFound(new { mensaje = "Orden de trabajo no encontrada." });

    var cabecera = new
    {
        idOrdenTrabajo = id,
        numeroOT = dr["NumeroOT"]?.ToString() ?? string.Empty,
        idOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
        numeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
        ordenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
        tipoOT = dr["TipoOT"]?.ToString() ?? string.Empty,
        idCliente = Convert.ToInt32(dr["IdCliente"]),
        nombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
        fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
        estado = dr["Estado"]?.ToString() ?? string.Empty,
        usuarioCreacion = dr["NombreUsuario"]?.ToString() ?? string.Empty,
        usuarioAutoriza = dr["UsuarioAutoriza"]?.ToString() ?? string.Empty,
        observacion = dr["Observacion"]?.ToString() ?? string.Empty,
        fechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
    };

    List<object> detalles = [];
    if (await dr.NextResultAsync())
    {
        while (await dr.ReadAsync())
        {
            detalles.Add(new
            {
                idDetalleOT = Convert.ToInt32(dr["IdDetalleOT"]),
                idOrdenTrabajo = Convert.ToInt32(dr["IdOrdenTrabajo"]),
                idProducto = Convert.ToInt32(dr["IdProducto"]),
                codigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                nombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                cantidadRequerida = Convert.ToDecimal(dr["CantidadRequerida"]),
                cantidadPlanificada = Convert.ToDecimal(dr["CantidadPlanificada"]),
                cantidadLanzada = Convert.ToDecimal(dr["CantidadLanzada"]),
                cantidadProducida = Convert.ToDecimal(dr["CantidadProducida"]),
                cantidadPendiente = Convert.ToDecimal(dr["CantidadPendiente"]),
                estado = dr["Estado"]?.ToString() ?? string.Empty
            });
        }
    }

    List<object> areas = [];
    if (await dr.NextResultAsync())
    {
        while (await dr.ReadAsync())
        {
            areas.Add(new
            {
                idDetalleArea = Convert.ToInt64(dr["IdDetalleArea"]),
                idOrdenTrabajo = Convert.ToInt32(dr["IdOrdenTrabajo"]),
                idDetalleOT = Convert.ToInt32(dr["IdDetalleOT"]),
                idAreaProduccion = Convert.ToInt32(dr["IdAreaProduccion"]),
                codigoArea = dr["CodigoArea"]?.ToString() ?? string.Empty,
                nombreArea = dr["NombreArea"]?.ToString() ?? string.Empty,
                ordenSecuencia = Convert.ToInt32(dr["OrdenSecuencia"]),
                esInicio = Convert.ToBoolean(dr["EsInicio"]),
                esTermino = Convert.ToBoolean(dr["EsTermino"]),
                manejaMerma = Convert.ToBoolean(dr["ManejaMerma"]),
                modoEnvio = dr["ModoEnvio"]?.ToString() ?? string.Empty,
                cantidadRecibida = Convert.ToDecimal(dr["CantidadRecibida"]),
                cantidadEnviada = Convert.ToDecimal(dr["CantidadEnviada"]),
                cantidadMerma = Convert.ToDecimal(dr["CantidadMerma"]),
                cantidadPendiente = Convert.ToDecimal(dr["CantidadPendiente"]),
                estado = dr["Estado"]?.ToString() ?? string.Empty,
                codigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                nombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty
            });
        }
    }

    return Results.Ok(new { cabecera, detalles, areas });
});

app.MapGet("/api/ordenes-trabajo/{id:int}/kardex", async (int id) =>
{
    const string sql = @"
SELECT d.CodigoProducto,d.NombreProducto,k.Cantidad,al.NombreAlmacen Almacen,k.FechaMovimiento,k.UsuarioResponsable Usuario
FROM dbo.KardexProductos k
JOIN dbo.OrdenTrabajoTerminacion t ON t.IdOperacionTerminacion=k.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoTerminacionDetalle td ON td.IdOperacionTerminacion=t.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=td.IdDetalleOT AND d.IdProducto=k.IdProducto
JOIN dbo.Almacenes al ON al.IdAlmacen=k.IdAlmacen
WHERE t.IdOrdenTrabajo=@IdOrdenTrabajo
ORDER BY k.FechaMovimiento DESC;";

    List<OrdenTrabajoKardexApi> items = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = id;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        items.Add(new OrdenTrabajoKardexApi(
            LeerString(dr, "CodigoProducto"),
            LeerString(dr, "NombreProducto"),
            Convert.ToDecimal(dr["Cantidad"]),
            LeerString(dr, "Almacen"),
            Convert.ToDateTime(dr["FechaMovimiento"]),
            LeerString(dr, "Usuario")));
    }

    return Results.Ok(new { total = items.Count, items });
});

app.MapGet("/api/ordenes-trabajo/{id:int}/movimientos", async (int id) =>
{
    const string sql = @"
SELECT t.FechaRegistro FechaHora,d.CodigoProducto,d.NombreProducto,ao.NombreArea Origen,ad.NombreArea Destino,
       td.CantidadEnviada Cantidad,'AVANCE_AREA' Accion,ISNULL(ua.NombreUsuario,us.NombreUsuario) Usuario,t.Observacion
FROM dbo.OrdenTrabajoTransferencia t
JOIN dbo.OrdenTrabajoTransferenciaDetalle td ON td.IdOperacionTransferencia=t.IdOperacionTransferencia
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=td.IdDetalleOT
JOIN dbo.AreaProduccion ao ON ao.IdAreaProduccion=t.IdAreaOrigen
JOIN dbo.AreaProduccion ad ON ad.IdAreaProduccion=t.IdAreaDestino
JOIN dbo.Usuarios us ON us.IdUsuario=t.IdUsuarioSesion
LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario=t.IdUsuarioAutoriza
WHERE t.IdOrdenTrabajo=@IdOrdenTrabajo
UNION ALL
SELECT m.FechaRegistro FechaHora,d.CodigoProducto,d.NombreProducto,a.NombreArea Origen,'' Destino,
       m.Cantidad,'REGISTRO_MERMA' Accion,ISNULL(ua.NombreUsuario,us.NombreUsuario) Usuario,
       CONCAT(m.Motivo,CASE WHEN NULLIF(m.Observacion,'') IS NULL THEN '' ELSE CONCAT(' - ',m.Observacion) END) Observacion
FROM dbo.OrdenTrabajoMerma m
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=m.IdDetalleOT
JOIN dbo.OrdenTrabajoDetalleArea da ON da.IdDetalleArea=m.IdDetalleArea
JOIN dbo.AreaProduccion a ON a.IdAreaProduccion=da.IdAreaProduccion
JOIN dbo.Usuarios us ON us.IdUsuario=m.IdUsuarioSesion
LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario=m.IdUsuarioAutoriza
WHERE m.IdOrdenTrabajo=@IdOrdenTrabajo
UNION ALL
SELECT c.FechaRegistro FechaHora,d.CodigoProducto,d.NombreProducto,'INSUMOS' Origen,'PRODUCCION' Destino,
       SUM(c.CantidadConsumida) Cantidad,'CONSUMO_INSUMOS' Accion,u.NombreUsuario Usuario,'' Observacion
FROM dbo.OrdenTrabajoConsumoInsumo c
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=c.IdDetalleOT
JOIN dbo.Usuarios u ON u.IdUsuario=c.IdUsuario
WHERE c.IdOrdenTrabajo=@IdOrdenTrabajo
GROUP BY c.FechaRegistro,d.CodigoProducto,d.NombreProducto,u.NombreUsuario
UNION ALL
SELECT t.FechaRegistro FechaHora,d.CodigoProducto,d.NombreProducto,a.NombreArea Origen,'PRODUCTO TERMINADO' Destino,
       td.Cantidad,'CIERRE_PRODUCCION' Accion,ISNULL(ua.NombreUsuario,us.NombreUsuario) Usuario,t.Observacion
FROM dbo.OrdenTrabajoTerminacion t
JOIN dbo.OrdenTrabajoTerminacionDetalle td ON td.IdOperacionTerminacion=t.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=td.IdDetalleOT
JOIN dbo.AreaProduccion a ON a.IdAreaProduccion=t.IdAreaTermino
JOIN dbo.Usuarios us ON us.IdUsuario=t.IdUsuarioSesion
LEFT JOIN dbo.Usuarios ua ON ua.IdUsuario=t.IdUsuarioAutoriza
WHERE t.IdOrdenTrabajo=@IdOrdenTrabajo
UNION ALL
SELECT k.FechaMovimiento FechaHora,d.CodigoProducto,d.NombreProducto,'PRODUCCION' Origen,al.NombreAlmacen Destino,
       k.Cantidad,'INGRESO_KARDEX' Accion,k.UsuarioResponsable Usuario,k.Observacion
FROM dbo.KardexProductos k
JOIN dbo.OrdenTrabajoTerminacion t ON t.IdOperacionTerminacion=k.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoTerminacionDetalle td ON td.IdOperacionTerminacion=t.IdOperacionTerminacion
JOIN dbo.OrdenTrabajoDetalle d ON d.IdDetalleOT=td.IdDetalleOT AND d.IdProducto=k.IdProducto
JOIN dbo.Almacenes al ON al.IdAlmacen=k.IdAlmacen
WHERE t.IdOrdenTrabajo=@IdOrdenTrabajo
ORDER BY FechaHora DESC;";

    List<OrdenTrabajoMovimientoApi> items = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = id;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        items.Add(new OrdenTrabajoMovimientoApi(
            Convert.ToDateTime(dr["FechaHora"]),
            LeerString(dr, "CodigoProducto"),
            LeerString(dr, "NombreProducto"),
            LeerString(dr, "Origen"),
            LeerString(dr, "Destino"),
            Convert.ToDecimal(dr["Cantidad"]),
            LeerString(dr, "Accion"),
            LeerString(dr, "Usuario"),
            LeerString(dr, "Observacion")));
    }

    return Results.Ok(new { total = items.Count, items });
});

app.MapPost("/api/ordenes-trabajo/{id:int}/lanzar", async (int id, OrdenTrabajoLanzarApiRequest request) =>
{
    if (request.Detalles.Count == 0)
        return Results.BadRequest(new { mensaje = "Seleccione al menos un producto para iniciar." });

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_PRO_OT_LANZAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = id;
    cmd.Parameters.Add("@IdUsuarioSesion", SqlDbType.Int).Value = request.IdUsuarioSesion;
    cmd.Parameters.Add("@IdUsuarioAutoriza", SqlDbType.Int).Value = request.IdUsuarioAutoriza <= 0 ? request.IdUsuarioSesion : request.IdUsuarioAutoriza;
    cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
    {
        TypeName = "dbo.TipoOTLanzamiento",
        Value = CrearTablaLanzamiento(request.Detalles)
    });
    await conexion.OpenAsync();
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensaje = "Producción iniciada correctamente." });
});

app.MapPost("/api/ordenes-trabajo/{id:int}/transferir", async (int id, OrdenTrabajoTransferirApiRequest request) =>
{
    if (request.Detalles.Count == 0)
        return Results.BadRequest(new { mensaje = "Seleccione al menos un producto para transferir." });

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(request.EsTerminacion ? "USP_PRO_OT_TERMINAR" : "USP_PRO_OT_TRANSFERIR", conexion)
    {
        CommandType = CommandType.StoredProcedure
    };
    cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = id;
    cmd.Parameters.Add(request.EsTerminacion ? "@IdAreaTermino" : "@IdAreaOrigen", SqlDbType.Int).Value = request.IdAreaProduccion;
    cmd.Parameters.Add("@IdUsuarioSesion", SqlDbType.Int).Value = request.IdUsuarioSesion;
    cmd.Parameters.Add("@IdUsuarioAutoriza", SqlDbType.Int).Value = request.IdUsuarioAutoriza <= 0 ? request.IdUsuarioSesion : request.IdUsuarioAutoriza;
    cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = request.Observacion ?? string.Empty;
    cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured)
    {
        TypeName = "dbo.TipoOTTransferencia",
        Value = CrearTablaTransferencia(request.Detalles)
    });
    SqlParameter op = new("@IdOperacion", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
    cmd.Parameters.Add(op);
    await conexion.OpenAsync();
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new
    {
        mensaje = request.EsTerminacion ? "Producto terminado correctamente." : "Transferencia realizada correctamente.",
        idOperacion = Convert.ToInt64(op.Value)
    });
});

app.MapPost("/api/ordenes-trabajo/{id:int}/merma", async (int id, OrdenTrabajoMermaApiRequest request) =>
{
    if (request.Cantidad <= 0)
        return Results.BadRequest(new { mensaje = "Ingrese una cantidad de merma mayor a cero." });

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("USP_PRO_OT_MERMA_REGISTRAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdDetalleArea", SqlDbType.BigInt).Value = request.IdDetalleArea;
    cmd.Parameters.Add("@Cantidad", SqlDbType.Decimal).Value = request.Cantidad;
    cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 120).Value = string.IsNullOrWhiteSpace(request.Motivo) ? "MERMA EN OPERACION" : request.Motivo;
    cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = request.Observacion ?? string.Empty;
    cmd.Parameters.Add("@IdUsuarioSesion", SqlDbType.Int).Value = request.IdUsuarioSesion;
    cmd.Parameters.Add("@IdUsuarioAutoriza", SqlDbType.Int).Value = request.IdUsuarioAutoriza <= 0 ? request.IdUsuarioSesion : request.IdUsuarioAutoriza;
    await conexion.OpenAsync();
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensaje = "Merma registrada correctamente." });
});

app.MapGet("/api/stock/manual/ingresos", async (string? buscar) =>
{
    List<object> ingresos = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("dbo.USP_ALM_INGRESO_MANUAL_STOCK_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = DBNull.Value;
    cmd.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = DBNull.Value;
    cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = DBNull.Value;
    cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = DBNull.Value;
    cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = DBNull.Value;
    cmd.Parameters.Add("@NumeroDocumento", SqlDbType.VarChar, 60).Value = string.IsNullOrWhiteSpace(buscar) ? DBNull.Value : buscar.Trim();
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        ingresos.Add(new
        {
            idIngresoManualStock = Convert.ToInt32(dr["IdIngresoManualStock"]),
            fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
            nombreProveedor = dr["NombreProveedor"]?.ToString() ?? string.Empty,
            nombreTipoDocumento = dr["NombreTipoDocumento"]?.ToString() ?? string.Empty,
            numeroDocumento = dr["NumeroDocumento"]?.ToString() ?? string.Empty,
            nombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
            observacion = dr["Observacion"]?.ToString() ?? string.Empty,
            estado = dr["Estado"]?.ToString() ?? string.Empty,
            total = Convert.ToDecimal(dr["Total"]),
            usuarioCreador = dr["UsuarioCreador"]?.ToString() ?? string.Empty,
            fechaCreacion = dr["FechaCreacion"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["FechaCreacion"])
        });
    }

    return Results.Ok(new { total = ingresos.Count, items = ingresos });
});

app.MapGet("/api/stock/manual/ingresos/{id:int}", async (int id) =>
{
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("dbo.USP_ALM_INGRESO_MANUAL_STOCK_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdIngresoManualStock", SqlDbType.Int).Value = id;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return Results.NotFound(new { mensaje = "Ingreso manual de stock no encontrado." });

    var cabecera = new
    {
        idIngresoManualStock = Convert.ToInt32(dr["IdIngresoManualStock"]),
        fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
        nombreProveedor = dr["NombreProveedor"]?.ToString() ?? string.Empty,
        nombreTipoDocumento = dr["NombreTipoDocumento"]?.ToString() ?? string.Empty,
        numeroDocumento = dr["NumeroDocumento"]?.ToString() ?? string.Empty,
        nombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
        observacion = dr["Observacion"]?.ToString() ?? string.Empty,
        estado = dr["Estado"]?.ToString() ?? string.Empty,
        subtotal = Convert.ToDecimal(dr["Subtotal"]),
        descuentoTotal = Convert.ToDecimal(dr["DescuentoTotal"]),
        total = Convert.ToDecimal(dr["Total"]),
        usuarioCreador = dr["UsuarioCreador"]?.ToString() ?? string.Empty,
        fechaCreacion = dr["FechaCreacion"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["FechaCreacion"]),
        usuarioAbastecimiento = dr["UsuarioAbastecimiento"]?.ToString() ?? string.Empty,
        fechaAbastecimiento = dr["FechaAbastecimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["FechaAbastecimiento"])
    };

    List<object> detalles = [];
    if (await dr.NextResultAsync())
    {
        while (await dr.ReadAsync())
        {
            detalles.Add(new
            {
                idIngresoManualStockDetalle = Convert.ToInt32(dr["IdIngresoManualStockDetalle"]),
                idIngresoManualStock = Convert.ToInt32(dr["IdIngresoManualStock"]),
                idProducto = Convert.ToInt32(dr["IdProducto"]),
                codigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                nombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                nombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
                stockActual = Convert.ToDecimal(dr["StockActual"]),
                cantidad = Convert.ToDecimal(dr["Cantidad"]),
                precioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
                descuento = Convert.ToDecimal(dr["Descuento"]),
                importe = Convert.ToDecimal(dr["Importe"])
            });
        }
    }

    return Results.Ok(new { cabecera, detalles });
});

app.MapGet("/api/stock/manual/preparar", async () =>
{
    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();

    List<object> proveedores = [];
    await using (SqlCommand cmd = new("dbo.USP_ALM_PROVEEDOR_LISTAR", conexion) { CommandType = CommandType.StoredProcedure })
    await using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
    {
        while (await dr.ReadAsync())
        {
            proveedores.Add(new
            {
                idProveedor = Convert.ToInt32(dr["IdProveedor"]),
                nombreRazonSocial = dr["NombreRazonSocial"]?.ToString() ?? string.Empty
            });
        }
    }

    List<object> almacenes = [];
    await using (SqlCommand cmd = new("dbo.USP_ALM_ALMACEN_LISTAR", conexion) { CommandType = CommandType.StoredProcedure })
    await using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
    {
        while (await dr.ReadAsync())
        {
            almacenes.Add(new
            {
                idAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
                nombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty
            });
        }
    }

    List<object> tiposDocumento = [];
    await using (SqlCommand cmd = new("dbo.USP_ALM_TIPO_DOCUMENTO_STOCK_LISTAR", conexion) { CommandType = CommandType.StoredProcedure })
    await using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
    {
        while (await dr.ReadAsync())
        {
            tiposDocumento.Add(new
            {
                idTipoDocumento = Convert.ToInt32(dr["IdTipoDocumento"]),
                nombreTipoDocumento = dr["NombreTipoDocumento"]?.ToString() ?? string.Empty
            });
        }
    }

    return Results.Ok(new { proveedores, almacenes, tiposDocumento });
});

app.MapGet("/api/stock/manual/productos", async (int idAlmacen, string? buscar) =>
{
    List<object> productos = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("dbo.USP_ALM_PRODUCTO_STOCK_BUSCAR", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = idAlmacen;
    cmd.Parameters.Add("@Texto", SqlDbType.VarChar, 150).Value = buscar?.Trim() ?? string.Empty;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        productos.Add(new
        {
            idProducto = Convert.ToInt32(dr["IdProducto"]),
            codigo = dr["Codigo"]?.ToString() ?? string.Empty,
            nombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
            etiquetaCliente = dr["EtiquetaCliente"]?.ToString() ?? string.Empty,
            idUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
            nombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
            stockActual = Convert.ToDecimal(dr["StockActual"])
        });
    }

    return Results.Ok(new { total = productos.Count, items = productos });
});

app.MapPost("/api/stock/manual/ingresar", async (IngresoManualApiRequest request) =>
{
    if (request.IdProveedor <= 0 || request.IdTipoDocumento <= 0 || request.IdAlmacen <= 0)
        return Results.BadRequest(new { mensaje = "Seleccione proveedor, tipo de documento y almacen." });

    if (request.Detalles.Count == 0 || request.Detalles.Any(x => x.IdProducto <= 0 || x.Cantidad <= 0))
        return Results.BadRequest(new { mensaje = "Agregue productos con cantidad mayor a cero." });

    string usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "Android" : request.Usuario.Trim();
    string serie = "APP";
    string numero = DateTime.Now.ToString("yyyyMMddHHmmss");
    string numeroDocumento = $"{serie}-{numero}";

    await using SqlConnection conexion = new(connectionString);
    await conexion.OpenAsync();
    await using SqlTransaction tx = (SqlTransaction)await conexion.BeginTransactionAsync();

    try
    {
        await using SqlCommand cabecera = new(@"
INSERT INTO dbo.IngresosManualesStock
(
    FechaEmision, IdProveedor, IdTipoDocumento, TipoNumeracion, Serie, Numero, NumeroDocumento,
    IdAlmacen, Observacion, Estado, Subtotal, DescuentoTotal, Total, UsuarioCreador, FechaCreacion,
    UsuarioAbastecimiento, FechaAbastecimiento
)
VALUES
(
    CAST(GETDATE() AS DATE), @IdProveedor, @IdTipoDocumento, 'Automatica', @Serie, @Numero, @NumeroDocumento,
    @IdAlmacen, @Observacion, 'Abastecido', 0, 0, 0, @Usuario, GETDATE(),
    @Usuario, GETDATE()
);
SELECT CONVERT(INT, SCOPE_IDENTITY());", conexion, tx);
        cabecera.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = request.IdProveedor;
        cabecera.Parameters.Add("@IdTipoDocumento", SqlDbType.Int).Value = request.IdTipoDocumento;
        cabecera.Parameters.Add("@Serie", SqlDbType.VarChar, 20).Value = serie;
        cabecera.Parameters.Add("@Numero", SqlDbType.VarChar, 30).Value = numero;
        cabecera.Parameters.Add("@NumeroDocumento", SqlDbType.VarChar, 60).Value = numeroDocumento;
        cabecera.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = request.IdAlmacen;
        cabecera.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = request.Observacion?.Trim() ?? string.Empty;
        cabecera.Parameters.Add("@Usuario", SqlDbType.VarChar, 100).Value = usuario;
        int idIngreso = Convert.ToInt32(await cabecera.ExecuteScalarAsync());

        decimal total = 0m;
        foreach (IngresoManualDetalleApiRequest detalle in request.Detalles)
        {
            await using SqlCommand productoCmd = new(@"
SELECT TOP (1) IdProducto, Codigo, IdUnidadMedida
FROM dbo.Productos
WHERE IdProducto = @IdProducto AND Estado = 1;", conexion, tx);
            productoCmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = detalle.IdProducto;
            await using SqlDataReader productoReader = await productoCmd.ExecuteReaderAsync();
            if (!await productoReader.ReadAsync())
            {
                await tx.RollbackAsync();
                return Results.BadRequest(new { mensaje = $"Producto {detalle.IdProducto} no existe o esta inactivo." });
            }

            string codigo = productoReader["Codigo"]?.ToString() ?? string.Empty;
            int idUnidadMedida = Convert.ToInt32(productoReader["IdUnidadMedida"]);
            await productoReader.CloseAsync();

            await using SqlCommand detalleCmd = new(@"
INSERT INTO dbo.IngresosManualesStockDetalle
(
    IdIngresoManualStock, IdProducto, CodigoProducto, IdUnidadMedida,
    Cantidad, PrecioUnitario, Descuento, Importe
)
VALUES
(
    @IdIngresoManualStock, @IdProducto, @CodigoProducto, @IdUnidadMedida,
    @Cantidad, 0, 0, 0
);", conexion, tx);
            detalleCmd.Parameters.Add("@IdIngresoManualStock", SqlDbType.Int).Value = idIngreso;
            detalleCmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = detalle.IdProducto;
            detalleCmd.Parameters.Add("@CodigoProducto", SqlDbType.VarChar, 50).Value = codigo;
            detalleCmd.Parameters.Add("@IdUnidadMedida", SqlDbType.Int).Value = idUnidadMedida;
            detalleCmd.Parameters.Add("@Cantidad", SqlDbType.Decimal).Value = detalle.Cantidad;
            await detalleCmd.ExecuteNonQueryAsync();

            decimal anterior = 0m;
            await using SqlCommand asegurarStock = new(@"
IF NOT EXISTS (SELECT 1 FROM dbo.StockProductosAlmacen WHERE IdProducto = @IdProducto AND IdAlmacen = @IdAlmacen)
    INSERT INTO dbo.StockProductosAlmacen (IdProducto, IdAlmacen, StockActual) VALUES (@IdProducto, @IdAlmacen, 0);
SELECT StockActual FROM dbo.StockProductosAlmacen WITH (UPDLOCK, HOLDLOCK) WHERE IdProducto = @IdProducto AND IdAlmacen = @IdAlmacen;", conexion, tx);
            asegurarStock.Parameters.Add("@IdProducto", SqlDbType.Int).Value = detalle.IdProducto;
            asegurarStock.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = request.IdAlmacen;
            anterior = Convert.ToDecimal(await asegurarStock.ExecuteScalarAsync());
            decimal resultante = anterior + detalle.Cantidad;

            await using SqlCommand actualizarStock = new(@"
UPDATE dbo.StockProductosAlmacen
SET StockActual = @Resultante, FechaActualizacion = GETDATE()
WHERE IdProducto = @IdProducto AND IdAlmacen = @IdAlmacen;

IF EXISTS (SELECT 1 FROM dbo.StockProductos WHERE IdProducto = @IdProducto)
    UPDATE dbo.StockProductos SET StockActual = StockActual + @Cantidad, FechaActualizacion = GETDATE() WHERE IdProducto = @IdProducto;
ELSE
    INSERT INTO dbo.StockProductos (IdProducto, StockActual) VALUES (@IdProducto, @Cantidad);

INSERT INTO dbo.KardexProductos
(
    TipoMovimiento, IdIngresoManualStock, IdProducto, IdAlmacen,
    StockAnterior, Cantidad, StockResultante, UsuarioResponsable, FechaMovimiento, Observacion
)
VALUES
(
    'INGRESO_MANUAL_STOCK', @IdIngresoManualStock, @IdProducto, @IdAlmacen,
    @Anterior, @Cantidad, @Resultante, @Usuario, GETDATE(), 'Ingreso manual desde Android'
);", conexion, tx);
            actualizarStock.Parameters.Add("@IdIngresoManualStock", SqlDbType.Int).Value = idIngreso;
            actualizarStock.Parameters.Add("@IdProducto", SqlDbType.Int).Value = detalle.IdProducto;
            actualizarStock.Parameters.Add("@IdAlmacen", SqlDbType.Int).Value = request.IdAlmacen;
            actualizarStock.Parameters.Add("@Anterior", SqlDbType.Decimal).Value = anterior;
            actualizarStock.Parameters.Add("@Cantidad", SqlDbType.Decimal).Value = detalle.Cantidad;
            actualizarStock.Parameters.Add("@Resultante", SqlDbType.Decimal).Value = resultante;
            actualizarStock.Parameters.Add("@Usuario", SqlDbType.VarChar, 100).Value = usuario;
            await actualizarStock.ExecuteNonQueryAsync();

            total += detalle.Cantidad;
        }

        await tx.CommitAsync();
        return Results.Ok(new
        {
            mensaje = "Stock manual ingresado correctamente.",
            idIngresoManualStock = idIngreso,
            numeroDocumento,
            cantidadTotal = total
        });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.Problem(title: "No se pudo ingresar el stock manual.", detail: ex.Message);
    }
});

app.MapGet("/api/fichas-tecnicas/{codigoProducto}/info", async (string codigoProducto) =>
{
    FichaDocumentoApi? ficha = await ObtenerFichaAsync(connectionString, rutaFichas, codigoProducto);
    return ficha == null
        ? Results.NotFound(new
        {
            mensaje = "Ficha técnica no existe.",
            codigoProducto,
            codigoModelo = CodigoModeloProducto.Obtener(codigoProducto)
        })
        : Results.Ok(new
        {
            codigoProducto,
            ficha.CodigoModelo,
            ficha.NombreArchivo,
            ficha.Version,
            ficha.FechaRegistro,
            disponible = File.Exists(ficha.RutaCompleta),
            url = $"/api/fichas-tecnicas/{Uri.EscapeDataString(codigoProducto)}"
        });
});

app.MapGet("/api/fichas-tecnicas/{codigoProducto}", async (string codigoProducto) =>
{
    FichaDocumentoApi? ficha = await ObtenerFichaAsync(connectionString, rutaFichas, codigoProducto);
    if (ficha == null || !File.Exists(ficha.RutaCompleta))
    {
        return Results.NotFound(new
        {
            mensaje = "Ficha técnica no existe.",
            codigoProducto,
            codigoModelo = CodigoModeloProducto.Obtener(codigoProducto)
        });
    }

    return Results.File(
        ficha.RutaCompleta,
        contentType: "application/pdf",
        fileDownloadName: ficha.NombreArchivo,
        enableRangeProcessing: true);
});

app.Run();

static async Task<FichaDocumentoApi?> ObtenerFichaAsync(
    string connectionString,
    string rutaBase,
    string codigoProducto)
{
    string codigoModelo = CodigoModeloProducto.Obtener(codigoProducto);
    if (string.IsNullOrWhiteSpace(codigoModelo))
        return null;

    const string sql = @"
SELECT TOP (1)
    CodigoModelo,
    NombreArchivo,
    RutaRelativa,
    Version,
    FechaRegistro
FROM dbo.FichaTecnicaDocumento
WHERE CodigoModelo = @CodigoModelo
  AND Estado = 1
ORDER BY Version DESC, IdFichaTecnicaDocumento DESC;";

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    cmd.Parameters.Add("@CodigoModelo", SqlDbType.VarChar, 40).Value = codigoModelo;
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return null;

    string rutaRelativa = dr["RutaRelativa"]?.ToString() ?? string.Empty;
    string rutaCompleta = Path.IsPathRooted(rutaRelativa)
        ? rutaRelativa
        : Path.Combine(rutaBase, rutaRelativa);

    return new FichaDocumentoApi(
        dr["CodigoModelo"]?.ToString() ?? codigoModelo,
        dr["NombreArchivo"]?.ToString() ?? $"{codigoModelo}.pdf",
        rutaCompleta,
        Convert.ToInt32(dr["Version"]),
        Convert.ToDateTime(dr["FechaRegistro"]));
}

static DataTable CrearTablaLanzamiento(IEnumerable<OrdenTrabajoLanzamientoDetalleApiRequest> detalles)
{
    DataTable tabla = new();
    tabla.Columns.Add("IdDetalleOT", typeof(int));
    tabla.Columns.Add("CantidadLanzada", typeof(decimal));
    tabla.Columns.Add("Motivo", typeof(string));
    tabla.Columns.Add("Observacion", typeof(string));

    foreach (OrdenTrabajoLanzamientoDetalleApiRequest detalle in detalles)
    {
        tabla.Rows.Add(detalle.IdDetalleOT, detalle.CantidadLanzada, detalle.Motivo ?? string.Empty, detalle.Observacion ?? string.Empty);
    }

    return tabla;
}

static DataTable CrearTablaPlanificacion(IEnumerable<OrdenTrabajoPlanificacionApiRequest> detalles)
{
    DataTable tabla = new();
    tabla.Columns.Add("IdOrdenCompraInternaDetalle", typeof(int));
    tabla.Columns.Add("CantidadPlanificada", typeof(decimal));

    foreach (OrdenTrabajoPlanificacionApiRequest detalle in detalles)
    {
        tabla.Rows.Add(detalle.IdOrdenCompraInternaDetalle, detalle.CantidadPlanificada);
    }

    return tabla;
}

static DataTable CrearTablaGuiaInterna(IEnumerable<GuiaInternaDetalleApiRequest> detalles)
{
    DataTable tabla = new();
    tabla.Columns.Add("IdOrdenCompraInternaDetalle", typeof(int));
    tabla.Columns.Add("CantidadDespachar", typeof(decimal));
    tabla.Columns.Add("Observacion", typeof(string));

    foreach (GuiaInternaDetalleApiRequest detalle in detalles)
    {
        tabla.Rows.Add(detalle.IdOrdenCompraInternaDetalle, detalle.CantidadDespachar, detalle.Observacion ?? string.Empty);
    }

    return tabla;
}

static DataTable CrearTablaGuiaInternaManual(IEnumerable<GuiaInternaManualDetalleApiRequest> detalles)
{
    DataTable tabla = new();
    tabla.Columns.Add("IdProducto", typeof(int));
    tabla.Columns.Add("CantidadDespachar", typeof(decimal));
    tabla.Columns.Add("Observacion", typeof(string));

    foreach (GuiaInternaManualDetalleApiRequest detalle in detalles)
    {
        tabla.Rows.Add(detalle.IdProducto, detalle.CantidadDespachar, detalle.Observacion ?? string.Empty);
    }

    return tabla;
}

static object MapearGuiaInternaCabecera(SqlDataReader dr) => new
{
    idGuiaInterna = Convert.ToInt32(dr["IdGuiaInterna"]),
    numeroGuia = dr["NumeroGuia"]?.ToString() ?? string.Empty,
    origen = dr["Origen"]?.ToString() ?? string.Empty,
    idOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
    idCliente = dr["IdCliente"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["IdCliente"]),
    numeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
    numeroProforma = dr["NumeroProforma"]?.ToString() ?? string.Empty,
    ordenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
    fechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
    idAlmacen = Convert.ToInt32(dr["IdAlmacen"]),
    nombreAlmacen = dr["NombreAlmacen"]?.ToString() ?? string.Empty,
    rucEmisor = dr["RucEmisor"]?.ToString() ?? string.Empty,
    empresaEmisora = dr["EmpresaEmisora"]?.ToString() ?? string.Empty,
    rucDestino = dr["RucDestino"]?.ToString() ?? string.Empty,
    empresaDestino = dr["EmpresaDestino"]?.ToString() ?? string.Empty,
    usuarioEmisor = dr["UsuarioEmisor"]?.ToString() ?? string.Empty,
    usuarioAutorizador = dr["UsuarioAutorizador"]?.ToString() ?? string.Empty,
    observacion = dr["Observacion"]?.ToString() ?? string.Empty,
    motivoEmisionManual = dr["MotivoEmisionManual"]?.ToString() ?? string.Empty,
    estado = dr["Estado"]?.ToString() ?? string.Empty,
    usuarioAnulacion = dr["UsuarioAnulacion"]?.ToString() ?? string.Empty,
    fechaAnulacion = dr["FechaAnulacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["FechaAnulacion"]),
    motivoAnulacion = dr["MotivoAnulacion"]?.ToString() ?? string.Empty,
    fechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
};

static object MapearGuiaInternaDetalle(SqlDataReader dr) => new
{
    idOrdenCompraInternaDetalle = Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
    idProducto = Convert.ToInt32(dr["IdProducto"]),
    codigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
    nombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
    idUnidadMedida = Convert.ToInt32(dr["IdUnidadMedida"]),
    nombreUnidad = dr["NombreUnidad"]?.ToString() ?? string.Empty,
    cantidadRequerida = Convert.ToDecimal(dr["CantidadRequerida"]),
    cantidadEntregada = Convert.ToDecimal(dr["CantidadEntregada"]),
    cantidadPendiente = Convert.ToDecimal(dr["CantidadPendiente"]),
    stockActual = Convert.ToDecimal(dr["StockActual"]),
    precioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
    cantidadDespachar = Convert.ToDecimal(dr["CantidadSugerida"]),
    observacion = dr["Observacion"]?.ToString() ?? string.Empty
};

static async Task<List<OtValidacionProductoApi>> ValidarOtInsumosAsync(SqlConnection conexion, int idOrdenCompraInterna)
{
    List<OtValidacionProductoApi> productos = [];
    await using SqlCommand cmd = new("USP_PRO_OT_VALIDAR_INSUMOS", conexion) { CommandType = CommandType.StoredProcedure };
    cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = idOrdenCompraInterna;
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    while (await dr.ReadAsync())
    {
        productos.Add(new OtValidacionProductoApi(
            Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
            Convert.ToInt32(dr["IdProducto"]),
            LeerString(dr, "CodigoProducto"),
            LeerString(dr, "NombreProducto"),
            LeerString(dr, "Observacion"),
            Convert.ToDecimal(dr["CantidadRequerida"]),
            dr["IdFichaTecnica"] == DBNull.Value ? null : Convert.ToInt32(dr["IdFichaTecnica"]),
            Convert.ToDecimal(dr["StockAlmacen"]),
            Convert.ToDecimal(dr["StockCorte"]),
            Convert.ToDecimal(dr["StockConfeccion"]),
            Convert.ToDecimal(dr["StockAcabado"]),
            Convert.ToDecimal(dr["StockTotal"]),
            Convert.ToDecimal(dr["Deficit"]),
            LeerString(dr, "EstadoInsumos")));
    }

    return productos;
}

static bool OtValidacionOk(OtValidacionProductoApi producto) =>
    producto.IdFichaTecnica.HasValue
    && producto.EstadoInsumos.Equals("Completo para producir", StringComparison.OrdinalIgnoreCase)
    && producto.Deficit <= 0;

static string LeerString(SqlDataReader dr, string columna)
{
    for (int i = 0; i < dr.FieldCount; i++)
    {
        if (dr.GetName(i).Equals(columna, StringComparison.OrdinalIgnoreCase))
            return dr.IsDBNull(i) ? string.Empty : dr.GetValue(i)?.ToString() ?? string.Empty;
    }
    return string.Empty;
}

static decimal LeerDecimal(SqlDataReader dr, string columna, decimal valorPredeterminado = 0)
{
    for (int i = 0; i < dr.FieldCount; i++)
    {
        if (dr.GetName(i).Equals(columna, StringComparison.OrdinalIgnoreCase))
            return dr.IsDBNull(i) ? valorPredeterminado : Convert.ToDecimal(dr.GetValue(i));
    }
    return valorPredeterminado;
}

static async Task<int> ObtenerIdGuiaInternaPorNumeroAsync(string connectionString, string numeroGuia)
{
    if (string.IsNullOrWhiteSpace(numeroGuia))
        return 0;

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new("SELECT TOP (1) IdGuiaInterna FROM dbo.GuiasInternas WHERE NumeroGuia = @NumeroGuia", conexion);
    cmd.Parameters.Add("@NumeroGuia", SqlDbType.VarChar, 30).Value = numeroGuia;
    await conexion.OpenAsync();
    object? value = await cmd.ExecuteScalarAsync();
    return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
}

static async Task<EmpresaPdfInfo> ObtenerEmpresaPdfAsync(string connectionString)
{
    const string sql = @"
SELECT TOP (1) Ruc, Nombre, Telefono, Correo, Direccion
FROM dbo.Empresas
WHERE Estado = 1
ORDER BY EsPredeterminada DESC, IdEmpresa ASC;";

    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    await conexion.OpenAsync();
    await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
    if (!await dr.ReadAsync())
        return new EmpresaPdfInfo("CorexProd", string.Empty, string.Empty, string.Empty, string.Empty);

    return new EmpresaPdfInfo(
        dr["Nombre"]?.ToString() ?? "CorexProd",
        dr["Ruc"]?.ToString() ?? string.Empty,
        dr["Direccion"]?.ToString() ?? string.Empty,
        dr["Telefono"]?.ToString() ?? string.Empty,
        dr["Correo"]?.ToString() ?? string.Empty);
}

static GuiaInternaPdfCabecera LeerGuiaInternaPdfCabecera(SqlDataReader dr) => new(
    Convert.ToInt32(dr["IdGuiaInterna"]),
    LeerString(dr, "NumeroGuia"),
    LeerString(dr, "Origen"),
    LeerString(dr, "NumeroOci"),
    LeerString(dr, "NumeroProforma"),
    LeerString(dr, "OrdenCompraCliente"),
    Convert.ToDateTime(dr["FechaEmision"]),
    LeerString(dr, "NombreAlmacen"),
    LeerString(dr, "RucEmisor"),
    LeerString(dr, "EmpresaEmisora"),
    LeerString(dr, "RucDestino"),
    LeerString(dr, "EmpresaDestino"),
    LeerString(dr, "UsuarioEmisor"),
    LeerString(dr, "UsuarioAutorizador"),
    LeerString(dr, "Observacion"),
    LeerString(dr, "MotivoEmisionManual"),
    LeerString(dr, "Estado"),
    Convert.ToDateTime(dr["FechaRegistro"]));

static GuiaInternaPdfDetalle LeerGuiaInternaPdfDetalle(SqlDataReader dr) => new(
    LeerString(dr, "CodigoProducto"),
    LeerString(dr, "NombreProducto"),
    LeerDecimal(dr, "CantidadPendiente"),
    LeerDecimal(dr, "CantidadDespachar", LeerDecimal(dr, "CantidadSugerida")),
    LeerString(dr, "Observacion"));

static byte[] CrearGuiaInternaPdf(EmpresaPdfInfo empresa, GuiaInternaPdfCabecera guia, IReadOnlyList<GuiaInternaPdfDetalle> detalles)
{
    const double pageWidth = 595;
    const double pageHeight = 842;
    const double margin = 34;
    SimplePdf document = new();
    SimplePdfPage page = NuevaPaginaGuia(document, pageWidth, pageHeight, margin, empresa, guia, out double y);
    DibujarDatosGuia(page, margin, pageWidth, guia, ref y);
    DibujarCabeceraDetalleGuia(page, margin, pageWidth, ref y);

    foreach (GuiaInternaPdfDetalle detalle in detalles.Where(d => d.CantidadDespachar > 0))
    {
        double alto = Math.Max(23, 8 + DividirLineasPdf(ProductoConObservacionPdf(detalle), 62).Count * 10);
        if (y - alto < 155)
        {
            page = NuevaPaginaGuia(document, pageWidth, pageHeight, margin, empresa, guia, out y);
            DibujarCabeceraDetalleGuia(page, margin, pageWidth, ref y);
        }
        DibujarDetalleGuia(page, margin, pageWidth, detalle, ref y);
    }

    if (y < 145)
        page = NuevaPaginaGuia(document, pageWidth, pageHeight, margin, empresa, guia, out y);

    DibujarCierreGuia(page, margin, pageWidth, guia, ref y);
    return document.Save();
}

static SimplePdfPage NuevaPaginaGuia(SimplePdf document, double pageWidth, double pageHeight, double margin, EmpresaPdfInfo empresa, GuiaInternaPdfCabecera guia, out double y)
{
    SimplePdfPage page = document.AddPage(pageWidth, pageHeight);
    y = pageHeight - margin;
    page.Text(LimpiarPdf(empresa.Nombre).ToUpperInvariant(), margin, y, 13, true);
    page.Text($"RUC: {empresa.Ruc}", margin, y - 15, 9);
    page.Text(LimpiarPdf(empresa.Direccion), margin, y - 28, 8);
    page.Text(LimpiarPdf(string.Join(" - ", new[] { empresa.Telefono, empresa.Correo }.Where(x => !string.IsNullOrWhiteSpace(x)))), margin, y - 40, 8);
    double boxX = 405, boxY = y - 55, boxW = 156, boxH = 58;
    page.Rectangle(boxX, boxY, boxW, boxH);
    page.CenterText("GUIA INTERNA DE SALIDA", boxX + boxW / 2, boxY + 37, 10, true);
    page.CenterText(LimpiarPdf(guia.NumeroGuia), boxX + boxW / 2, boxY + 18, 12, true);
    y -= 66;
    if (guia.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
    {
        page.CenterText("GUIA ANULADA", pageWidth / 2, y, 24, true);
        y -= 28;
    }
    return page;
}

static void DibujarDatosGuia(SimplePdfPage page, double margin, double pageWidth, GuiaInternaPdfCabecera guia, ref double y)
{
    const double h = 19;
    double w = pageWidth - margin * 2, half = w / 2;
    string fechaHora = $"{guia.FechaEmision:dd/MM/yyyy} {guia.FechaRegistro:HH:mm}";
    FilaDatoGuia(page, margin, y, half, "FECHA Y HORA", fechaHora); FilaDatoGuia(page, margin + half, y, half, "ALMACEN", guia.NombreAlmacen); y -= h;
    FilaDatoGuia(page, margin, y, half, "CLIENTE", string.IsNullOrWhiteSpace(guia.EmpresaDestino) ? "No especificado" : guia.EmpresaDestino); FilaDatoGuia(page, margin + half, y, half, "DOCUMENTO", guia.RucDestino); y -= h;
    FilaDatoGuia(page, margin, y, half, "N. OCI", guia.NumeroOci); FilaDatoGuia(page, margin + half, y, half, "OC CLIENTE", guia.OrdenCompraCliente); y -= h;
    FilaDatoGuia(page, margin, y, half, "N. PROFORMA", guia.NumeroProforma); FilaDatoGuia(page, margin + half, y, half, "ORIGEN", guia.Origen); y -= h;
    FilaDatoGuia(page, margin, y, w, "MOTIVO SALIDA", guia.MotivoEmisionManual); y -= h;
    FilaDatoGuia(page, margin, y, half, "RESPONSABLE", guia.UsuarioEmisor); FilaDatoGuia(page, margin + half, y, half, "AUTORIZADO", guia.UsuarioAutorizador); y -= h + 14;
}

static void FilaDatoGuia(SimplePdfPage page, double x, double y, double width, string label, string value)
{
    page.Rectangle(x, y - 19, width, 19);
    page.Text(label, x + 5, y - 13, 7, true);
    page.Text(TruncarPdf(LimpiarPdf(string.IsNullOrWhiteSpace(value) ? "-" : value), 33), x + 78, y - 13, 8);
}

static void DibujarCabeceraDetalleGuia(SimplePdfPage page, double margin, double pageWidth, ref double y)
{
    double w = pageWidth - margin * 2;
    page.Rectangle(margin, y - 21, w, 21);
    page.Text("CODIGO", margin + 5, y - 14, 7, true);
    page.Text("PRODUCTO", margin + 70, y - 14, 7, true);
    page.RightText("CANTIDAD", margin + 432, y - 14, 7, true);
    page.Text("ESTADO", margin + 447, y - 14, 7, true);
    y -= 21;
}

static void DibujarDetalleGuia(SimplePdfPage page, double margin, double pageWidth, GuiaInternaPdfDetalle detalle, ref double y)
{
    List<string> lineas = DividirLineasPdf(ProductoConObservacionPdf(detalle), 62);
    double h = Math.Max(23, 8 + lineas.Count * 10);
    double w = pageWidth - margin * 2;
    page.Rectangle(margin, y - h, w, h);
    page.Text(TruncarPdf(LimpiarPdf(detalle.CodigoProducto), 12), margin + 5, y - 15, 7.5);
    for (int i = 0; i < lineas.Count; i++)
        page.Text(lineas[i], margin + 70, y - 15 - i * 10, 7.5);
    page.RightText(detalle.CantidadDespachar.ToString("N2"), margin + 432, y - 15, 7.5);
    page.Text(detalle.CantidadDespachar < detalle.CantidadPendiente ? "PARCIAL" : "COMPLETO", margin + 447, y - 15, 7, true);
    y -= h;
}

static void DibujarCierreGuia(SimplePdfPage page, double margin, double pageWidth, GuiaInternaPdfCabecera guia, ref double y)
{
    y -= 14; page.Text("OBSERVACIONES", margin, y, 8, true); y -= 13;
    page.Rectangle(margin, y - 34, pageWidth - margin * 2, 38);
    page.Text(TruncarPdf(LimpiarPdf(guia.Observacion), 105), margin + 7, y - 15, 8);
    y -= 82;
    double ancho = (pageWidth - margin * 2) / 3;
    page.Line(margin + 18, y, margin + ancho - 18, y);
    page.Line(margin + ancho + 18, y, margin + ancho * 2 - 18, y);
    page.Line(margin + ancho * 2 + 18, y, pageWidth - margin - 18, y);
    page.CenterText("ENTREGADO POR", margin + ancho / 2, y - 14, 8, true);
    page.CenterText("RECIBIDO POR", margin + ancho * 1.5, y - 14, 8, true);
    page.CenterText("AUTORIZADO POR", margin + ancho * 2.5, y - 14, 8, true);
}

static string ProductoConObservacionPdf(GuiaInternaPdfDetalle detalle) => string.IsNullOrWhiteSpace(detalle.Observacion)
    ? LimpiarPdf(detalle.NombreProducto)
    : LimpiarPdf($"{detalle.NombreProducto} ({detalle.Observacion.Trim()})");

static List<string> DividirLineasPdf(string texto, int maximo)
{
    List<string> lineas = [];
    string pendiente = LimpiarPdf(texto).Trim();
    while (pendiente.Length > maximo)
    {
        int corte = pendiente.LastIndexOf(' ', maximo);
        if (corte <= 0) corte = maximo;
        lineas.Add(pendiente[..corte].Trim());
        pendiente = pendiente[corte..].TrimStart();
    }
    lineas.Add(string.IsNullOrWhiteSpace(pendiente) ? "-" : pendiente);
    return lineas;
}

static string TruncarPdf(string value, int max) => value.Length <= max ? value : value[..Math.Max(0, max - 3)] + "...";

static string LimpiarPdf(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    Dictionary<char, char> reemplazos = new()
    {
        ['á'] = 'a', ['é'] = 'e', ['í'] = 'i', ['ó'] = 'o', ['ú'] = 'u',
        ['Á'] = 'A', ['É'] = 'E', ['Í'] = 'I', ['Ó'] = 'O', ['Ú'] = 'U',
        ['ñ'] = 'n', ['Ñ'] = 'N'
    };
    return new string(value.Select(c => reemplazos.TryGetValue(c, out char r) ? r : c <= 127 ? c : ' ').ToArray());
}

static async Task<decimal> ObtenerTotalProducidoOtAsync(string connectionString, int idOrdenTrabajo)
{
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(
        """
        SELECT ISNULL(SUM(CantidadProducida), 0)
        FROM dbo.OrdenTrabajoDetalle
        WHERE IdOrdenTrabajo = @IdOrdenTrabajo
        """,
        conexion);
    cmd.Parameters.Add("@IdOrdenTrabajo", SqlDbType.Int).Value = idOrdenTrabajo;
    await conexion.OpenAsync();
    return Convert.ToDecimal(await cmd.ExecuteScalarAsync());
}

static DataTable CrearTablaTransferencia(IEnumerable<OrdenTrabajoTransferenciaDetalleApiRequest> detalles)
{
    DataTable tabla = new();
    tabla.Columns.Add("IdDetalleOT", typeof(int));
    tabla.Columns.Add("Cantidad", typeof(decimal));

    foreach (OrdenTrabajoTransferenciaDetalleApiRequest detalle in detalles)
    {
        tabla.Rows.Add(detalle.IdDetalleOT, detalle.Cantidad);
    }

    return tabla;
}

static string CrearDetallesProformaXml(IEnumerable<ProformaGuardarDetalleApiRequest> detalles)
{
    System.Text.StringBuilder xml = new("<Detalles>");
    foreach (ProformaGuardarDetalleApiRequest detalle in detalles)
    {
        decimal importe = Math.Max(0, Math.Round((detalle.Cantidad * detalle.PrecioUnitario) - detalle.Descuento, 2));
        xml.Append("<Detalle ");
        xml.Append(CrearAtributoXml("IdProducto", detalle.IdProducto.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        xml.Append(CrearAtributoXml("Cantidad", detalle.Cantidad.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        xml.Append(CrearAtributoXml("PrecioUnitario", detalle.PrecioUnitario.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        xml.Append(CrearAtributoXml("Descuento", detalle.Descuento.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        xml.Append(CrearAtributoXml("Importe", importe.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        xml.Append(CrearAtributoXml("Observacion", detalle.Observacion?.Trim() ?? string.Empty));
        xml.Append("/>");
    }

    xml.Append("</Detalles>");
    return xml.ToString();
}

static string CrearAtributoXml(string nombre, string valor)
    => $"{nombre}=\"{System.Security.SecurityElement.Escape(valor) ?? string.Empty}\" ";

static async Task ConfigurarOpcionesInsertAsync(SqlConnection conexion)
{
    await using SqlCommand cmd = new(
        """
        SET ANSI_NULLS ON;
        SET ANSI_PADDING ON;
        SET ANSI_WARNINGS ON;
        SET ARITHABORT ON;
        SET CONCAT_NULL_YIELDS_NULL ON;
        SET QUOTED_IDENTIFIER ON;
        SET NUMERIC_ROUNDABORT OFF;
        """,
        conexion);

    await cmd.ExecuteNonQueryAsync();
}

internal sealed class SimplePdf
{
    private readonly List<SimplePdfPage> _pages = [];

    public SimplePdfPage AddPage(double width, double height)
    {
        SimplePdfPage page = new(width, height);
        _pages.Add(page);
        return page;
    }

    public byte[] Save()
    {
        int pageCount = _pages.Count;
        int objectCount = 4 + pageCount * 2;
        byte[][] objects = new byte[objectCount + 1][];
        int[] pageObjectIds = new int[pageCount];
        int[] contentObjectIds = new int[pageCount];
        int nextId = 5;

        for (int i = 0; i < pageCount; i++)
        {
            pageObjectIds[i] = nextId++;
            contentObjectIds[i] = nextId++;
        }

        string kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
        objects[1] = AsciiObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
        objects[2] = AsciiObject(2, $"<< /Type /Pages /Count {pageCount} /Kids [{kids}] >>");
        objects[3] = AsciiObject(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        objects[4] = AsciiObject(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        for (int i = 0; i < pageCount; i++)
        {
            SimplePdfPage page = _pages[i];
            objects[pageObjectIds[i]] = AsciiObject(pageObjectIds[i],
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(page.Width)} {N(page.Height)}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectIds[i]} 0 R >>");
            objects[contentObjectIds[i]] = StreamObject(contentObjectIds[i], page.Content);
        }

        using MemoryStream stream = new();
        WriteAscii(stream, "%PDF-1.4\n");
        long[] offsets = new long[objectCount + 1];
        for (int id = 1; id <= objectCount; id++)
        {
            offsets[id] = stream.Position;
            stream.Write(objects[id]);
        }

        long xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objectCount + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        for (int id = 1; id <= objectCount; id++)
            WriteAscii(stream, $"{offsets[id]:0000000000} 00000 n \n");
        WriteAscii(stream, $"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();
    }

    private static byte[] AsciiObject(int id, string body) => Encoding.ASCII.GetBytes($"{id} 0 obj\n{body}\nendobj\n");
    private static byte[] StreamObject(int id, string content)
    {
        byte[] contentBytes = Encoding.ASCII.GetBytes(content);
        byte[] header = Encoding.ASCII.GetBytes($"{id} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
        byte[] footer = Encoding.ASCII.GetBytes("endstream\nendobj\n");
        using MemoryStream stream = new();
        stream.Write(header);
        stream.Write(contentBytes);
        stream.Write(footer);
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value) => stream.Write(Encoding.ASCII.GetBytes(value));
    internal static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}

internal sealed class SimplePdfPage(double width, double height)
{
    private readonly StringBuilder _content = new();

    public double Width { get; } = width;
    public double Height { get; } = height;
    public string Content => _content.ToString();

    public void Text(string text, double x, double y, double size, bool bold = false)
    {
        _content.Append("0 0 0 rg BT /");
        _content.Append(bold ? "F2" : "F1");
        _content.Append(' ');
        _content.Append(SimplePdf.N(size));
        _content.Append(" Tf ");
        _content.Append(SimplePdf.N(x));
        _content.Append(' ');
        _content.Append(SimplePdf.N(y));
        _content.Append(" Td ");
        _content.Append(PdfString(text));
        _content.Append(" Tj ET\n");
    }

    public void RightText(string text, double rightX, double y, double size, bool bold = false)
        => Text(text, rightX - ApproximateWidth(text, size, bold), y, size, bold);

    public void CenterText(string text, double centerX, double y, double size, bool bold = false)
        => Text(text, centerX - ApproximateWidth(text, size, bold) / 2, y, size, bold);

    public void Rectangle(double x, double y, double width, double height)
    {
        _content.Append("0 0 0 RG ");
        _content.Append(SimplePdf.N(x));
        _content.Append(' ');
        _content.Append(SimplePdf.N(y));
        _content.Append(' ');
        _content.Append(SimplePdf.N(width));
        _content.Append(' ');
        _content.Append(SimplePdf.N(height));
        _content.Append(" re S\n");
    }

    public void Line(double x1, double y1, double x2, double y2)
    {
        _content.Append("0 0 0 RG ");
        _content.Append(SimplePdf.N(x1));
        _content.Append(' ');
        _content.Append(SimplePdf.N(y1));
        _content.Append(" m ");
        _content.Append(SimplePdf.N(x2));
        _content.Append(' ');
        _content.Append(SimplePdf.N(y2));
        _content.Append(" l S\n");
    }

    private static double ApproximateWidth(string text, double size, bool bold)
    {
        double width = 0;
        foreach (char c in text ?? string.Empty)
        {
            if (char.IsDigit(c)) width += size * 0.556;
            else if (c is '.' or ',' or '/' or ' ' or '-') width += size * 0.278;
            else if (c is 'i' or 'l' or 'I' or 't' or 'f') width += size * 0.278;
            else if (char.IsUpper(c) || c is 'w' or 'm') width += size * 0.722;
            else width += size * 0.556;
        }
        return bold ? width * 1.05 : width;
    }

    private static string PdfString(string text)
    {
        StringBuilder builder = new("(");
        foreach (char c in text ?? string.Empty)
        {
            if (c is '(' or ')' or '\\')
                builder.Append('\\');
            builder.Append(c <= 127 ? c : ' ');
        }
        builder.Append(')');
        return builder.ToString();
    }
}

internal sealed record EmpresaPdfInfo(string Nombre, string Ruc, string Direccion, string Telefono, string Correo);

internal sealed record GuiaInternaPdfCabecera(
    int IdGuiaInterna,
    string NumeroGuia,
    string Origen,
    string NumeroOci,
    string NumeroProforma,
    string OrdenCompraCliente,
    DateTime FechaEmision,
    string NombreAlmacen,
    string RucEmisor,
    string EmpresaEmisora,
    string RucDestino,
    string EmpresaDestino,
    string UsuarioEmisor,
    string UsuarioAutorizador,
    string Observacion,
    string MotivoEmisionManual,
    string Estado,
    DateTime FechaRegistro);

internal sealed record GuiaInternaPdfDetalle(
    string CodigoProducto,
    string NombreProducto,
    decimal CantidadPendiente,
    decimal CantidadDespachar,
    string Observacion);

internal sealed record FichaDocumentoApi(
    string CodigoModelo,
    string NombreArchivo,
    string RutaCompleta,
    int Version,
    DateTime FechaRegistro);

internal sealed record LoginRequest(string? Usuario, string? Clave);

internal sealed record LoginUserResponse(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    int IdRol,
    string NombreRol);

internal sealed record LoginResponse(
    LoginUserResponse Usuario,
    IReadOnlyList<string> Menus,
    DateTime FechaHora,
    string Mensaje);

internal sealed record IngresoManualApiRequest(
    int IdProveedor,
    int IdTipoDocumento,
    int IdAlmacen,
    string? Observacion,
    string? Usuario,
    List<IngresoManualDetalleApiRequest> Detalles);

internal sealed record IngresoManualDetalleApiRequest(
    int IdProducto,
    decimal Cantidad);

internal sealed record ProformaGuardarApiRequest(
    int IdCliente,
    DateTime FechaVencimiento,
    string? OrdenCompraCliente,
    string? Observacion,
    decimal IgvPorcentaje,
    string CondicionTributaria,
    string? Usuario,
    List<ProformaGuardarDetalleApiRequest> Detalles);

internal sealed record ProformaGuardarDetalleApiRequest(
    int IdProducto,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    string? Observacion);

internal sealed record DocumentoAccionApiRequest(
    string? Usuario,
    string? Motivo);

internal sealed record OrdenTrabajoLanzarApiRequest(
    int IdUsuarioSesion,
    int IdUsuarioAutoriza,
    List<OrdenTrabajoLanzamientoDetalleApiRequest> Detalles);

internal sealed record OrdenTrabajoPlanificacionApiRequest(
    int IdOrdenCompraInternaDetalle,
    decimal CantidadPlanificada);

internal sealed record OrdenTrabajoKardexApi(
    string CodigoProducto,
    string NombreProducto,
    decimal Cantidad,
    string Almacen,
    DateTime FechaMovimiento,
    string Usuario);

internal sealed record OrdenTrabajoMovimientoApi(
    DateTime FechaHora,
    string CodigoProducto,
    string NombreProducto,
    string Origen,
    string Destino,
    decimal Cantidad,
    string Accion,
    string Usuario,
    string Observacion);

internal sealed record OtValidacionProductoApi(
    int IdOrdenCompraInternaDetalle,
    int IdProducto,
    string CodigoProducto,
    string NombreProducto,
    string Observacion,
    decimal CantidadRequerida,
    int? IdFichaTecnica,
    decimal StockAlmacen,
    decimal StockCorte,
    decimal StockConfeccion,
    decimal StockAcabado,
    decimal StockTotal,
    decimal Deficit,
    string EstadoInsumos);

internal sealed record OtValidacionInsumoApi(
    int IdInsumo,
    string CodigoInsumo,
    string NombreInsumo,
    string UnidadMedida,
    decimal ConsumoUnitario,
    decimal CantidadProduccion,
    decimal CantidadNecesaria,
    decimal StockActual,
    decimal StockProyectado,
    decimal CantidadFaltante,
    string Estado);

internal sealed record GuiaInternaDetalleApiRequest(
    int IdOrdenCompraInternaDetalle,
    decimal CantidadDespachar,
    string? Observacion);

internal sealed record GuiaInternaOciEmitirApiRequest(
    int IdAlmacen,
    DateTime FechaEmision,
    string? UsuarioEmisor,
    string? UsuarioAutorizador,
    string? Observacion,
    List<GuiaInternaOciDetalleApiRequest> Detalles);

internal sealed record GuiaInternaOciDetalleApiRequest(
    int IdOrdenCompraInternaDetalle,
    decimal CantidadDespachar,
    string? Observacion);

internal sealed record GuiaInternaManualApiRequest(
    int IdAlmacen,
    DateTime FechaEmision,
    int? IdCliente,
    string MotivoEmisionManual,
    string? Observacion,
    string? UsuarioEmisor,
    string? UsuarioAutorizador,
    List<GuiaInternaManualDetalleApiRequest> Detalles);

internal sealed record GuiaInternaManualDetalleApiRequest(
    int IdProducto,
    decimal CantidadDespachar,
    string? Observacion);

internal sealed record OrdenTrabajoLanzamientoDetalleApiRequest(
    int IdDetalleOT,
    decimal CantidadLanzada,
    string? Motivo,
    string? Observacion);

internal sealed record OrdenTrabajoTransferirApiRequest(
    int IdAreaProduccion,
    int IdUsuarioSesion,
    int IdUsuarioAutoriza,
    bool EsTerminacion,
    string? Observacion,
    List<OrdenTrabajoTransferenciaDetalleApiRequest> Detalles);

internal sealed record OrdenTrabajoTransferenciaDetalleApiRequest(
    int IdDetalleOT,
    decimal Cantidad);

internal sealed record OrdenTrabajoMermaApiRequest(
    long IdDetalleArea,
    decimal Cantidad,
    string? Motivo,
    string? Observacion,
    int IdUsuarioSesion,
    int IdUsuarioAutoriza);
