using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class MenuSistemaDatos
    {
        public List<MenuSistema> Listar()
        {
            List<MenuSistema> lista = [];

            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            SincronizarMenusPredeterminados(cn);

            using SqlCommand cmd = new("""
                SELECT
                    m.IdMenu,
                    m.NombreMenu,
                    m.IdMenuPadre,
                    ISNULL(p.NombreMenu, '') AS NombrePadre,
                    m.Orden,
                    m.Estado
                FROM dbo.Menu m
                LEFT JOIN dbo.Menu p ON p.IdMenu = m.IdMenuPadre
                ORDER BY
                    CASE WHEN m.IdMenuPadre IS NULL THEN m.Orden ELSE p.Orden END,
                    CASE WHEN m.IdMenuPadre IS NULL THEN 0 ELSE 1 END,
                    m.Orden,
                    m.NombreMenu;
                """, cn);

            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new MenuSistema
                {
                    IdMenu = Convert.ToInt32(dr["IdMenu"]),
                    NombreMenu = dr["NombreMenu"]?.ToString() ?? string.Empty,
                    IdMenuPadre = dr["IdMenuPadre"] == DBNull.Value ? null : Convert.ToInt32(dr["IdMenuPadre"]),
                    NombrePadre = dr["NombrePadre"]?.ToString() ?? string.Empty,
                    Orden = Convert.ToInt32(dr["Orden"]),
                    Estado = Convert.ToBoolean(dr["Estado"])
                });
            }

            return lista;
        }

        public void SincronizarMenusPredeterminados()
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            SincronizarMenusPredeterminados(cn);
        }

        public void GuardarOrdenes(IEnumerable<MenuSistema> menus)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();

            using SqlTransaction transaccion = cn.BeginTransaction();

            try
            {
                foreach (MenuSistema menu in menus)
                {
                    using SqlCommand cmd = new("""
                        UPDATE dbo.Menu
                        SET Orden = @Orden,
                            Estado = @Estado
                        WHERE IdMenu = @IdMenu;
                        """, cn, transaccion);

                    cmd.Parameters.Add("@IdMenu", SqlDbType.Int).Value = menu.IdMenu;
                    cmd.Parameters.Add("@Orden", SqlDbType.Int).Value = menu.Orden;
                    cmd.Parameters.Add("@Estado", SqlDbType.Bit).Value = menu.Estado;
                    cmd.ExecuteNonQuery();
                }

                transaccion.Commit();
            }
            catch
            {
                transaccion.Rollback();
                throw;
            }
        }

        private static void SincronizarMenusPredeterminados(SqlConnection cn)
        {
            using SqlCommand cmd = new("""
                DECLARE @MenuBase TABLE
                (
                    NombreMenu NVARCHAR(100) NOT NULL,
                    NombrePadre NVARCHAR(100) NULL,
                    Orden INT NOT NULL,
                    Estado BIT NOT NULL
                );

                INSERT INTO @MenuBase (NombreMenu, NombrePadre, Orden, Estado)
                VALUES
                    (N'Inicio', NULL, 1, 1),
                    (N'Ventas', NULL, 2, 1),
                    (N'Producción', NULL, 3, 1),
                    (N'Reportes', NULL, 4, 1),
                    (N'Almacén', NULL, 5, 1),
                    (N'Productos', NULL, 6, 1),
                    (N'Destajo y Pagos', NULL, 7, 0),
                    (N'Seguridad', NULL, 8, 1),
                    (N'Orden de Compra', N'Ventas', 1, 1),
                    (N'Orden de Trabajo', N'Ventas', 2, 1),
                    (N'Guía de Salida', N'Ventas', 3, 1),
                    (N'Unidades de Medida', N'Almacén', 1, 1),
                    (N'Categorías de Insumos', N'Almacén', 2, 1),
                    (N'Insumos', N'Almacén', 3, 1),
                    (N'Entrada Manual de Productos', N'Almacén', 4, 1),
                    (N'Entrada Manual de Insumos', N'Almacén', 5, 1),
                    (N'Ficha Técnica', N'Almacén', 6, 1),
                    (N'Supercategorías', N'Productos', 1, 1),
                    (N'Categorías de Productos', N'Productos', 2, 1),
                    (N'Productos', N'Productos', 3, 1),
                    (N'Áreas de Producción', N'Producción', 1, 1),
                    (N'Seguimiento OT', N'Producción', 2, 1),
                    (N'Stock Productos', N'Reportes', 1, 1),
                    (N'Stock Reservas', N'Reportes', 2, 1),
                    (N'Stock Insumos', N'Reportes', 3, 1),
                    (N'Kardex Productos', N'Reportes', 4, 1),
                    (N'Kardex Insumos', N'Reportes', 5, 1),
                    (N'Estadísticas', N'Reportes', 6, 1),
                    (N'Panel de Destajo', N'Destajo y Pagos', 1, 0),
                    (N'Periodos de Pago', N'Destajo y Pagos', 2, 0),
                    (N'Movimientos Operativos', N'Destajo y Pagos', 3, 0),
                    (N'Prestamos y Cuotas', N'Destajo y Pagos', 4, 0),
                    (N'Lotes de Pago', N'Destajo y Pagos', 5, 0),
                    (N'Reportes de Pagos', N'Destajo y Pagos', 6, 0),
                    (N'Configuración', N'Destajo y Pagos', 7, 0),
                    (N'Roles', N'Seguridad', 1, 1),
                    (N'Cargos', N'Seguridad', 2, 1),
                    (N'Usuarios', N'Seguridad', 3, 1),
                    (N'Empleados', N'Seguridad', 4, 1),
                    (N'Empresa', N'Seguridad', 5, 1),
                    (N'Parámetros', N'Seguridad', 6, 1),
                    (N'Clientes', N'Seguridad', 7, 1),
                    (N'Proveedores', N'Seguridad', 8, 1),
                    (N'Series y Correlativos', N'Seguridad', 9, 1),
                    (N'Menú', N'Seguridad', 10, 1);

                DECLARE @IdVentas INT = (SELECT TOP (1) IdMenu FROM dbo.Menu WHERE NombreMenu = N'Ventas' AND IdMenuPadre IS NULL ORDER BY IdMenu);
                IF @IdVentas IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Orden de Compra' AND IdMenuPadre = @IdVentas)
                        UPDATE dbo.Menu SET NombreMenu = N'Orden de Compra' WHERE NombreMenu = N'OCI' AND IdMenuPadre = @IdVentas;

                    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Guía de Salida' AND IdMenuPadre = @IdVentas)
                        UPDATE dbo.Menu SET NombreMenu = N'Guía de Salida' WHERE NombreMenu = N'Guía Interna' AND IdMenuPadre = @IdVentas;
                END;

                DECLARE @IdAlmacen INT = (SELECT TOP (1) IdMenu FROM dbo.Menu WHERE NombreMenu = N'Almacén' AND IdMenuPadre IS NULL ORDER BY IdMenu);
                IF @IdAlmacen IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Entrada Manual de Productos' AND IdMenuPadre = @IdAlmacen)
                        UPDATE dbo.Menu SET NombreMenu = N'Entrada Manual de Productos' WHERE NombreMenu = N'Ingresos Manuales de Stock' AND IdMenuPadre = @IdAlmacen;

                    IF NOT EXISTS (SELECT 1 FROM dbo.Menu WHERE NombreMenu = N'Entrada Manual de Insumos' AND IdMenuPadre = @IdAlmacen)
                        UPDATE dbo.Menu SET NombreMenu = N'Entrada Manual de Insumos' WHERE NombreMenu = N'Ingresos de Stock de Insumos' AND IdMenuPadre = @IdAlmacen;
                END;

                INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado)
                SELECT b.NombreMenu, NULL, b.Orden, b.Estado
                FROM @MenuBase b
                WHERE b.NombrePadre IS NULL
                  AND NOT EXISTS (
                        SELECT 1
                        FROM dbo.Menu m
                        WHERE m.NombreMenu = b.NombreMenu
                          AND m.IdMenuPadre IS NULL
                  );

                INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado)
                SELECT b.NombreMenu, p.IdMenu, b.Orden, b.Estado
                FROM @MenuBase b
                INNER JOIN dbo.Menu p
                    ON p.NombreMenu = b.NombrePadre
                   AND p.IdMenuPadre IS NULL
                WHERE b.NombrePadre IS NOT NULL
                  AND NOT EXISTS (
                        SELECT 1
                        FROM dbo.Menu m
                        WHERE m.NombreMenu = b.NombreMenu
                          AND m.IdMenuPadre = p.IdMenu
                  );

                INSERT INTO dbo.PermisosMenu (IdRol, IdMenu, PuedeVer)
                SELECT
                    r.IdRol,
                    m.IdMenu,
                    CAST(
                        CASE
                            WHEN r.NombreRol IN (N'Administrador', N'SuperAdmin') THEN 1
                            WHEN m.IdMenuPadre IS NOT NULL AND ISNULL(pp.PuedeVer, 0) = 1 THEN 1
                            ELSE 0
                        END AS BIT
                    ) AS PuedeVer
                FROM dbo.Roles r
                INNER JOIN dbo.Menu m
                    ON EXISTS (
                        SELECT 1
                        FROM @MenuBase b
                        LEFT JOIN dbo.Menu p
                            ON p.NombreMenu = b.NombrePadre
                           AND p.IdMenuPadre IS NULL
                        WHERE b.NombreMenu = m.NombreMenu
                          AND (
                                (b.NombrePadre IS NULL AND m.IdMenuPadre IS NULL)
                                OR (b.NombrePadre IS NOT NULL AND m.IdMenuPadre = p.IdMenu)
                          )
                    )
                LEFT JOIN dbo.PermisosMenu pp
                    ON pp.IdRol = r.IdRol
                   AND pp.IdMenu = m.IdMenuPadre
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM dbo.PermisosMenu px
                    WHERE px.IdRol = r.IdRol
                      AND px.IdMenu = m.IdMenu
                );
                """, cn);

            cmd.ExecuteNonQuery();
        }
    }
}
