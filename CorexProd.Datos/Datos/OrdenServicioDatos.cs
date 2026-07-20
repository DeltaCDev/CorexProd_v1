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
    public class OrdenServicioDatos
    {
        public List<TipoServicio> ListarTiposServicio(bool soloActivos = false)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);

            using SqlCommand cmd = new("""
                SELECT IdTipoServicio, Codigo, Nombre, Descripcion, RequiereEntrega, Estado
                FROM dbo.TiposServicio
                WHERE @SoloActivos = 0 OR Estado = 1
                ORDER BY Nombre;
                """, cn);
            cmd.Parameters.Add("@SoloActivos", SqlDbType.Bit).Value = soloActivos;

            List<TipoServicio> lista = [];
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearTipo(dr));
            return lista;
        }

        public string GuardarTipoServicio(TipoServicio tipo)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);

            using SqlCommand cmd = new("""
                IF @IdTipoServicio = 0
                BEGIN
                    IF EXISTS (SELECT 1 FROM dbo.TiposServicio WHERE Codigo = @Codigo)
                    BEGIN
                        SELECT 'Ya existe un tipo de servicio con ese codigo.';
                        RETURN;
                    END;

                    INSERT INTO dbo.TiposServicio (Codigo, Nombre, Descripcion, RequiereEntrega, Estado)
                    VALUES (@Codigo, @Nombre, @Descripcion, @RequiereEntrega, @Estado);
                    SELECT 'Tipo de servicio registrado correctamente.';
                    RETURN;
                END;

                UPDATE dbo.TiposServicio
                SET Codigo = @Codigo,
                    Nombre = @Nombre,
                    Descripcion = @Descripcion,
                    RequiereEntrega = @RequiereEntrega,
                    Estado = @Estado
                WHERE IdTipoServicio = @IdTipoServicio;
                SELECT 'Tipo de servicio actualizado correctamente.';
                """, cn);
            cmd.Parameters.Add("@IdTipoServicio", SqlDbType.Int).Value = tipo.IdTipoServicio;
            cmd.Parameters.Add("@Codigo", SqlDbType.VarChar, 20).Value = tipo.Codigo;
            cmd.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value = tipo.Nombre;
            cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar, 500).Value = tipo.Descripcion;
            cmd.Parameters.Add("@RequiereEntrega", SqlDbType.Bit).Value = tipo.RequiereEntrega;
            cmd.Parameters.Add("@Estado", SqlDbType.Bit).Value = tipo.Estado;
            return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        public List<OrdenServicio> Listar(string? buscar, string? estado)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);

            using SqlCommand cmd = new("""
                SELECT
                    O.IdOrdenServicio, O.NumeroOrden, O.Fecha, O.FechaComprometida,
                    O.IdProveedor, ISNULL(P.NombreRazonSocial, '') AS NombreProveedor,
                    ISNULL(P.NumeroDocumento, '') AS RucProveedor,
                    O.IdTipoServicio, T.Nombre AS TipoServicioNombre, T.RequiereEntrega,
                    O.Cliente, O.OciRelacionada, O.OtRelacionada, O.Responsable, O.FormaPago,
                    O.ObservacionesInternas, O.Observaciones, O.DistribucionFotosPdf, O.Subtotal, O.Igv, O.Total,
                    ISNULL(PG.TotalPagado, 0) AS TotalPagado,
                    O.Estado, O.EstadoServicio, O.EstadoPago, O.UsuarioRegistro, O.FechaRegistro, O.MotivoAnulacion
                FROM dbo.OrdenesServicio O
                INNER JOIN dbo.TiposServicio T ON T.IdTipoServicio = O.IdTipoServicio
                LEFT JOIN dbo.Proveedores P ON P.IdProveedor = O.IdProveedor
                OUTER APPLY
                (
                    SELECT SUM(Importe) AS TotalPagado
                    FROM dbo.OrdenServicioPagos
                    WHERE IdOrdenServicio = O.IdOrdenServicio
                ) PG
                WHERE (@Estado = '' OR O.Estado = @Estado)
                  AND (
                        @Buscar = ''
                        OR O.NumeroOrden LIKE '%' + @Buscar + '%'
                        OR ISNULL(P.NombreRazonSocial, '') LIKE '%' + @Buscar + '%'
                        OR T.Nombre LIKE '%' + @Buscar + '%'
                        OR O.Cliente LIKE '%' + @Buscar + '%'
                        OR O.OciRelacionada LIKE '%' + @Buscar + '%'
                        OR O.OtRelacionada LIKE '%' + @Buscar + '%'
                        OR O.Responsable LIKE '%' + @Buscar + '%'
                  )
                ORDER BY O.Fecha DESC, O.IdOrdenServicio DESC;
                """, cn);
            cmd.Parameters.Add("@Buscar", SqlDbType.VarChar, 120).Value = buscar?.Trim() ?? string.Empty;
            cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 40).Value = estado is null or "" or "Todos" ? string.Empty : estado;

            List<OrdenServicio> lista = [];
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                lista.Add(MapearOrden(dr));
            return lista;
        }

        public OrdenServicio? Obtener(int idOrdenServicio)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);

            OrdenServicio? orden;
            using (SqlCommand cmd = new("""
                SELECT
                    O.IdOrdenServicio, O.NumeroOrden, O.Fecha, O.FechaComprometida,
                    O.IdProveedor, ISNULL(P.NombreRazonSocial, '') AS NombreProveedor,
                    ISNULL(P.NumeroDocumento, '') AS RucProveedor,
                    O.IdTipoServicio, T.Nombre AS TipoServicioNombre, T.RequiereEntrega,
                    O.Cliente, O.OciRelacionada, O.OtRelacionada, O.Responsable, O.FormaPago,
                    O.ObservacionesInternas, O.Observaciones, O.DistribucionFotosPdf, O.Subtotal, O.Igv, O.Total,
                    ISNULL(PG.TotalPagado, 0) AS TotalPagado,
                    O.Estado, O.EstadoServicio, O.EstadoPago, O.UsuarioRegistro, O.FechaRegistro, O.MotivoAnulacion
                FROM dbo.OrdenesServicio O
                INNER JOIN dbo.TiposServicio T ON T.IdTipoServicio = O.IdTipoServicio
                LEFT JOIN dbo.Proveedores P ON P.IdProveedor = O.IdProveedor
                OUTER APPLY
                (
                    SELECT SUM(Importe) AS TotalPagado
                    FROM dbo.OrdenServicioPagos
                    WHERE IdOrdenServicio = O.IdOrdenServicio
                ) PG
                WHERE O.IdOrdenServicio = @IdOrdenServicio;
                """, cn))
            {
                cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
                using SqlDataReader dr = cmd.ExecuteReader();
                orden = dr.Read() ? MapearOrden(dr) : null;
            }

            if (orden == null)
                return null;

            using (SqlCommand cmd = new("""
                SELECT IdOrdenServicioDetalle, IdOrdenServicio, IdProducto, Producto, Descripcion,
                       Cantidad, Unidad, PrecioUnitario, Total, Observaciones
                FROM dbo.OrdenServicioDetalle
                WHERE IdOrdenServicio = @IdOrdenServicio
                ORDER BY IdOrdenServicioDetalle;
                """, cn))
            {
                cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
                using SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                    orden.Detalles.Add(MapearDetalle(dr));
            }

            using (SqlCommand cmd = new("""
                SELECT IdOrdenServicioPago, IdOrdenServicio, Fecha, TipoPago, Importe,
                       MedioPago, NumeroOperacion, Observacion, UsuarioRegistro
                FROM dbo.OrdenServicioPagos
                WHERE IdOrdenServicio = @IdOrdenServicio
                ORDER BY Fecha, IdOrdenServicioPago;
                """, cn))
            {
                cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
                using SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                    orden.Pagos.Add(MapearPago(dr));
            }

            CargarMovimientos(cn, orden);
            CargarFotos(cn, orden);
            CargarHistorial(cn, orden);

            return orden;
        }

        public List<OrdenServicioHistorial> ListarHistorial(int idOrdenServicio)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);

            OrdenServicio orden = new() { IdOrdenServicio = idOrdenServicio };
            CargarHistorial(cn, orden);
            return orden.Historial;
        }

        public string Guardar(OrdenServicio orden)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);
            using SqlTransaction tx = cn.BeginTransaction();

            try
            {
                int id = orden.IdOrdenServicio;
                if (id == 0)
                {
                    string numero = ObtenerSiguienteNumero(cn, tx, orden.Fecha);
                    using SqlCommand cmd = new("""
                        INSERT INTO dbo.OrdenesServicio
                        (NumeroOrden, Fecha, FechaComprometida, IdProveedor, IdTipoServicio, Cliente,
                         OciRelacionada, OtRelacionada, Responsable, FormaPago, ObservacionesInternas, Observaciones, DistribucionFotosPdf,
                         Subtotal, Igv, Total, Estado, EstadoServicio, EstadoPago, UsuarioRegistro)
                        OUTPUT INSERTED.IdOrdenServicio
                        VALUES
                        (@NumeroOrden, @Fecha, @FechaComprometida, @IdProveedor, @IdTipoServicio, @Cliente,
                         @OciRelacionada, @OtRelacionada, @Responsable, @FormaPago, @ObservacionesInternas, @Observaciones, @DistribucionFotosPdf,
                         @Subtotal, @Igv, @Total, 'Borrador', 'Borrador', @EstadoPago, @UsuarioRegistro);
                        """, cn, tx);
                    AgregarParametrosOrden(cmd, orden);
                    cmd.Parameters.Add("@NumeroOrden", SqlDbType.VarChar, 30).Value = numero;
                    cmd.Parameters.Add("@EstadoPago", SqlDbType.VarChar, 40).Value = orden.ACuenta >= orden.Total && orden.Total > 0 ? "Pagada" : "Pendiente";
                    id = Convert.ToInt32(cmd.ExecuteScalar());
                    orden.IdOrdenServicio = id;
                    orden.NumeroOrden = numero;
                    RegistrarHistorial(cn, tx, id, orden.UsuarioRegistro, "Creacion");
                    if (orden.ACuenta > 0)
                    {
                        InsertarPagoInicial(cn, tx, id, orden);
                        RegistrarHistorial(cn, tx, id, orden.UsuarioRegistro, "Registro de pago a cuenta");
                    }
                }
                else
                {
                    using SqlCommand validar = new("SELECT Estado FROM dbo.OrdenesServicio WHERE IdOrdenServicio = @Id;", cn, tx);
                    validar.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    string estado = validar.ExecuteScalar()?.ToString() ?? string.Empty;
                    if (!estado.Equals("Borrador", StringComparison.OrdinalIgnoreCase))
                        return "Solo se puede editar una orden en estado Borrador.";

                    using SqlCommand cmd = new("""
                        UPDATE dbo.OrdenesServicio
                        SET Fecha = @Fecha,
                            FechaComprometida = @FechaComprometida,
                            IdProveedor = @IdProveedor,
                            IdTipoServicio = @IdTipoServicio,
                            Cliente = @Cliente,
                            OciRelacionada = @OciRelacionada,
                            OtRelacionada = @OtRelacionada,
                            Responsable = @Responsable,
                            FormaPago = @FormaPago,
                            ObservacionesInternas = @ObservacionesInternas,
                            Observaciones = @Observaciones,
                            DistribucionFotosPdf = @DistribucionFotosPdf,
                            Subtotal = @Subtotal,
                            Igv = @Igv,
                            Total = @Total,
                            EstadoPago = CASE
                                WHEN @Total <= ISNULL((SELECT SUM(Importe) FROM dbo.OrdenServicioPagos WHERE IdOrdenServicio = @IdOrdenServicio), 0) THEN 'Pagada'
                                ELSE 'Pendiente'
                            END
                        WHERE IdOrdenServicio = @IdOrdenServicio;
                        """, cn, tx);
                    cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = id;
                    AgregarParametrosOrden(cmd, orden);
                    cmd.ExecuteNonQuery();

                    using SqlCommand limpiar = new("DELETE FROM dbo.OrdenServicioDetalle WHERE IdOrdenServicio = @Id;", cn, tx);
                    limpiar.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    limpiar.ExecuteNonQuery();
                    RegistrarHistorial(cn, tx, id, orden.UsuarioRegistro, "Modificacion");
                }

                InsertarDetalles(cn, tx, id, orden.Detalles);
                tx.Commit();
                return "Orden de servicio guardada correctamente.";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return ex.Message;
            }
        }

        public string Aprobar(int idOrdenServicio, string usuario) => CambiarEstado(idOrdenServicio, "Borrador", "Aprobada", usuario, "Aprobacion");

        public string Anular(int idOrdenServicio, string usuario, string motivo)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);
            using SqlCommand cmd = new("""
                UPDATE dbo.OrdenesServicio
                SET Estado = 'Anulada',
                    EstadoServicio = 'Anulada',
                    MotivoAnulacion = @Motivo
                WHERE IdOrdenServicio = @IdOrdenServicio
                  AND Estado <> 'Anulada'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.OrdenServicioMovimientos
                      WHERE IdOrdenServicio = @IdOrdenServicio
                  );
                SELECT @@ROWCOUNT;
                """, cn);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
            cmd.Parameters.Add("@Motivo", SqlDbType.VarChar, 500).Value = motivo;
            int filas = Convert.ToInt32(cmd.ExecuteScalar());
            if (filas == 0)
                return "No se pudo anular la orden seleccionada.";
            RegistrarHistorial(cn, null, idOrdenServicio, usuario, "Anulacion");
            return "Orden de servicio anulada correctamente.";
        }

        public string RegistrarPago(OrdenServicioPago pago)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);
            using SqlTransaction tx = cn.BeginTransaction();
            try
            {
                decimal saldo;
                using (SqlCommand saldoCmd = new("""
                    SELECT O.Total - ISNULL((SELECT SUM(Importe) FROM dbo.OrdenServicioPagos WHERE IdOrdenServicio = O.IdOrdenServicio), 0)
                    FROM dbo.OrdenesServicio O
                    WHERE O.IdOrdenServicio = @Id AND O.Estado <> 'Anulada';
                    """, cn, tx))
                {
                    saldoCmd.Parameters.Add("@Id", SqlDbType.Int).Value = pago.IdOrdenServicio;
                    object? valor = saldoCmd.ExecuteScalar();
                    if (valor == null || valor == DBNull.Value)
                        return "No se encontro la orden de servicio.";
                    saldo = Convert.ToDecimal(valor);
                }

                if (pago.Importe <= 0 || pago.Importe > saldo)
                    return "El importe del pago debe ser mayor a cero y no exceder el saldo pendiente.";

                using SqlCommand cmd = new("""
                    INSERT INTO dbo.OrdenServicioPagos
                    (IdOrdenServicio, Fecha, TipoPago, Importe, MedioPago, NumeroOperacion, Observacion, UsuarioRegistro)
                    VALUES (@IdOrdenServicio, @Fecha, @TipoPago, @Importe, @MedioPago, @NumeroOperacion, @Observacion, @UsuarioRegistro);

                    UPDATE O
                    SET EstadoPago = CASE WHEN O.Total <= ISNULL(P.TotalPagado, 0) THEN 'Pagada' ELSE 'Pendiente' END,
                        Estado = CASE
                            WHEN O.Total <= ISNULL(P.TotalPagado, 0) THEN 'Pagada'
                            WHEN O.Estado IN ('Aprobada', 'Pendiente de Pago') THEN 'Pendiente de Pago'
                            ELSE O.Estado
                        END
                    FROM dbo.OrdenesServicio O
                    OUTER APPLY
                    (
                        SELECT SUM(Importe) AS TotalPagado
                        FROM dbo.OrdenServicioPagos
                        WHERE IdOrdenServicio = O.IdOrdenServicio
                    ) P
                    WHERE O.IdOrdenServicio = @IdOrdenServicio;
                    """, cn, tx);
                cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = pago.IdOrdenServicio;
                cmd.Parameters.Add("@Fecha", SqlDbType.Date).Value = pago.Fecha.Date;
                cmd.Parameters.Add("@TipoPago", SqlDbType.VarChar, 40).Value = pago.TipoPago;
                cmd.Parameters.Add("@Importe", SqlDbType.Decimal).Value = pago.Importe;
                cmd.Parameters.Add("@MedioPago", SqlDbType.VarChar, 60).Value = pago.MedioPago;
                cmd.Parameters.Add("@NumeroOperacion", SqlDbType.VarChar, 80).Value = pago.NumeroOperacion;
                cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = pago.Observacion;
                cmd.Parameters.Add("@UsuarioRegistro", SqlDbType.VarChar, 80).Value = pago.UsuarioRegistro;
                cmd.ExecuteNonQuery();

                RegistrarHistorial(cn, tx, pago.IdOrdenServicio, pago.UsuarioRegistro, "Registro de pago");
                tx.Commit();
                return "Pago registrado correctamente.";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return ex.Message;
            }
        }

        public string Copiar(int idOrdenServicio, string usuario)
        {
            OrdenServicio? origen = Obtener(idOrdenServicio);
            if (origen == null)
                return "No se encontro la orden a copiar.";
            origen.IdOrdenServicio = 0;
            origen.NumeroOrden = string.Empty;
            origen.Estado = "Borrador";
            origen.UsuarioRegistro = usuario;
            origen.Pagos.Clear();
            origen.Entregas.Clear();
            origen.Recepciones.Clear();
            origen.Fotos.Clear();
            foreach (OrdenServicioDetalle detalle in origen.Detalles)
                detalle.IdOrdenServicioDetalle = 0;
            return Guardar(origen);
        }

        private string CambiarEstado(int idOrdenServicio, string estadoActual, string estadoNuevo, string usuario, string accion)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);
            using SqlCommand cmd = new("""
                UPDATE dbo.OrdenesServicio
                SET Estado = @EstadoNuevo,
                    EstadoServicio = @EstadoNuevo
                WHERE IdOrdenServicio = @IdOrdenServicio
                  AND Estado = @EstadoActual;
                SELECT @@ROWCOUNT;
                """, cn);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
            cmd.Parameters.Add("@EstadoActual", SqlDbType.VarChar, 40).Value = estadoActual;
            cmd.Parameters.Add("@EstadoNuevo", SqlDbType.VarChar, 40).Value = estadoNuevo;
            int filas = Convert.ToInt32(cmd.ExecuteScalar());
            if (filas == 0)
                return $"Solo se puede cambiar una orden en estado {estadoActual}.";
            RegistrarHistorial(cn, null, idOrdenServicio, usuario, accion);
            return "Estado actualizado correctamente.";
        }

        public List<OrdenServicioMovimiento> PrepararEntrega(int idOrdenServicio)
        {
            OrdenServicio? orden = Obtener(idOrdenServicio);
            if (orden == null)
                return [];

            return orden.Detalles.Select(detalle =>
            {
                decimal enviada = orden.Entregas
                    .Where(x => x.IdOrdenServicioDetalle == detalle.IdOrdenServicioDetalle)
                    .Sum(x => x.CantidadMovimiento);
                return new OrdenServicioMovimiento
                {
                    IdOrdenServicio = idOrdenServicio,
                    IdOrdenServicioDetalle = detalle.IdOrdenServicioDetalle,
                    TipoMovimiento = "Entrega",
                    Producto = detalle.Producto,
                    Descripcion = detalle.Descripcion,
                    Cantidad = detalle.Cantidad,
                    CantidadAnterior = enviada,
                    CantidadMovimiento = Math.Max(0, detalle.Cantidad - enviada),
                    CantidadPendiente = Math.Max(0, detalle.Cantidad - enviada),
                    Unidad = detalle.Unidad,
                    OtRelacionada = orden.OtRelacionada
                };
            }).ToList();
        }

        public List<OrdenServicioMovimiento> PrepararRecepcion(int idOrdenServicio)
        {
            OrdenServicio? orden = Obtener(idOrdenServicio);
            if (orden == null)
                return [];

            IEnumerable<OrdenServicioMovimiento> baseItems = orden.RequiereEntrega && orden.Entregas.Count > 0
                ? orden.Entregas
                    .GroupBy(x => x.IdOrdenServicioDetalle)
                    .Select(g => new OrdenServicioMovimiento
                    {
                        IdOrdenServicio = idOrdenServicio,
                        IdOrdenServicioDetalle = g.Key,
                        Producto = g.First().Producto,
                        Descripcion = g.First().Descripcion,
                        Unidad = g.First().Unidad,
                        Cantidad = g.Sum(x => x.CantidadMovimiento)
                    })
                : orden.Detalles.Select(d => new OrdenServicioMovimiento
                {
                    IdOrdenServicio = idOrdenServicio,
                    IdOrdenServicioDetalle = d.IdOrdenServicioDetalle,
                    Producto = d.Producto,
                    Descripcion = d.Descripcion,
                    Unidad = d.Unidad,
                    Cantidad = d.Cantidad
                });

            return baseItems.Select(item =>
            {
                decimal recibida = orden.Recepciones
                    .Where(x => x.IdOrdenServicioDetalle == item.IdOrdenServicioDetalle)
                    .Sum(x => x.CantidadMovimiento);
                item.TipoMovimiento = "Recepcion";
                item.CantidadAnterior = recibida;
                item.CantidadMovimiento = Math.Max(0, item.Cantidad - recibida);
                item.CantidadPendiente = Math.Max(0, item.Cantidad - recibida);
                return item;
            }).ToList();
        }

        public string RegistrarMovimientos(int idOrdenServicio, string tipoMovimiento, IEnumerable<OrdenServicioMovimiento> movimientos, string usuario)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);
            using SqlTransaction tx = cn.BeginTransaction();
            try
            {
                List<OrdenServicioMovimiento> validos = movimientos.Where(x => x.CantidadMovimiento > 0).ToList();
                if (validos.Count == 0)
                    return "Debe ingresar al menos una cantidad mayor a cero.";

                foreach (OrdenServicioMovimiento mov in validos)
                {
                    using SqlCommand cmd = new("""
                        INSERT INTO dbo.OrdenServicioMovimientos
                        (IdOrdenServicio, IdOrdenServicioDetalle, TipoMovimiento, Fecha, Producto, Descripcion,
                         Cantidad, CantidadAnterior, CantidadMovimiento, CantidadPendiente, Unidad, Observacion, OtRelacionada, UsuarioRegistro)
                        VALUES
                        (@IdOrdenServicio, @IdOrdenServicioDetalle, @TipoMovimiento, CAST(GETDATE() AS DATE), @Producto, @Descripcion,
                         @Cantidad, @CantidadAnterior, @CantidadMovimiento, @CantidadPendiente, @Unidad, @Observacion, @OtRelacionada, @UsuarioRegistro);
                        """, cn, tx);
                    cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
                    cmd.Parameters.Add("@IdOrdenServicioDetalle", SqlDbType.Int).Value = (object?)mov.IdOrdenServicioDetalle ?? DBNull.Value;
                    cmd.Parameters.Add("@TipoMovimiento", SqlDbType.VarChar, 20).Value = tipoMovimiento;
                    cmd.Parameters.Add("@Producto", SqlDbType.VarChar, 200).Value = mov.Producto;
                    cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar, 500).Value = mov.Descripcion;
                    cmd.Parameters.Add("@Cantidad", SqlDbType.Decimal).Value = mov.Cantidad;
                    cmd.Parameters.Add("@CantidadAnterior", SqlDbType.Decimal).Value = mov.CantidadAnterior;
                    cmd.Parameters.Add("@CantidadMovimiento", SqlDbType.Decimal).Value = mov.CantidadMovimiento;
                    cmd.Parameters.Add("@CantidadPendiente", SqlDbType.Decimal).Value = Math.Max(0, mov.Cantidad - mov.CantidadAnterior - mov.CantidadMovimiento);
                    cmd.Parameters.Add("@Unidad", SqlDbType.VarChar, 20).Value = mov.Unidad;
                    cmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 500).Value = mov.Observacion ?? string.Empty;
                    cmd.Parameters.Add("@OtRelacionada", SqlDbType.VarChar, 60).Value = mov.OtRelacionada ?? string.Empty;
                    cmd.Parameters.Add("@UsuarioRegistro", SqlDbType.VarChar, 80).Value = usuario;
                    cmd.ExecuteNonQuery();
                }

                string nuevoEstado = tipoMovimiento.Equals("Entrega", StringComparison.OrdinalIgnoreCase)
                    ? "Enviada al proveedor"
                    : EstadoRecepcion(cn, tx, idOrdenServicio);

                using SqlCommand estadoCmd = new("""
                    UPDATE dbo.OrdenesServicio
                    SET EstadoServicio = @EstadoServicio,
                        Estado = CASE WHEN Estado NOT IN ('Pagada', 'Pendiente de Pago') THEN @EstadoServicio ELSE Estado END
                    WHERE IdOrdenServicio = @IdOrdenServicio;
                    """, cn, tx);
                estadoCmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
                estadoCmd.Parameters.Add("@EstadoServicio", SqlDbType.VarChar, 40).Value = nuevoEstado;
                estadoCmd.ExecuteNonQuery();

                string accion = tipoMovimiento.Equals("Entrega", StringComparison.OrdinalIgnoreCase)
                    ? "Registro de entrega"
                    : $"Registro de recepcion - {nuevoEstado}";
                RegistrarHistorial(cn, tx, idOrdenServicio, usuario, accion);
                tx.Commit();
                return tipoMovimiento.Equals("Entrega", StringComparison.OrdinalIgnoreCase)
                    ? "Entrega registrada correctamente."
                    : "Recepcion registrada correctamente.";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return ex.Message;
            }
        }

        public string RegistrarFoto(OrdenServicioFoto foto)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);
            using SqlCommand cmd = new("""
                INSERT INTO dbo.OrdenServicioFotos
                (IdOrdenServicio, IdOrdenServicioDetalle, RutaArchivo, NombreArchivo, Titulo, UbicacionPdf, Descripcion, Orden, UsuarioRegistro)
                VALUES (@IdOrdenServicio, @IdOrdenServicioDetalle, @RutaArchivo, @NombreArchivo, @Titulo, @UbicacionPdf, @Descripcion, @Orden, @UsuarioRegistro);
                """, cn);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = foto.IdOrdenServicio;
            cmd.Parameters.Add("@IdOrdenServicioDetalle", SqlDbType.Int).Value = (object?)foto.IdOrdenServicioDetalle ?? DBNull.Value;
            cmd.Parameters.Add("@RutaArchivo", SqlDbType.VarChar, 500).Value = foto.RutaArchivo;
            cmd.Parameters.Add("@NombreArchivo", SqlDbType.VarChar, 260).Value = foto.NombreArchivo;
            cmd.Parameters.Add("@Titulo", SqlDbType.VarChar, 160).Value = foto.Titulo;
            cmd.Parameters.Add("@UbicacionPdf", SqlDbType.VarChar, 40).Value = foto.UbicacionPdf;
            cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar, 500).Value = foto.Descripcion;
            cmd.Parameters.Add("@Orden", SqlDbType.Int).Value = foto.Orden;
            cmd.Parameters.Add("@UsuarioRegistro", SqlDbType.VarChar, 80).Value = foto.UsuarioRegistro;
            cmd.ExecuteNonQuery();
            RegistrarHistorial(cn, null, foto.IdOrdenServicio, foto.UsuarioRegistro, "Registro de fotografia");
            return "Fotografia registrada correctamente.";
        }

        public string ActualizarOrdenFotos(int idOrdenServicio, IEnumerable<OrdenServicioFoto> fotos, string usuario)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);
            using SqlTransaction tx = cn.BeginTransaction();
            try
            {
                int orden = 1;
                foreach (OrdenServicioFoto foto in fotos.Where(x => x.IdOrdenServicioFoto > 0))
                {
                    using SqlCommand cmd = new("""
                        UPDATE dbo.OrdenServicioFotos
                        SET Orden = @Orden
                        WHERE IdOrdenServicioFoto = @IdFoto AND IdOrdenServicio = @IdOrdenServicio;
                        """, cn, tx);
                    cmd.Parameters.Add("@Orden", SqlDbType.Int).Value = orden++;
                    cmd.Parameters.Add("@IdFoto", SqlDbType.Int).Value = foto.IdOrdenServicioFoto;
                    cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
                    cmd.ExecuteNonQuery();
                }

                RegistrarHistorial(cn, tx, idOrdenServicio, usuario, "Actualizacion de orden de fotografias");
                tx.Commit();
                return "Orden de fotografias actualizado correctamente.";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return ex.Message;
            }
        }

        public string EliminarFoto(int idFoto, string usuario)
        {
            using SqlConnection cn = Conexion.ObtenerConexion();
            cn.Open();
            AsegurarEsquema(cn);
            using SqlCommand cmd = new("""
                DECLARE @IdOrdenServicio INT;
                SELECT @IdOrdenServicio = IdOrdenServicio FROM dbo.OrdenServicioFotos WHERE IdOrdenServicioFoto = @IdFoto;
                DELETE FROM dbo.OrdenServicioFotos WHERE IdOrdenServicioFoto = @IdFoto;
                SELECT ISNULL(@IdOrdenServicio, 0);
                """, cn);
            cmd.Parameters.Add("@IdFoto", SqlDbType.Int).Value = idFoto;
            int idOrden = Convert.ToInt32(cmd.ExecuteScalar());
            if (idOrden > 0)
                RegistrarHistorial(cn, null, idOrden, usuario, "Eliminacion de fotografia");
            return "Fotografia eliminada correctamente.";
        }

        private static void InsertarDetalles(SqlConnection cn, SqlTransaction tx, int idOrdenServicio, List<OrdenServicioDetalle> detalles)
        {
            foreach (OrdenServicioDetalle detalle in detalles)
            {
                using SqlCommand cmd = new("""
                    INSERT INTO dbo.OrdenServicioDetalle
                    (IdOrdenServicio, IdProducto, Producto, Descripcion, Cantidad, Unidad, PrecioUnitario, Total, Observaciones)
                    VALUES (@IdOrdenServicio, @IdProducto, @Producto, @Descripcion, @Cantidad, @Unidad, @PrecioUnitario, @Total, @Observaciones);
                    """, cn, tx);
                cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
                cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = (object?)detalle.IdProducto ?? DBNull.Value;
                cmd.Parameters.Add("@Producto", SqlDbType.VarChar, 200).Value = detalle.Producto;
                cmd.Parameters.Add("@Descripcion", SqlDbType.VarChar, 500).Value = detalle.Descripcion;
                cmd.Parameters.Add("@Cantidad", SqlDbType.Decimal).Value = detalle.Cantidad;
                cmd.Parameters.Add("@Unidad", SqlDbType.VarChar, 20).Value = detalle.Unidad;
                cmd.Parameters.Add("@PrecioUnitario", SqlDbType.Decimal).Value = detalle.PrecioUnitario;
                cmd.Parameters.Add("@Total", SqlDbType.Decimal).Value = detalle.Total;
                cmd.Parameters.Add("@Observaciones", SqlDbType.VarChar, 500).Value = detalle.Observaciones;
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertarPagoInicial(SqlConnection cn, SqlTransaction tx, int idOrdenServicio, OrdenServicio orden)
        {
            using SqlCommand cmd = new("""
                INSERT INTO dbo.OrdenServicioPagos
                (IdOrdenServicio, Fecha, TipoPago, Importe, MedioPago, NumeroOperacion, Observacion, UsuarioRegistro)
                VALUES (@IdOrdenServicio, @Fecha, @TipoPago, @Importe, @MedioPago, '', 'Pago registrado al crear la orden', @UsuarioRegistro);
                """, cn, tx);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
            cmd.Parameters.Add("@Fecha", SqlDbType.Date).Value = orden.Fecha.Date;
            cmd.Parameters.Add("@TipoPago", SqlDbType.VarChar, 40).Value = orden.ACuenta >= orden.Total ? "Pago final" : "Adelanto";
            cmd.Parameters.Add("@Importe", SqlDbType.Decimal).Value = orden.ACuenta;
            cmd.Parameters.Add("@MedioPago", SqlDbType.VarChar, 60).Value = orden.FormaPago;
            cmd.Parameters.Add("@UsuarioRegistro", SqlDbType.VarChar, 80).Value = orden.UsuarioRegistro;
            cmd.ExecuteNonQuery();
        }

        private static void AgregarParametrosOrden(SqlCommand cmd, OrdenServicio orden)
        {
            cmd.Parameters.Add("@Fecha", SqlDbType.Date).Value = orden.Fecha.Date;
            cmd.Parameters.Add("@FechaComprometida", SqlDbType.Date).Value = (object?)orden.FechaComprometida?.Date ?? DBNull.Value;
            cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = orden.IdProveedor;
            cmd.Parameters.Add("@IdTipoServicio", SqlDbType.Int).Value = orden.IdTipoServicio;
            cmd.Parameters.Add("@Cliente", SqlDbType.VarChar, 160).Value = orden.Cliente;
            cmd.Parameters.Add("@OciRelacionada", SqlDbType.VarChar, 60).Value = orden.OciRelacionada;
            cmd.Parameters.Add("@OtRelacionada", SqlDbType.VarChar, 60).Value = orden.OtRelacionada;
            cmd.Parameters.Add("@Responsable", SqlDbType.VarChar, 100).Value = orden.Responsable;
            cmd.Parameters.Add("@FormaPago", SqlDbType.VarChar, 80).Value = orden.FormaPago;
            cmd.Parameters.Add("@ObservacionesInternas", SqlDbType.VarChar, 1000).Value = orden.ObservacionesInternas;
            cmd.Parameters.Add("@Observaciones", SqlDbType.VarChar, 1000).Value = orden.Observaciones;
            cmd.Parameters.Add("@DistribucionFotosPdf", SqlDbType.VarChar, 20).Value = string.IsNullOrWhiteSpace(orden.DistribucionFotosPdf) ? "1 x 2" : orden.DistribucionFotosPdf;
            cmd.Parameters.Add("@Subtotal", SqlDbType.Decimal).Value = orden.Subtotal;
            cmd.Parameters.Add("@Igv", SqlDbType.Decimal).Value = orden.Igv;
            cmd.Parameters.Add("@Total", SqlDbType.Decimal).Value = orden.Total;
            cmd.Parameters.Add("@UsuarioRegistro", SqlDbType.VarChar, 80).Value = orden.UsuarioRegistro;
        }

        private static string ObtenerSiguienteNumero(SqlConnection cn, SqlTransaction tx, DateTime fecha)
        {
            string prefijo = fecha.ToString("yyMM");
            using SqlCommand cmd = new("""
                SELECT ISNULL(MAX(TRY_CONVERT(INT, RIGHT(NumeroOrden, 2))), 0) + 1
                FROM dbo.OrdenesServicio
                WHERE NumeroOrden LIKE @Prefijo + '[_]__';
                """, cn, tx);
            cmd.Parameters.Add("@Prefijo", SqlDbType.VarChar, 4).Value = prefijo;
            int correlativo = Convert.ToInt32(cmd.ExecuteScalar());
            return $"{prefijo}_{correlativo:00}";
        }

        private static void RegistrarHistorial(SqlConnection cn, SqlTransaction? tx, int idOrdenServicio, string usuario, string accion)
        {
            using SqlCommand cmd = new("""
                INSERT INTO dbo.OrdenServicioHistorial (IdOrdenServicio, Usuario, Accion)
                VALUES (@IdOrdenServicio, @Usuario, @Accion);
                """, cn, tx);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
            cmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 80).Value = string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario;
            cmd.Parameters.Add("@Accion", SqlDbType.VarChar, 120).Value = accion;
            cmd.ExecuteNonQuery();
        }

        private static TipoServicio MapearTipo(SqlDataReader dr) => new()
        {
            IdTipoServicio = Convert.ToInt32(dr["IdTipoServicio"]),
            Codigo = dr["Codigo"]?.ToString() ?? string.Empty,
            Nombre = dr["Nombre"]?.ToString() ?? string.Empty,
            Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
            RequiereEntrega = Convert.ToBoolean(dr["RequiereEntrega"]),
            Estado = Convert.ToBoolean(dr["Estado"])
        };

        private static OrdenServicio MapearOrden(SqlDataReader dr) => new()
        {
            IdOrdenServicio = Convert.ToInt32(dr["IdOrdenServicio"]),
            NumeroOrden = dr["NumeroOrden"]?.ToString() ?? string.Empty,
            Fecha = Convert.ToDateTime(dr["Fecha"]),
            FechaComprometida = dr["FechaComprometida"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaComprometida"]),
            IdProveedor = Convert.ToInt32(dr["IdProveedor"]),
            NombreProveedor = dr["NombreProveedor"]?.ToString() ?? string.Empty,
            RucProveedor = dr["RucProveedor"]?.ToString() ?? string.Empty,
            IdTipoServicio = Convert.ToInt32(dr["IdTipoServicio"]),
            TipoServicioNombre = dr["TipoServicioNombre"]?.ToString() ?? string.Empty,
            RequiereEntrega = Convert.ToBoolean(dr["RequiereEntrega"]),
            Cliente = dr["Cliente"]?.ToString() ?? string.Empty,
            OciRelacionada = dr["OciRelacionada"]?.ToString() ?? string.Empty,
            OtRelacionada = dr["OtRelacionada"]?.ToString() ?? string.Empty,
            Responsable = dr["Responsable"]?.ToString() ?? string.Empty,
            FormaPago = dr["FormaPago"]?.ToString() ?? string.Empty,
            ObservacionesInternas = dr["ObservacionesInternas"]?.ToString() ?? string.Empty,
            Observaciones = dr["Observaciones"]?.ToString() ?? string.Empty,
            DistribucionFotosPdf = dr["DistribucionFotosPdf"]?.ToString() ?? "1 x 2",
            Subtotal = Convert.ToDecimal(dr["Subtotal"]),
            Igv = Convert.ToDecimal(dr["Igv"]),
            Total = Convert.ToDecimal(dr["Total"]),
            TotalPagado = Convert.ToDecimal(dr["TotalPagado"]),
            Estado = dr["Estado"]?.ToString() ?? string.Empty,
            EstadoServicio = dr["EstadoServicio"]?.ToString() ?? string.Empty,
            EstadoPago = dr["EstadoPago"]?.ToString() ?? string.Empty,
            UsuarioRegistro = dr["UsuarioRegistro"]?.ToString() ?? string.Empty,
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
            MotivoAnulacion = dr["MotivoAnulacion"]?.ToString() ?? string.Empty
        };

        private static void CargarMovimientos(SqlConnection cn, OrdenServicio orden)
        {
            using SqlCommand cmd = new("""
                SELECT IdMovimiento, IdOrdenServicio, IdOrdenServicioDetalle, TipoMovimiento, Fecha, Producto, Descripcion,
                       Cantidad, CantidadAnterior, CantidadMovimiento, CantidadPendiente, Unidad, Observacion, OtRelacionada, UsuarioRegistro
                FROM dbo.OrdenServicioMovimientos
                WHERE IdOrdenServicio = @IdOrdenServicio
                ORDER BY Fecha, IdMovimiento;
                """, cn);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = orden.IdOrdenServicio;
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                OrdenServicioMovimiento mov = MapearMovimiento(dr);
                if (mov.TipoMovimiento.Equals("Entrega", StringComparison.OrdinalIgnoreCase))
                    orden.Entregas.Add(mov);
                else
                    orden.Recepciones.Add(mov);
            }
        }

        private static void CargarFotos(SqlConnection cn, OrdenServicio orden)
        {
            using SqlCommand cmd = new("""
                SELECT IdOrdenServicioFoto, IdOrdenServicio, IdOrdenServicioDetalle, RutaArchivo, NombreArchivo,
                       Titulo, UbicacionPdf, Descripcion, Orden, UsuarioRegistro, FechaRegistro
                FROM dbo.OrdenServicioFotos
                WHERE IdOrdenServicio = @IdOrdenServicio
                ORDER BY IdOrdenServicioDetalle, CASE WHEN Orden <= 0 THEN 999999 ELSE Orden END, IdOrdenServicioFoto;
                """, cn);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = orden.IdOrdenServicio;
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                orden.Fotos.Add(MapearFoto(dr));
        }

        private static void CargarHistorial(SqlConnection cn, OrdenServicio orden)
        {
            using SqlCommand cmd = new("""
                SELECT IdOrdenServicioHistorial, IdOrdenServicio, Usuario, Accion, FechaHora
                FROM dbo.OrdenServicioHistorial
                WHERE IdOrdenServicio = @IdOrdenServicio
                ORDER BY FechaHora, IdOrdenServicioHistorial;
                """, cn);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = orden.IdOrdenServicio;
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
                orden.Historial.Add(MapearHistorial(dr));
        }

        private static OrdenServicioDetalle MapearDetalle(SqlDataReader dr) => new()
        {
            IdOrdenServicioDetalle = Convert.ToInt32(dr["IdOrdenServicioDetalle"]),
            IdOrdenServicio = Convert.ToInt32(dr["IdOrdenServicio"]),
            IdProducto = dr["IdProducto"] == DBNull.Value ? null : Convert.ToInt32(dr["IdProducto"]),
            Producto = dr["Producto"]?.ToString() ?? string.Empty,
            Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
            Cantidad = Convert.ToDecimal(dr["Cantidad"]),
            Unidad = dr["Unidad"]?.ToString() ?? string.Empty,
            PrecioUnitario = Convert.ToDecimal(dr["PrecioUnitario"]),
            Total = Convert.ToDecimal(dr["Total"]),
            Observaciones = dr["Observaciones"]?.ToString() ?? string.Empty
        };

        private static OrdenServicioPago MapearPago(SqlDataReader dr) => new()
        {
            IdOrdenServicioPago = Convert.ToInt32(dr["IdOrdenServicioPago"]),
            IdOrdenServicio = Convert.ToInt32(dr["IdOrdenServicio"]),
            Fecha = Convert.ToDateTime(dr["Fecha"]),
            TipoPago = dr["TipoPago"]?.ToString() ?? string.Empty,
            Importe = Convert.ToDecimal(dr["Importe"]),
            MedioPago = dr["MedioPago"]?.ToString() ?? string.Empty,
            NumeroOperacion = dr["NumeroOperacion"]?.ToString() ?? string.Empty,
            Observacion = dr["Observacion"]?.ToString() ?? string.Empty,
            UsuarioRegistro = dr["UsuarioRegistro"]?.ToString() ?? string.Empty
        };

        private static OrdenServicioMovimiento MapearMovimiento(SqlDataReader dr) => new()
        {
            IdMovimiento = Convert.ToInt32(dr["IdMovimiento"]),
            IdOrdenServicio = Convert.ToInt32(dr["IdOrdenServicio"]),
            IdOrdenServicioDetalle = dr["IdOrdenServicioDetalle"] == DBNull.Value ? null : Convert.ToInt32(dr["IdOrdenServicioDetalle"]),
            TipoMovimiento = dr["TipoMovimiento"]?.ToString() ?? string.Empty,
            Fecha = Convert.ToDateTime(dr["Fecha"]),
            Producto = dr["Producto"]?.ToString() ?? string.Empty,
            Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
            Cantidad = Convert.ToDecimal(dr["Cantidad"]),
            CantidadAnterior = Convert.ToDecimal(dr["CantidadAnterior"]),
            CantidadMovimiento = Convert.ToDecimal(dr["CantidadMovimiento"]),
            CantidadPendiente = Convert.ToDecimal(dr["CantidadPendiente"]),
            Unidad = dr["Unidad"]?.ToString() ?? string.Empty,
            Observacion = dr["Observacion"]?.ToString() ?? string.Empty,
            OtRelacionada = dr["OtRelacionada"]?.ToString() ?? string.Empty,
            UsuarioRegistro = dr["UsuarioRegistro"]?.ToString() ?? string.Empty
        };

        private static OrdenServicioFoto MapearFoto(SqlDataReader dr) => new()
        {
            IdOrdenServicioFoto = Convert.ToInt32(dr["IdOrdenServicioFoto"]),
            IdOrdenServicio = Convert.ToInt32(dr["IdOrdenServicio"]),
            IdOrdenServicioDetalle = dr["IdOrdenServicioDetalle"] == DBNull.Value ? null : Convert.ToInt32(dr["IdOrdenServicioDetalle"]),
            RutaArchivo = dr["RutaArchivo"]?.ToString() ?? string.Empty,
            NombreArchivo = dr["NombreArchivo"]?.ToString() ?? string.Empty,
            Titulo = dr["Titulo"]?.ToString() ?? string.Empty,
            UbicacionPdf = dr["UbicacionPdf"]?.ToString() ?? string.Empty,
            Descripcion = dr["Descripcion"]?.ToString() ?? string.Empty,
            Orden = dr["Orden"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Orden"]),
            UsuarioRegistro = dr["UsuarioRegistro"]?.ToString() ?? string.Empty,
            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
        };

        private static OrdenServicioHistorial MapearHistorial(SqlDataReader dr) => new()
        {
            IdOrdenServicioHistorial = Convert.ToInt32(dr["IdOrdenServicioHistorial"]),
            IdOrdenServicio = Convert.ToInt32(dr["IdOrdenServicio"]),
            Usuario = dr["Usuario"]?.ToString() ?? string.Empty,
            Accion = dr["Accion"]?.ToString() ?? string.Empty,
            FechaHora = Convert.ToDateTime(dr["FechaHora"])
        };

        private static string EstadoRecepcion(SqlConnection cn, SqlTransaction tx, int idOrdenServicio)
        {
            using SqlCommand cmd = new("""
                WITH Base AS
                (
                    SELECT D.IdOrdenServicioDetalle, D.Cantidad AS CantidadEsperada
                    FROM dbo.OrdenServicioDetalle D
                    INNER JOIN dbo.OrdenesServicio O ON O.IdOrdenServicio = D.IdOrdenServicio
                    WHERE D.IdOrdenServicio = @IdOrdenServicio
                ),
                Recibido AS
                (
                    SELECT IdOrdenServicioDetalle, SUM(CantidadMovimiento) AS CantidadRecibida
                    FROM dbo.OrdenServicioMovimientos
                    WHERE IdOrdenServicio = @IdOrdenServicio
                      AND TipoMovimiento = 'Recepcion'
                    GROUP BY IdOrdenServicioDetalle
                )
                SELECT CASE
                    WHEN NOT EXISTS (SELECT 1 FROM Base) THEN 'Recibida'
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM Base B
                        LEFT JOIN Recibido R ON R.IdOrdenServicioDetalle = B.IdOrdenServicioDetalle
                        WHERE ISNULL(R.CantidadRecibida, 0) < B.CantidadEsperada
                    ) THEN 'Recepcion Parcial'
                    ELSE 'Recibida'
                END;
                """, cn, tx);
            cmd.Parameters.Add("@IdOrdenServicio", SqlDbType.Int).Value = idOrdenServicio;
            return cmd.ExecuteScalar()?.ToString() ?? "Recepcion Parcial";
        }

        private static void AsegurarEsquema(SqlConnection cn)
        {
            using SqlCommand cmd = new("""
                IF OBJECT_ID('dbo.TiposServicio', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.TiposServicio
                    (
                        IdTipoServicio INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TiposServicio PRIMARY KEY,
                        Codigo VARCHAR(20) NOT NULL,
                        Nombre VARCHAR(100) NOT NULL,
                        Descripcion VARCHAR(500) NOT NULL CONSTRAINT DF_TiposServicio_Descripcion DEFAULT(''),
                        RequiereEntrega BIT NOT NULL CONSTRAINT DF_TiposServicio_RequiereEntrega DEFAULT(0),
                        Estado BIT NOT NULL CONSTRAINT DF_TiposServicio_Estado DEFAULT(1)
                    );
                    CREATE UNIQUE INDEX UX_TiposServicio_Codigo ON dbo.TiposServicio(Codigo);
                END;

                IF NOT EXISTS (SELECT 1 FROM dbo.TiposServicio)
                BEGIN
                    INSERT INTO dbo.TiposServicio (Codigo, Nombre, Descripcion, RequiereEntrega, Estado)
                    VALUES
                    ('BOR', 'Bordado', '', 1, 1),
                    ('EST', 'Estampado', '', 1, 1),
                    ('CON', 'Confeccion', '', 1, 1),
                    ('COR', 'Corte', '', 1, 1),
                    ('LAV', 'Lavado', '', 1, 1),
                    ('ACA', 'Acabados', '', 1, 1),
                    ('REP', 'Reparaciones', '', 1, 1),
                    ('CMO', 'Compra de Mochilas', '', 0, 1),
                    ('CMA', 'Compra de Maletines', '', 0, 1),
                    ('OTR', 'Otros', '', 0, 1);
                END;

                IF NOT EXISTS (SELECT 1 FROM dbo.TiposServicio WHERE Nombre = 'SERVICIO DE BORDADO')
                    INSERT INTO dbo.TiposServicio (Codigo, Nombre, Descripcion, RequiereEntrega, Estado)
                    VALUES (
                        CASE WHEN EXISTS (SELECT 1 FROM dbo.TiposServicio WHERE Codigo = 'SVBORDADO') THEN 'SVBORDADO2' ELSE 'SVBORDADO' END,
                        'SERVICIO DE BORDADO', '', 0, 1);

                IF NOT EXISTS (SELECT 1 FROM dbo.TiposServicio WHERE Nombre = 'SERVICIO DE LAVADO')
                    INSERT INTO dbo.TiposServicio (Codigo, Nombre, Descripcion, RequiereEntrega, Estado)
                    VALUES (
                        CASE WHEN EXISTS (SELECT 1 FROM dbo.TiposServicio WHERE Codigo = 'SVLAVADO') THEN 'SVLAVADO2' ELSE 'SVLAVADO' END,
                        'SERVICIO DE LAVADO', '', 0, 1);

                IF NOT EXISTS (SELECT 1 FROM dbo.TiposServicio WHERE Nombre = 'SERVICIO DE CONFECCION')
                    INSERT INTO dbo.TiposServicio (Codigo, Nombre, Descripcion, RequiereEntrega, Estado)
                    VALUES (
                        CASE WHEN EXISTS (SELECT 1 FROM dbo.TiposServicio WHERE Codigo = 'SVCONFECCION') THEN 'SVCONFECCION2' ELSE 'SVCONFECCION' END,
                        'SERVICIO DE CONFECCION', '', 0, 1);

                UPDATE dbo.TiposServicio
                SET Nombre = CASE
                        WHEN UPPER(Nombre) = 'BORDADO' THEN 'SERVICIO DE BORDADO'
                        WHEN UPPER(Nombre) = 'LAVADO' THEN 'SERVICIO DE LAVADO'
                        WHEN UPPER(Nombre) = 'CONFECCION' THEN 'SERVICIO DE CONFECCION'
                        ELSE Nombre
                    END
                WHERE UPPER(Nombre) IN ('BORDADO', 'LAVADO', 'CONFECCION');

                IF OBJECT_ID('dbo.OrdenesServicio', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.OrdenesServicio
                    (
                        IdOrdenServicio INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenesServicio PRIMARY KEY,
                        NumeroOrden VARCHAR(30) NOT NULL,
                        Fecha DATE NOT NULL,
                        FechaComprometida DATE NULL,
                        IdProveedor INT NOT NULL,
                        IdTipoServicio INT NOT NULL,
                        Cliente VARCHAR(160) NOT NULL CONSTRAINT DF_OrdenesServicio_Cliente DEFAULT(''),
                        OciRelacionada VARCHAR(60) NOT NULL CONSTRAINT DF_OrdenesServicio_Oci DEFAULT(''),
                        OtRelacionada VARCHAR(60) NOT NULL CONSTRAINT DF_OrdenesServicio_Ot DEFAULT(''),
                        Responsable VARCHAR(100) NOT NULL CONSTRAINT DF_OrdenesServicio_Responsable DEFAULT(''),
                        FormaPago VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenesServicio_FormaPago DEFAULT(''),
                        ObservacionesInternas VARCHAR(1000) NOT NULL CONSTRAINT DF_OrdenesServicio_ObservacionesInternas DEFAULT(''),
                        Observaciones VARCHAR(1000) NOT NULL CONSTRAINT DF_OrdenesServicio_Observaciones DEFAULT(''),
                        DistribucionFotosPdf VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenesServicio_DistribucionFotosPdf DEFAULT('1 x 2'),
                        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenesServicio_Subtotal DEFAULT(0),
                        Igv DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenesServicio_Igv DEFAULT(0),
                        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenesServicio_Total DEFAULT(0),
                        Estado VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_Estado DEFAULT('Borrador'),
                        EstadoServicio VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_EstadoServicio DEFAULT('Borrador'),
                        EstadoPago VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_EstadoPago DEFAULT('Pendiente'),
                        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenesServicio_Usuario DEFAULT('Sistema'),
                        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OrdenesServicio_FechaRegistro DEFAULT(GETDATE()),
                        MotivoAnulacion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenesServicio_Motivo DEFAULT('')
                    );
                    CREATE UNIQUE INDEX UX_OrdenesServicio_NumeroOrden ON dbo.OrdenesServicio(NumeroOrden);
                END;

                IF COL_LENGTH('dbo.OrdenesServicio', 'EstadoServicio') IS NULL
                    ALTER TABLE dbo.OrdenesServicio ADD EstadoServicio VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_EstadoServicio_Legacy DEFAULT('Borrador');

                IF COL_LENGTH('dbo.OrdenesServicio', 'EstadoPago') IS NULL
                    ALTER TABLE dbo.OrdenesServicio ADD EstadoPago VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenesServicio_EstadoPago_Legacy DEFAULT('Pendiente');

                IF COL_LENGTH('dbo.OrdenesServicio', 'ObservacionesInternas') IS NULL
                    ALTER TABLE dbo.OrdenesServicio ADD ObservacionesInternas VARCHAR(1000) NOT NULL CONSTRAINT DF_OrdenesServicio_ObservacionesInternas_Legacy DEFAULT('');

                IF COL_LENGTH('dbo.OrdenesServicio', 'DistribucionFotosPdf') IS NULL
                    ALTER TABLE dbo.OrdenesServicio ADD DistribucionFotosPdf VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenesServicio_DistribucionFotosPdf_Legacy DEFAULT('1 x 2');

                IF OBJECT_ID('dbo.OrdenServicioDetalle', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.OrdenServicioDetalle
                    (
                        IdOrdenServicioDetalle INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioDetalle PRIMARY KEY,
                        IdOrdenServicio INT NOT NULL,
                        IdProducto INT NULL,
                        Producto VARCHAR(200) NOT NULL CONSTRAINT DF_OrdenServicioDetalle_Producto DEFAULT(''),
                        Descripcion VARCHAR(500) NOT NULL,
                        Cantidad DECIMAL(18,2) NOT NULL,
                        Unidad VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenServicioDetalle_Unidad DEFAULT('UND'),
                        PrecioUnitario DECIMAL(18,2) NOT NULL,
                        Total DECIMAL(18,2) NOT NULL,
                        Observaciones VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioDetalle_Obs DEFAULT(''),
                        CONSTRAINT FK_OrdenServicioDetalle_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
                    );
                END;

                IF OBJECT_ID('dbo.OrdenServicioPagos', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.OrdenServicioPagos
                    (
                        IdOrdenServicioPago INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioPagos PRIMARY KEY,
                        IdOrdenServicio INT NOT NULL,
                        Fecha DATE NOT NULL,
                        TipoPago VARCHAR(40) NOT NULL,
                        Importe DECIMAL(18,2) NOT NULL,
                        MedioPago VARCHAR(60) NOT NULL CONSTRAINT DF_OrdenServicioPagos_Medio DEFAULT(''),
                        NumeroOperacion VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenServicioPagos_Operacion DEFAULT(''),
                        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioPagos_Obs DEFAULT(''),
                        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenServicioPagos_Usuario DEFAULT('Sistema'),
                        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OrdenServicioPagos_FechaRegistro DEFAULT(GETDATE()),
                        CONSTRAINT FK_OrdenServicioPagos_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
                    );
                END;

                IF OBJECT_ID('dbo.OrdenServicioHistorial', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.OrdenServicioHistorial
                    (
                        IdOrdenServicioHistorial INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioHistorial PRIMARY KEY,
                        IdOrdenServicio INT NOT NULL,
                        Usuario VARCHAR(80) NOT NULL,
                        Accion VARCHAR(120) NOT NULL,
                        FechaHora DATETIME NOT NULL CONSTRAINT DF_OrdenServicioHistorial_Fecha DEFAULT(GETDATE()),
                        CONSTRAINT FK_OrdenServicioHistorial_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
                    );
                END;

                IF OBJECT_ID('dbo.OrdenServicioMovimientos', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.OrdenServicioMovimientos
                    (
                        IdMovimiento INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioMovimientos PRIMARY KEY,
                        IdOrdenServicio INT NOT NULL,
                        IdOrdenServicioDetalle INT NULL,
                        TipoMovimiento VARCHAR(20) NOT NULL,
                        Fecha DATE NOT NULL,
                        Producto VARCHAR(200) NOT NULL,
                        Descripcion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Descripcion DEFAULT(''),
                        Cantidad DECIMAL(18,2) NOT NULL,
                        CantidadAnterior DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Anterior DEFAULT(0),
                        CantidadMovimiento DECIMAL(18,2) NOT NULL,
                        CantidadPendiente DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Pendiente DEFAULT(0),
                        Unidad VARCHAR(20) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Unidad DEFAULT('UND'),
                        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Obs DEFAULT(''),
                        OtRelacionada VARCHAR(60) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_OT DEFAULT(''),
                        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Usuario DEFAULT('Sistema'),
                        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OrdenServicioMovimientos_Fecha DEFAULT(GETDATE()),
                        CONSTRAINT FK_OrdenServicioMovimientos_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
                    );
                END;

                IF OBJECT_ID('dbo.OrdenServicioFotos', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.OrdenServicioFotos
                    (
                        IdOrdenServicioFoto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenServicioFotos PRIMARY KEY,
                        IdOrdenServicio INT NOT NULL,
                        IdOrdenServicioDetalle INT NULL,
                        RutaArchivo VARCHAR(500) NOT NULL,
                        NombreArchivo VARCHAR(260) NOT NULL,
                        Titulo VARCHAR(160) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Titulo DEFAULT(''),
                        UbicacionPdf VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Ubicacion DEFAULT('Abajo'),
                        Descripcion VARCHAR(500) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Descripcion DEFAULT(''),
                        Orden INT NOT NULL CONSTRAINT DF_OrdenServicioFotos_Orden DEFAULT(0),
                        UsuarioRegistro VARCHAR(80) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Usuario DEFAULT('Sistema'),
                        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OrdenServicioFotos_Fecha DEFAULT(GETDATE()),
                        CONSTRAINT FK_OrdenServicioFotos_Orden FOREIGN KEY (IdOrdenServicio) REFERENCES dbo.OrdenesServicio(IdOrdenServicio)
                    );
                END;

                IF COL_LENGTH('dbo.OrdenServicioFotos', 'Titulo') IS NULL
                    ALTER TABLE dbo.OrdenServicioFotos ADD Titulo VARCHAR(160) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Titulo_Legacy DEFAULT('');

                IF COL_LENGTH('dbo.OrdenServicioFotos', 'UbicacionPdf') IS NULL
                    ALTER TABLE dbo.OrdenServicioFotos ADD UbicacionPdf VARCHAR(40) NOT NULL CONSTRAINT DF_OrdenServicioFotos_Ubicacion_Legacy DEFAULT('Abajo');

                IF COL_LENGTH('dbo.OrdenServicioFotos', 'Orden') IS NULL
                    ALTER TABLE dbo.OrdenServicioFotos ADD Orden INT NOT NULL CONSTRAINT DF_OrdenServicioFotos_Orden_Legacy DEFAULT(0);
                """, cn);
            cmd.ExecuteNonQuery();
        }
    }
}
