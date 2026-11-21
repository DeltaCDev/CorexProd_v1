using Microsoft.Data.SqlClient;

namespace CorexProd.Api;

public static class SeguridadEndpoints
{
    public static void MapSeguridadEndpoints(this WebApplication app, string connectionString)
    {
        app.MapGet("/api/seguridad/parametros", async () =>
            Results.Ok(await ConsultarAsync(connectionString, """
                SELECT IdParametro, CodigoParametro, NombreParametro, ValorParametro,
                       ISNULL(Descripcion, '') AS Descripcion, Estado, FechaRegistro
                FROM dbo.Parametros ORDER BY NombreParametro;
                """, dr => new ParametroMovilApi(
                    dr.GetInt32(0), dr.GetString(1), dr.GetString(2), dr.GetString(3),
                    dr.GetString(4), dr.GetBoolean(5), dr.GetDateTime(6))));

        app.MapGet("/api/seguridad/roles", async () =>
            Results.Ok(await ConsultarAsync(connectionString, """
                SELECT IdRol, NombreRol, Estado, FechaRegistro
                FROM dbo.Roles ORDER BY NombreRol;
                """, dr => new RolMovilApi(
                    dr.GetInt32(0), dr.GetString(1), dr.GetBoolean(2), dr.GetDateTime(3))));

        app.MapGet("/api/seguridad/empleados", async () =>
            Results.Ok(await ConsultarAsync(connectionString, """
                SELECT e.IdEmpleado, e.TipoDocumento, e.NumeroDocumento, e.Nombre,
                       e.Apellido, ISNULL(e.Sexo, ''), ISNULL(e.Telefono, ''),
                       ISNULL(e.Email, ''), ISNULL(e.Direccion, ''), e.IdCargo,
                       ISNULL(c.NombreCargo, ''), e.FechaNacimiento, e.FechaRegistro, e.Estado
                FROM dbo.Empleados e
                LEFT JOIN dbo.Cargos c ON c.IdCargo = e.IdCargo
                ORDER BY e.Apellido, e.Nombre;
                """, dr => new EmpleadoMovilApi(
                    dr.GetInt32(0), dr.GetString(1), dr.GetString(2), dr.GetString(3),
                    dr.GetString(4), dr.GetString(5), dr.GetString(6), dr.GetString(7),
                    dr.GetString(8), dr.GetInt32(9), dr.GetString(10),
                    dr.IsDBNull(11) ? null : dr.GetDateTime(11), dr.GetDateTime(12), dr.GetBoolean(13))));

        app.MapGet("/api/seguridad/usuarios", async () =>
            Results.Ok(await ConsultarAsync(connectionString, """
                SELECT u.IdUsuario, u.IdEmpleado, CONCAT(e.Nombre, ' ', e.Apellido),
                       u.NombreUsuario, u.IdRol, ISNULL(r.NombreRol, ''), u.FechaRegistro, u.Estado
                FROM dbo.Usuarios u
                INNER JOIN dbo.Empleados e ON e.IdEmpleado = u.IdEmpleado
                LEFT JOIN dbo.Roles r ON r.IdRol = u.IdRol
                ORDER BY u.NombreUsuario;
                """, dr => new UsuarioMovilApi(
                    dr.GetInt32(0), dr.GetInt32(1), dr.GetString(2).Trim(), dr.GetString(3),
                    dr.GetInt32(4), dr.GetString(5), dr.GetDateTime(6), dr.GetBoolean(7))));
    }

    private static async Task<List<T>> ConsultarAsync<T>(string connectionString, string sql, Func<SqlDataReader, T> map)
    {
        List<T> resultado = [];
        await using SqlConnection cn = new(connectionString);
        await cn.OpenAsync();
        await using SqlCommand cmd = new(sql, cn);
        await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
            resultado.Add(map(dr));
        return resultado;
    }
}

public sealed record ParametroMovilApi(int IdParametro, string CodigoParametro, string NombreParametro,
    string ValorParametro, string Descripcion, bool Estado, DateTime FechaRegistro);
public sealed record RolMovilApi(int IdRol, string NombreRol, bool Estado, DateTime FechaRegistro);
public sealed record EmpleadoMovilApi(int IdEmpleado, string TipoDocumento, string NumeroDocumento,
    string Nombre, string Apellido, string Sexo, string Telefono, string Email, string Direccion,
    int IdCargo, string NombreCargo, DateTime? FechaNacimiento, DateTime FechaRegistro, bool Estado);
public sealed record UsuarioMovilApi(int IdUsuario, int IdEmpleado, string NombreCompleto,
    string NombreUsuario, int IdRol, string NombreRol, DateTime FechaRegistro, bool Estado);
