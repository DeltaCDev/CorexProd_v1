using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Security;
using System.Text;

namespace CorexProd.Datos.Datos
{
    public class OrdenCompraInternaDatos
    {
        public List<OrdenCompraInterna> Listar()
        {
            List<OrdenCompraInterna> lista = [];
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_OCI_LISTAR", conexion) { CommandType = CommandType.StoredProcedure };
            conexion.Open();

            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                    lista.Add(Mapear(dr));
            }

            if (lista.Exists(orden => orden.PuedeGenerarOt))
            {
                HashSet<int> idsConOtActiva = ObtenerIdsConOtActiva(conexion);
                foreach (OrdenCompraInterna orden in lista)
                {
                    if (orden.PuedeGenerarOt && idsConOtActiva.Contains(orden.IdOrdenCompraInterna))
                        orden.PuedeGenerarOt = false;
                }
            }

            return lista;
        }

        public OrdenCompraInterna? Obtener(int idOrdenCompraInterna)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_OCI_OBTENER", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", idOrdenCompraInterna);
            conexion.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read())
            {
                return null;
            }

            OrdenCompraInterna oci = Mapear(dr);
            if (oci.PuedeGenerarOt && TieneOtActiva(oci.IdOrdenCompraInterna))
                oci.PuedeGenerarOt = false;

            if (dr.NextResult())
            {
                while (dr.Read())
                {
                    oci.Detalles.Add(new OrdenCompraInternaDetalle
                    {
                        IdOrdenCompraInternaDetalle = Convert.ToInt32(dr["IdOrdenCompraInternaDetalle"]),
                        IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
                        IdProducto = Convert.ToInt32(dr["IdProducto"]),
                        CodigoProducto = dr["CodigoProducto"]?.ToString() ?? string.Empty,
                        NombreProducto = dr["NombreProducto"]?.ToString() ?? string.Empty,
                        Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                        StockActual = Convert.ToDecimal(dr["StockActual"]),
                        StockProcesoReservado = DecimalOpcional(dr, "StockProcesoReservado"),
                        StockProcesoReservadoDetalle = TextoOpcional(dr, "StockProcesoReservadoDetalle"),
                        CantidadDespachada = Convert.ToDecimal(dr["CantidadDespachada"]),
                        PrecioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
                        Descuento = Convert.ToDecimal(dr["Descuento"]),
                        Importe = Convert.ToDecimal(dr["Importe"]),
                        Observacion = dr["Observacion"]?.ToString() ?? string.Empty
                    });
                }
            }

            return oci;
        }

        public string Generar(int idProforma, string usuarioGenerador)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_OCI_GENERAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdProforma", idProforma);
            cmd.Parameters.AddWithValue("@UsuarioGenerador", usuarioGenerador);
            cmd.Parameters.Add(new SqlParameter("@IdGenerado", SqlDbType.Int) { Direction = ParameterDirection.Output });
            cmd.Parameters.Add(new SqlParameter("@NumeroOci", SqlDbType.VarChar, 40) { Direction = ParameterDirection.Output });
            cmd.Parameters.Add(new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output });
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(mensaje);
            conexion.Open();
            cmd.ExecuteNonQuery();
            return mensaje.Value?.ToString() ?? string.Empty;
        }

        public string ObtenerSiguienteNumero()
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                "SELECT ISNULL(MAX(TRY_CONVERT(INT, RIGHT(NumeroOci, 6))), 0) + 1 FROM dbo.OrdenesCompraInterna",
                conexion);
            conexion.Open();
            int correlativo = Convert.ToInt32(cmd.ExecuteScalar());
            return $"OC-{correlativo.ToString().PadLeft(6, '0')}";
        }

        public string GuardarDirecta(OrdenCompraInterna orden)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_ORDEN_COMPRA_GUARDAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@FechaEmision", orden.FechaEmision.Date);
            cmd.Parameters.AddWithValue("@OrdenCompraCliente", orden.OrdenCompraCliente);
            cmd.Parameters.AddWithValue("@IdCliente", orden.IdCliente);
            cmd.Parameters.AddWithValue("@Subtotal", orden.Subtotal);
            cmd.Parameters.AddWithValue("@Descuento", orden.Descuento);
            cmd.Parameters.AddWithValue("@Igv", orden.Igv);
            cmd.Parameters.AddWithValue("@IgvPorcentaje", orden.IgvPorcentaje);
            cmd.Parameters.AddWithValue("@CondicionTributaria", orden.CondicionTributaria);
            cmd.Parameters.AddWithValue("@Total", orden.Total);
            cmd.Parameters.AddWithValue("@DetallesXml", CrearDetallesXml(orden.Detalles));
            cmd.Parameters.AddWithValue("@UsuarioGenerador", orden.UsuarioGenerador);
            cmd.Parameters.Add(new SqlParameter("@IdGenerado", SqlDbType.Int) { Direction = ParameterDirection.Output });
            cmd.Parameters.Add(new SqlParameter("@NumeroOrden", SqlDbType.VarChar, 40) { Direction = ParameterDirection.Output });
            cmd.Parameters.Add(new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output });
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(mensaje);
            conexion.Open();
            ConfigurarOpcionesInsert(conexion);
            cmd.ExecuteNonQuery();
            return mensaje.Value?.ToString() ?? string.Empty;
        }

        public string ActualizarDirecta(OrdenCompraInterna orden)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            conexion.Open();
            ConfigurarOpcionesInsert(conexion);
            using SqlTransaction transaction = conexion.BeginTransaction();

            try
            {
                using (SqlCommand validar = new(
                    """
                    SELECT TOP (1)
                        Estado,
                        ISNULL(TieneGuiaSalida, 0) AS TieneGuiaSalida,
                        ISNULL(TieneOrdenTrabajo, 0) AS TieneOrdenTrabajo,
                        ISNULL(MotivoAnulacion, '') AS MotivoAnulacion,
                        FechaAnulacion
                    FROM dbo.OrdenesCompraInterna WITH (UPDLOCK, HOLDLOCK)
                    WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;
                    """,
                    conexion,
                    transaction))
                {
                    validar.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = orden.IdOrdenCompraInterna;
                    using SqlDataReader dr = validar.ExecuteReader();
                    if (!dr.Read())
                        return "No se encontro la orden de compra.";

                    string estado = dr["Estado"]?.ToString()?.Trim().ToUpperInvariant() ?? string.Empty;
                    bool tieneAccion = Convert.ToBoolean(dr["TieneGuiaSalida"])
                        || Convert.ToBoolean(dr["TieneOrdenTrabajo"])
                        || !string.IsNullOrWhiteSpace(dr["MotivoAnulacion"]?.ToString())
                        || dr["FechaAnulacion"] != DBNull.Value;

                    if (estado is not ("PENDIENTE" or "EMITIDA" or "EMITIDO") || tieneAccion)
                        return "Solo se puede editar una OC pendiente sin acciones realizadas.";
                }

                using (SqlCommand validarDetalle = new(
                    """
                    IF EXISTS
                    (
                        SELECT 1
                        FROM dbo.OrdenCompraInternaDetalle
                        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
                          AND ISNULL(CantidadDespachada, 0) > 0
                    )
                        SELECT CAST(1 AS BIT);
                    ELSE
                        SELECT CAST(0 AS BIT);
                    """,
                    conexion,
                    transaction))
                {
                    validarDetalle.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = orden.IdOrdenCompraInterna;
                    if (Convert.ToBoolean(validarDetalle.ExecuteScalar()))
                        return "No se puede editar una OC con despachos registrados.";
                }

                using (SqlCommand actualizar = new(
                    """
                    UPDATE O
                    SET FechaEmision = @FechaEmision,
                        OrdenCompraCliente = @OrdenCompraCliente,
                        IdCliente = C.IdCliente,
                        NombreCliente = C.NombreRazonSocial,
                        Subtotal = @Subtotal,
                        Descuento = @Descuento,
                        Igv = @Igv,
                        IgvPorcentaje = @IgvPorcentaje,
                        CondicionTributaria = @CondicionTributaria,
                        Total = @Total,
                        UsuarioGenerador = @UsuarioGenerador
                    FROM dbo.OrdenesCompraInterna O
                    INNER JOIN dbo.Clientes C ON C.IdCliente = @IdCliente AND C.Estado = 1
                    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

                    DELETE FROM dbo.OrdenCompraInternaDetalle
                    WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;

                    INSERT INTO dbo.OrdenCompraInternaDetalle
                    (
                        IdOrdenCompraInterna, IdProducto, CodigoProducto, NombreProducto,
                        Cantidad, PrecioUnitario, Descuento, Importe, Observacion
                    )
                    SELECT
                        @IdOrdenCompraInterna,
                        P.IdProducto,
                        P.Codigo,
                        P.NombreProducto,
                        X.Cantidad,
                        X.PrecioUnitario,
                        X.Descuento,
                        X.Importe,
                        X.Observacion
                    FROM
                    (
                        SELECT
                            D.X.value('@IdProducto', 'INT') AS IdProducto,
                            D.X.value('@Cantidad', 'DECIMAL(18,2)') AS Cantidad,
                            D.X.value('@PrecioUnitario', 'DECIMAL(18,2)') AS PrecioUnitario,
                            D.X.value('@Descuento', 'DECIMAL(18,2)') AS Descuento,
                            D.X.value('@Importe', 'DECIMAL(18,2)') AS Importe,
                            D.X.value('@Observacion', 'VARCHAR(500)') AS Observacion
                        FROM @DetallesXml.nodes('/Detalles/Detalle') D(X)
                    ) X
                    INNER JOIN dbo.Productos P ON P.IdProducto = X.IdProducto
                    WHERE X.Cantidad > 0;
                    """,
                    conexion,
                    transaction))
                {
                    actualizar.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = orden.IdOrdenCompraInterna;
                    actualizar.Parameters.Add("@FechaEmision", SqlDbType.Date).Value = orden.FechaEmision.Date;
                    actualizar.Parameters.Add("@OrdenCompraCliente", SqlDbType.VarChar, 100).Value = orden.OrdenCompraCliente ?? string.Empty;
                    actualizar.Parameters.Add("@IdCliente", SqlDbType.Int).Value = orden.IdCliente;
                    actualizar.Parameters.Add("@Subtotal", SqlDbType.Decimal).Value = orden.Subtotal;
                    actualizar.Parameters.Add("@Descuento", SqlDbType.Decimal).Value = orden.Descuento;
                    actualizar.Parameters.Add("@Igv", SqlDbType.Decimal).Value = orden.Igv;
                    actualizar.Parameters.Add("@IgvPorcentaje", SqlDbType.Decimal).Value = orden.IgvPorcentaje;
                    actualizar.Parameters.Add("@CondicionTributaria", SqlDbType.VarChar, 50).Value = orden.CondicionTributaria ?? string.Empty;
                    actualizar.Parameters.Add("@Total", SqlDbType.Decimal).Value = orden.Total;
                    actualizar.Parameters.Add("@UsuarioGenerador", SqlDbType.VarChar, 80).Value = orden.UsuarioGenerador ?? "Sistema";
                    actualizar.Parameters.Add("@DetallesXml", SqlDbType.Xml).Value = CrearDetallesXml(orden.Detalles);
                    actualizar.ExecuteNonQuery();
                }

                transaction.Commit();
                return $"Orden de compra {orden.NumeroOci} actualizada correctamente.";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return ex.Message;
            }
        }

        public string Anular(int idOrdenCompraInterna, string motivoAnulacion, string usuarioAnulacion)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_VEN_OCI_ANULAR", conexion) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IdOrdenCompraInterna", idOrdenCompraInterna);
            cmd.Parameters.AddWithValue("@MotivoAnulacion", motivoAnulacion);
            cmd.Parameters.AddWithValue("@UsuarioAnulacion", usuarioAnulacion);
            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(mensaje);
            conexion.Open();
            cmd.ExecuteNonQuery();
            return mensaje.Value?.ToString() ?? string.Empty;
        }

        public bool TieneOtActiva(int idOrdenCompraInterna)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(
                """
                SELECT CAST(CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.OrdenTrabajo
                    WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
                      AND UPPER(REPLACE(Estado, ' ', '_')) IN ('PENDIENTE', 'EMITIDA', 'EN_PROCESO', 'PARCIAL')
                ) THEN 1 ELSE 0 END AS BIT);
                """,
                conexion);
            cmd.Parameters.Add("@IdOrdenCompraInterna", SqlDbType.Int).Value = idOrdenCompraInterna;
            conexion.Open();
            return Convert.ToBoolean(cmd.ExecuteScalar());
        }

        private static HashSet<int> ObtenerIdsConOtActiva(SqlConnection conexion)
        {
            HashSet<int> ids = [];
            using SqlCommand cmd = new(
                """
                SELECT DISTINCT IdOrdenCompraInterna
                FROM dbo.OrdenTrabajo
                WHERE IdOrdenCompraInterna IS NOT NULL
                  AND UPPER(REPLACE(LTRIM(RTRIM(Estado)), ' ', '_')) IN ('PENDIENTE', 'EMITIDA', 'EN_PROCESO', 'PROCESO', 'PARCIAL');
                """,
                conexion);
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                ids.Add(Convert.ToInt32(dr["IdOrdenCompraInterna"]));
            return ids;
        }

        private static OrdenCompraInterna Mapear(SqlDataReader dr)
        {
            return new OrdenCompraInterna
            {
                IdOrdenCompraInterna = Convert.ToInt32(dr["IdOrdenCompraInterna"]),
                NumeroOci = dr["NumeroOci"]?.ToString() ?? string.Empty,
                FechaEmision = Convert.ToDateTime(dr["FechaEmision"]),
                OrdenCompraCliente = dr["OrdenCompraCliente"]?.ToString() ?? string.Empty,
                IdCliente = Convert.ToInt32(dr["IdCliente"]),
                NombreCliente = dr["NombreCliente"]?.ToString() ?? string.Empty,
                Subtotal = Convert.ToDecimal(dr["Subtotal"]),
                Descuento = Convert.ToDecimal(dr["Descuento"]),
                Igv = Convert.ToDecimal(dr["Igv"]),
                IgvPorcentaje = Convert.ToDecimal(dr["IgvPorcentaje"]),
                CondicionTributaria = dr["CondicionTributaria"]?.ToString() ?? string.Empty,
                Total = Convert.ToDecimal(dr["Total"]),
                Estado = dr["Estado"]?.ToString() ?? string.Empty,
                UsuarioGenerador = dr["UsuarioGenerador"]?.ToString() ?? string.Empty,
                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                MotivoAnulacion = dr["MotivoAnulacion"]?.ToString() ?? string.Empty,
                UsuarioAnulacion = dr["UsuarioAnulacion"]?.ToString() ?? string.Empty,
                FechaAnulacion = dr["FechaAnulacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaAnulacion"]),
                TieneGuiaSalida = Convert.ToBoolean(dr["TieneGuiaSalida"]),
                TieneOrdenTrabajo = Convert.ToBoolean(dr["TieneOrdenTrabajo"]),
                PuedeGenerarOt = Convert.ToBoolean(dr["PuedeGenerarOt"]),
                PuedeGenerarGuiaSalida = Convert.ToBoolean(dr["PuedeGenerarGuiaSalida"])
            };
        }

        private static string CrearDetallesXml(List<OrdenCompraInternaDetalle> detalles)
        {
            StringBuilder xml = new("<Detalles>");
            foreach (OrdenCompraInternaDetalle detalle in detalles)
            {
                xml.Append("<Detalle ");
                xml.Append(CrearAtributo("IdProducto", detalle.IdProducto.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("Cantidad", detalle.Cantidad.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("PrecioUnitario", detalle.PrecioUnitario.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("Descuento", detalle.Descuento.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("Importe", detalle.Importe.ToString(CultureInfo.InvariantCulture)));
                xml.Append(CrearAtributo("Observacion", detalle.Observacion));
                xml.Append("/>");
            }
            xml.Append("</Detalles>");
            return xml.ToString();
        }

        private static string CrearAtributo(string nombre, string valor) =>
            $"{nombre}=\"{SecurityElement.Escape(valor) ?? string.Empty}\" ";

        private static decimal DecimalOpcional(SqlDataReader dr, string columna)
        {
            try
            {
                int ordinal = dr.GetOrdinal(columna);
                return dr.IsDBNull(ordinal) ? 0 : Convert.ToDecimal(dr.GetValue(ordinal));
            }
            catch (IndexOutOfRangeException)
            {
                return 0;
            }
        }

        private static string TextoOpcional(SqlDataReader dr, string columna)
        {
            try
            {
                int ordinal = dr.GetOrdinal(columna);
                return dr.IsDBNull(ordinal) ? string.Empty : dr.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return string.Empty;
            }
        }

        private static void ConfigurarOpcionesInsert(SqlConnection conexion)
        {
            using SqlCommand cmd = new(
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
            cmd.ExecuteNonQuery();
        }
    }
}
