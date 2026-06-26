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

app.MapGet("/api/stock/productos", async (string? buscar) =>
{
    const string sql = @"
SELECT
    P.IdProducto,
    P.Codigo,
    P.NombreProducto,
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
  )
ORDER BY P.Codigo;";

    List<object> productos = [];
    await using SqlConnection conexion = new(connectionString);
    await using SqlCommand cmd = new(sql, conexion);
    cmd.Parameters.Add("@Buscar", SqlDbType.VarChar, 150).Value = buscar?.Trim() ?? string.Empty;
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
