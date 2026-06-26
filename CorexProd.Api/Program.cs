using CorexProd.Entidad.Utilidades;
using Microsoft.Data.SqlClient;
using System.Data;

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
    O.TieneOrdenTrabajo
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
            tieneOrdenTrabajo = Convert.ToBoolean(dr["TieneOrdenTrabajo"])
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
        numeroProforma = dr["NumeroProforma"]?.ToString() ?? string.Empty,
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
        decimal avance = totalPlanificado <= 0 ? 0 : Math.Min(1, totalLanzado / totalPlanificado);

        items.Add(new
        {
            idOrdenTrabajo = Convert.ToInt32(dr["IdOrdenTrabajo"]),
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

internal sealed record OrdenTrabajoLanzarApiRequest(
    int IdUsuarioSesion,
    int IdUsuarioAutoriza,
    List<OrdenTrabajoLanzamientoDetalleApiRequest> Detalles);

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
