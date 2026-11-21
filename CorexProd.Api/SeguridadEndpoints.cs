using Microsoft.Data.SqlClient;
using BCrypt.Net;

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

        app.MapGet("/api/seguridad/cargos", async () =>
            Results.Ok(await ConsultarAsync(connectionString, """
                SELECT IdCargo, NombreCargo FROM dbo.Cargos WHERE Estado=1 ORDER BY NombreCargo;
                """, dr => new CargoMovilApi(dr.GetInt32(0), dr.GetString(1))));

        app.MapPost("/api/seguridad/empleados", async (EmpleadoGuardarApi request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido)
                || string.IsNullOrWhiteSpace(request.NumeroDocumento) || request.IdCargo <= 0)
                return Results.BadRequest(new { mensaje = "Nombre, apellido, documento y cargo son obligatorios." });
            int id = await EjecutarAsync(connectionString, """
                IF @IdEmpleado=0
                BEGIN
                    INSERT dbo.Empleados (TipoDocumento, NumeroDocumento, Nombre, Apellido, Sexo, Telefono, Email, Direccion, IdCargo, FechaNacimiento, FechaRegistro, Estado)
                    VALUES (@TipoDocumento,@NumeroDocumento,@Nombre,@Apellido,@Sexo,@Telefono,@Email,@Direccion,@IdCargo,@FechaNacimiento,GETDATE(),1);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    UPDATE dbo.Empleados SET TipoDocumento=@TipoDocumento,NumeroDocumento=@NumeroDocumento,Nombre=@Nombre,Apellido=@Apellido,
                        Sexo=@Sexo,Telefono=@Telefono,Email=@Email,Direccion=@Direccion,IdCargo=@IdCargo,FechaNacimiento=@FechaNacimiento,Estado=@Estado
                    WHERE IdEmpleado=@IdEmpleado;
                    SELECT @IdEmpleado;
                END
                """, c =>
            {
                c.Parameters.AddWithValue("@IdEmpleado", request.IdEmpleado);
                c.Parameters.AddWithValue("@TipoDocumento", request.TipoDocumento?.Trim() ?? "DNI");
                c.Parameters.AddWithValue("@NumeroDocumento", request.NumeroDocumento.Trim());
                c.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                c.Parameters.AddWithValue("@Apellido", request.Apellido.Trim());
                c.Parameters.AddWithValue("@Sexo", (object?)request.Sexo?.Trim() ?? DBNull.Value);
                c.Parameters.AddWithValue("@Telefono", (object?)request.Telefono?.Trim() ?? DBNull.Value);
                c.Parameters.AddWithValue("@Email", (object?)request.Email?.Trim() ?? DBNull.Value);
                c.Parameters.AddWithValue("@Direccion", (object?)request.Direccion?.Trim() ?? DBNull.Value);
                c.Parameters.AddWithValue("@IdCargo", request.IdCargo);
                c.Parameters.AddWithValue("@FechaNacimiento", request.FechaNacimiento.HasValue ? request.FechaNacimiento.Value : DBNull.Value);
                c.Parameters.AddWithValue("@Estado", request.Estado);
            });
            return Results.Ok(new { id, mensaje = request.IdEmpleado == 0 ? "Empleado registrado correctamente." : "Empleado actualizado correctamente." });
        });

        app.MapPost("/api/seguridad/usuarios", async (UsuarioGuardarApi request) =>
        {
            if (request.IdEmpleado <= 0 || request.IdRol <= 0 || string.IsNullOrWhiteSpace(request.NombreUsuario)
                || (request.IdUsuario == 0 && string.IsNullOrWhiteSpace(request.Clave)))
                return Results.BadRequest(new { mensaje = "Empleado, rol, usuario y clave son obligatorios." });
            string? hash = string.IsNullOrWhiteSpace(request.Clave) ? null : BCrypt.Net.BCrypt.HashPassword(request.Clave.Trim());
            int id = await EjecutarAsync(connectionString, """
                IF @IdUsuario=0
                BEGIN
                    INSERT dbo.Usuarios (IdEmpleado,NombreUsuario,Clave,IdRol,FechaRegistro,Estado)
                    VALUES (@IdEmpleado,@NombreUsuario,@Clave,@IdRol,GETDATE(),1);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    UPDATE dbo.Usuarios SET IdEmpleado=@IdEmpleado,NombreUsuario=@NombreUsuario,IdRol=@IdRol,Estado=@Estado
                    WHERE IdUsuario=@IdUsuario;
                    IF @Clave IS NOT NULL UPDATE dbo.Usuarios SET Clave=@Clave WHERE IdUsuario=@IdUsuario;
                    SELECT @IdUsuario;
                END
                """, c =>
            {
                c.Parameters.AddWithValue("@IdUsuario", request.IdUsuario);
                c.Parameters.AddWithValue("@IdEmpleado", request.IdEmpleado);
                c.Parameters.AddWithValue("@NombreUsuario", request.NombreUsuario.Trim());
                c.Parameters.AddWithValue("@Clave", (object?)hash ?? DBNull.Value);
                c.Parameters.AddWithValue("@IdRol", request.IdRol);
                c.Parameters.AddWithValue("@Estado", request.Estado);
            });
            return Results.Ok(new { id, mensaje = request.IdUsuario == 0 ? "Usuario registrado correctamente." : "Usuario actualizado correctamente." });
        });

        app.MapPost("/api/seguridad/roles", async (RolGuardarApi request) =>
        {
            if (string.IsNullOrWhiteSpace(request.NombreRol))
                return Results.BadRequest(new { mensaje = "El nombre del rol es obligatorio." });
            int id = await EjecutarAsync(connectionString, """
                IF @IdRol = 0
                BEGIN
                    INSERT dbo.Roles (NombreRol, Estado, FechaRegistro)
                    VALUES (@NombreRol, @Estado, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    UPDATE dbo.Roles SET NombreRol=@NombreRol, Estado=@Estado WHERE IdRol=@IdRol;
                    SELECT @IdRol;
                END
                """, c =>
            {
                c.Parameters.AddWithValue("@IdRol", request.IdRol);
                c.Parameters.AddWithValue("@NombreRol", request.NombreRol.Trim());
                c.Parameters.AddWithValue("@Estado", request.Estado);
            });
            return Results.Ok(new { id, mensaje = request.IdRol == 0 ? "Rol registrado correctamente." : "Rol actualizado correctamente." });
        });

        app.MapPost("/api/seguridad/parametros", async (ParametroGuardarApi request) =>
        {
            if (string.IsNullOrWhiteSpace(request.CodigoParametro) || string.IsNullOrWhiteSpace(request.NombreParametro))
                return Results.BadRequest(new { mensaje = "Código y nombre son obligatorios." });
            int id = await EjecutarAsync(connectionString, """
                IF @IdParametro = 0
                BEGIN
                    INSERT dbo.Parametros (CodigoParametro, NombreParametro, ValorParametro, Descripcion, Estado, FechaRegistro)
                    VALUES (@Codigo, @Nombre, @Valor, @Descripcion, @Estado, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    UPDATE dbo.Parametros SET CodigoParametro=@Codigo, NombreParametro=@Nombre,
                        ValorParametro=@Valor, Descripcion=@Descripcion, Estado=@Estado
                    WHERE IdParametro=@IdParametro;
                    SELECT @IdParametro;
                END
                """, c =>
            {
                c.Parameters.AddWithValue("@IdParametro", request.IdParametro);
                c.Parameters.AddWithValue("@Codigo", request.CodigoParametro.Trim());
                c.Parameters.AddWithValue("@Nombre", request.NombreParametro.Trim());
                c.Parameters.AddWithValue("@Valor", request.ValorParametro?.Trim() ?? string.Empty);
                c.Parameters.AddWithValue("@Descripcion", (object?)request.Descripcion?.Trim() ?? DBNull.Value);
                c.Parameters.AddWithValue("@Estado", request.Estado);
            });
            return Results.Ok(new { id, mensaje = request.IdParametro == 0 ? "Parámetro registrado correctamente." : "Parámetro actualizado correctamente." });
        });

        app.MapGet("/api/seguridad/roles/{idRol:int}/permisos", async (int idRol) =>
            Results.Ok(await ConsultarAsync(connectionString, """
                SELECT m.IdMenu, m.NombreMenu, m.IdMenuPadre, m.Orden,
                       CAST(ISNULL(pm.PuedeVer, 0) AS bit) AS PuedeVer
                FROM dbo.Menu m
                LEFT JOIN dbo.PermisosMenu pm ON pm.IdMenu = m.IdMenu AND pm.IdRol = @IdRol
                WHERE m.Estado = 1
                ORDER BY CASE WHEN m.IdMenuPadre IS NULL THEN m.Orden ELSE 0 END,
                         m.IdMenuPadre, m.Orden, m.NombreMenu;
                """, dr => new PermisoRolMovilApi(
                    dr.GetInt32(0), dr.GetString(1), dr.IsDBNull(2) ? null : dr.GetInt32(2),
                    dr.GetInt32(3), dr.GetBoolean(4)), new("@IdRol", idRol))));

        app.MapPost("/api/seguridad/roles/{idRol:int}/permisos", async (int idRol, List<PermisoRolGuardarApi> permisos) =>
        {
            await using SqlConnection cn = new(connectionString);
            await cn.OpenAsync();
            await using SqlTransaction tx = (SqlTransaction)await cn.BeginTransactionAsync();
            try
            {
                foreach (PermisoRolGuardarApi permiso in permisos)
                {
                    await using SqlCommand cmd = new("""
                        IF EXISTS (SELECT 1 FROM dbo.PermisosMenu WHERE IdRol=@IdRol AND IdMenu=@IdMenu)
                            UPDATE dbo.PermisosMenu SET PuedeVer=@PuedeVer WHERE IdRol=@IdRol AND IdMenu=@IdMenu;
                        ELSE
                            INSERT dbo.PermisosMenu (IdRol, IdMenu, PuedeVer) VALUES (@IdRol, @IdMenu, @PuedeVer);
                        """, cn, tx);
                    cmd.Parameters.AddWithValue("@IdRol", idRol);
                    cmd.Parameters.AddWithValue("@IdMenu", permiso.IdMenu);
                    cmd.Parameters.AddWithValue("@PuedeVer", permiso.PuedeVer);
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
                return Results.Ok(new { mensaje = "Permisos actualizados correctamente." });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    private static async Task<List<T>> ConsultarAsync<T>(string connectionString, string sql, Func<SqlDataReader, T> map, params SqlParameter[] parametros)
    {
        List<T> resultado = [];
        await using SqlConnection cn = new(connectionString);
        await cn.OpenAsync();
        await using SqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddRange(parametros);
        await using SqlDataReader dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
            resultado.Add(map(dr));
        return resultado;
    }

    private static async Task<int> EjecutarAsync(string connectionString, string sql, Action<SqlCommand> configurar)
    {
        await using SqlConnection cn = new(connectionString);
        await cn.OpenAsync();
        await using SqlCommand cmd = new(sql, cn);
        configurar(cmd);
        object? value = await cmd.ExecuteScalarAsync();
        return value is null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
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
public sealed record RolGuardarApi(int IdRol, string NombreRol, bool Estado);
public sealed record ParametroGuardarApi(int IdParametro, string CodigoParametro, string NombreParametro,
    string ValorParametro, string? Descripcion, bool Estado);
public sealed record PermisoRolMovilApi(int IdMenu, string NombreMenu, int? IdMenuPadre, int Orden, bool PuedeVer);
public sealed record PermisoRolGuardarApi(int IdMenu, bool PuedeVer);
public sealed record CargoMovilApi(int IdCargo, string NombreCargo);
public sealed record EmpleadoGuardarApi(int IdEmpleado, string? TipoDocumento, string NumeroDocumento,
    string Nombre, string Apellido, string? Sexo, string? Telefono, string? Email, string? Direccion,
    int IdCargo, DateTime? FechaNacimiento, bool Estado);
public sealed record UsuarioGuardarApi(int IdUsuario, int IdEmpleado, string NombreUsuario,
    string? Clave, int IdRol, bool Estado);
