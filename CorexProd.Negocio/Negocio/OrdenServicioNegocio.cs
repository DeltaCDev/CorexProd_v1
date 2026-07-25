using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class OrdenServicioNegocio
    {
        private readonly OrdenServicioDatos _datos = new();
        private readonly UsuarioNegocio _usuarioNegocio = new();

        public List<OrdenServicio> Listar(string? buscar = null, string? estado = null) => _datos.Listar(buscar, estado);
        public OrdenServicio? Obtener(int idOrdenServicio) => idOrdenServicio <= 0 ? null : _datos.Obtener(idOrdenServicio);
        public List<OrdenServicioHistorial> ListarHistorial(int idOrdenServicio) => idOrdenServicio <= 0 ? [] : _datos.ListarHistorial(idOrdenServicio);
        public List<TipoServicio> ListarTiposServicio(bool soloActivos = false) => _datos.ListarTiposServicio(soloActivos);

        public string GuardarTipoServicio(TipoServicio tipo)
        {
            tipo.Codigo = tipo.Codigo.Trim().ToUpperInvariant();
            tipo.Nombre = tipo.Nombre.Trim();
            tipo.Descripcion = tipo.Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(tipo.Codigo))
                return "Debe ingresar el codigo del tipo de servicio.";
            if (string.IsNullOrWhiteSpace(tipo.Nombre))
                return "Debe ingresar el nombre del tipo de servicio.";

            return _datos.GuardarTipoServicio(tipo);
        }

        public string Guardar(OrdenServicio orden)
        {
            orden.Cliente = orden.Cliente.Trim();
            orden.OciRelacionada = orden.OciRelacionada.Trim();
            orden.OtRelacionada = orden.OtRelacionada.Trim();
            orden.Responsable = orden.Responsable.Trim();
            orden.FormaPago = orden.FormaPago.Trim();
            orden.ObservacionesInternas = orden.ObservacionesInternas.Trim();
            orden.Observaciones = orden.Observaciones.Trim();
            orden.DistribucionFotosPdf = NormalizarDistribucionFotos(orden.DistribucionFotosPdf);
            orden.PagoInicialMedio = orden.PagoInicialMedio.Trim();
            orden.PagoInicialDestino = orden.PagoInicialDestino.Trim();
            orden.PagoInicialNumeroOperacion = orden.PagoInicialNumeroOperacion.Trim();
            orden.PagoInicialObservacion = orden.PagoInicialObservacion.Trim();

            if (orden.IdProveedor <= 0)
                return "Debe seleccionar un proveedor.";
            if (orden.IdTipoServicio <= 0)
                return "Debe seleccionar un tipo de servicio.";
            if (orden.Detalles.Count == 0)
                return "Debe agregar al menos un detalle.";
            if (orden.Detalles.Any(d => string.IsNullOrWhiteSpace(d.Producto) || d.Cantidad <= 0 || d.PrecioUnitario < 0))
                return "Cada detalle debe tener producto, cantidad mayor a cero y precio valido.";

            foreach (OrdenServicioDetalle detalle in orden.Detalles)
            {
                detalle.Producto = detalle.Producto.Trim();
                detalle.Descripcion = string.IsNullOrWhiteSpace(detalle.Descripcion)
                    ? detalle.Producto
                    : detalle.Descripcion.Trim();
                detalle.Unidad = string.IsNullOrWhiteSpace(detalle.Unidad) ? "UND" : detalle.Unidad.Trim().ToUpperInvariant();
                detalle.Observaciones = detalle.Observaciones.Trim();
                detalle.Total = Math.Round(detalle.Cantidad * detalle.PrecioUnitario, 2);
            }

            orden.Subtotal = Math.Round(orden.Detalles.Sum(d => d.Total), 2);
            orden.Igv = 0;
            orden.Total = orden.Subtotal;
            orden.ACuenta = Math.Round(orden.ACuenta, 2);
            if (orden.ACuenta < 0 || orden.ACuenta > orden.Total)
                return "El importe a cuenta no puede ser menor a cero ni mayor al total.";
            if (orden.ACuenta > 0)
            {
                if (string.IsNullOrWhiteSpace(orden.PagoInicialMedio))
                    return "Debe seleccionar el medio de pago del importe a cuenta.";
                if (!orden.PagoInicialMedio.Equals("EFECTIVO", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(orden.PagoInicialDestino))
                    return orden.PagoInicialMedio.Equals("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase)
                        ? "Debe ingresar la cuenta del pago a cuenta."
                        : "Debe ingresar el numero del pago a cuenta.";
            }
            if (string.IsNullOrWhiteSpace(orden.UsuarioRegistro))
                orden.UsuarioRegistro = "Sistema";

            return _datos.Guardar(orden);
        }

        public string Aprobar(int idOrdenServicio, string usuario, string claveAprobacion)
        {
            if (idOrdenServicio <= 0)
                return "Debe seleccionar una orden de servicio.";

            var aprobacion = _usuarioNegocio.ResolverAprobadorOs(usuario, claveAprobacion);
            if (!aprobacion.Mensaje.Equals("OK", StringComparison.OrdinalIgnoreCase))
                return aprobacion.Mensaje;

            string aprobador = _usuarioNegocio.ObtenerNombrePersona(aprobacion.UsuarioAprobador);
            return _datos.Aprobar(idOrdenServicio, Usuario(aprobador));
        }

        public string Anular(int idOrdenServicio, string usuario, string motivo)
        {
            if (idOrdenServicio <= 0)
                return "Debe seleccionar una orden de servicio.";
            if (string.IsNullOrWhiteSpace(motivo))
                return "Debe ingresar el motivo de anulacion.";
            return _datos.Anular(idOrdenServicio, Usuario(usuario), motivo.Trim());
        }

        public string RegistrarPago(OrdenServicioPago pago)
        {
            if (pago.IdOrdenServicio <= 0)
                return "Debe seleccionar una orden de servicio.";
            if (pago.Importe <= 0)
                return "El importe debe ser mayor a cero.";

            pago.TipoPago = string.IsNullOrWhiteSpace(pago.TipoPago) ? "Pago parcial" : pago.TipoPago.Trim();
            pago.MedioPago = pago.MedioPago.Trim();
            pago.NumeroOperacion = pago.NumeroOperacion.Trim();
            pago.Observacion = pago.Observacion.Trim();
            pago.UsuarioRegistro = Usuario(pago.UsuarioRegistro);
            return _datos.RegistrarPago(pago);
        }

        public string Copiar(int idOrdenServicio, string usuario)
        {
            if (idOrdenServicio <= 0)
                return "Debe seleccionar una orden de servicio.";
            return _datos.Copiar(idOrdenServicio, Usuario(usuario));
        }

        public List<OrdenServicioMovimiento> PrepararEntrega(int idOrdenServicio)
        {
            if (idOrdenServicio <= 0)
                return [];
            return _datos.PrepararEntrega(idOrdenServicio);
        }

        public List<OrdenServicioMovimiento> PrepararRecepcion(int idOrdenServicio)
        {
            if (idOrdenServicio <= 0)
                return [];
            return _datos.PrepararRecepcion(idOrdenServicio);
        }

        public string RegistrarEntrega(int idOrdenServicio, IEnumerable<OrdenServicioMovimiento> movimientos, string usuario)
        {
            OrdenServicio? orden = Obtener(idOrdenServicio);
            if (orden == null)
                return "No se encontro la orden de servicio.";
            if (!orden.RequiereEntrega)
                return "El tipo de servicio seleccionado no requiere entrega al proveedor.";
            if (orden.EstadoServicio.Equals("Borrador", StringComparison.OrdinalIgnoreCase))
                return "Debe aprobar la orden antes de registrar entregas.";

            List<OrdenServicioMovimiento> lista = movimientos.ToList();
            if (lista.Any(x => x.CantidadMovimiento < 0 || x.CantidadMovimiento > x.CantidadPendiente))
                return "No puede enviar cantidades negativas ni mayores al pendiente.";

            return _datos.RegistrarMovimientos(idOrdenServicio, "Entrega", lista, Usuario(usuario));
        }

        public string RegistrarRecepcion(int idOrdenServicio, IEnumerable<OrdenServicioMovimiento> movimientos, string usuario)
        {
            OrdenServicio? orden = Obtener(idOrdenServicio);
            if (orden == null)
                return "No se encontro la orden de servicio.";
            if (orden.EstadoServicio.Equals("Borrador", StringComparison.OrdinalIgnoreCase))
                return "Debe aprobar la orden antes de registrar recepciones.";

            List<OrdenServicioMovimiento> lista = movimientos.ToList();
            if (lista.Any(x => x.CantidadMovimiento < 0 || x.CantidadMovimiento > x.CantidadPendiente))
                return "No puede recibir cantidades negativas ni mayores al pendiente.";

            return _datos.RegistrarMovimientos(idOrdenServicio, "Recepcion", lista, Usuario(usuario));
        }

        public string RegistrarFoto(OrdenServicioFoto foto)
        {
            if (foto.IdOrdenServicio <= 0)
                return "Debe seleccionar una orden de servicio.";
            if ((string.IsNullOrWhiteSpace(foto.RutaArchivo) && foto.Imagen is not { Length: > 0 }) || string.IsNullOrWhiteSpace(foto.NombreArchivo))
                return "Debe seleccionar una imagen valida.";
            foto.Titulo = foto.Titulo.Trim();
            foto.UbicacionPdf = string.IsNullOrWhiteSpace(foto.UbicacionPdf) ? "Abajo" : foto.UbicacionPdf.Trim();
            foto.Descripcion = foto.Descripcion.Trim();
            foto.UsuarioRegistro = Usuario(foto.UsuarioRegistro);
            return _datos.RegistrarFoto(foto);
        }

        public string ActualizarOrdenFotos(int idOrdenServicio, IEnumerable<OrdenServicioFoto> fotos, string usuario)
        {
            if (idOrdenServicio <= 0)
                return "Debe seleccionar una orden de servicio.";

            return _datos.ActualizarOrdenFotos(idOrdenServicio, fotos, Usuario(usuario));
        }

        public string EliminarFoto(int idFoto, string usuario)
        {
            if (idFoto <= 0)
                return "Debe seleccionar una fotografia.";
            return _datos.EliminarFoto(idFoto, Usuario(usuario));
        }

        private static string Usuario(string usuario) => string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim();

        private static string NormalizarDistribucionFotos(string distribucion)
        {
            distribucion = (distribucion ?? string.Empty).Trim();
            if (distribucion.Equals("2 x 4", StringComparison.OrdinalIgnoreCase))
                return "2 x 4";
            return distribucion.Equals("2 x 2", StringComparison.OrdinalIgnoreCase) ? "2 x 2" : "1 x 2";
        }
    }
}
