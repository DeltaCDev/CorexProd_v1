using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class StockReservaNegocio
    {
        private readonly StockReservaDatos _datos = new();

        public List<StockDisponibilidad> ListarDisponibilidad(int? idProducto = null, int? idAlmacen = null, string buscar = "") =>
            _datos.ListarDisponibilidad(idProducto, idAlmacen, buscar);

        public List<StockReserva> Listar(int? idOrdenCompraInterna = null, int? idProducto = null, bool soloActivas = false) =>
            _datos.Listar(idOrdenCompraInterna, idProducto, soloActivas);

        public List<StockReservaMovimiento> ListarMovimientos(long idStockReserva) =>
            _datos.ListarMovimientos(idStockReserva);

        public List<StockReservaHistorico> ListarHistorico(
            int? idProducto = null,
            int? idAlmacen = null,
            int? idOrdenCompraInterna = null,
            int? idOrdenTrabajo = null,
            string tipoMovimiento = "",
            string documentoReferencia = "",
            DateTime? desde = null,
            DateTime? hasta = null,
            string buscar = "",
            int top = 300) =>
            _datos.ListarHistorico(
                idProducto,
                idAlmacen,
                idOrdenCompraInterna,
                idOrdenTrabajo,
                tipoMovimiento,
                documentoReferencia,
                desde,
                hasta,
                buscar,
                top);

        public long Crear(StockReservaCrearRequest request)
        {
            ValidarUsuario(request.Usuario);
            if (request.IdOrdenCompraInterna <= 0)
                throw new InvalidOperationException("Debe indicar la orden de compra.");
            if (request.IdOrdenCompraInternaDetalle <= 0)
                throw new InvalidOperationException("Debe indicar el detalle de la orden de compra.");
            if (request.IdProducto <= 0)
                throw new InvalidOperationException("Debe indicar el producto.");
            if (request.Cantidad <= 0)
                throw new InvalidOperationException("La cantidad a reservar debe ser mayor a cero.");

            return _datos.Crear(request);
        }

        public void Consumir(int idOrdenCompraInterna, int idOrdenCompraInternaDetalle, decimal cantidad, string usuario, string documentoReferencia = "", string observacion = "")
        {
            ValidarUsuario(usuario);
            if (idOrdenCompraInterna <= 0 || idOrdenCompraInternaDetalle <= 0)
                throw new InvalidOperationException("Debe indicar la OC y su detalle.");
            if (cantidad <= 0)
                throw new InvalidOperationException("La cantidad a consumir debe ser mayor a cero.");

            _datos.Consumir(idOrdenCompraInterna, idOrdenCompraInternaDetalle, cantidad, usuario, documentoReferencia, observacion);
        }

        public void Liberar(long idStockReserva, decimal? cantidad, string usuario, string documentoReferencia = "", string observacion = "")
        {
            ValidarUsuario(usuario);
            if (idStockReserva <= 0)
                throw new InvalidOperationException("Debe indicar la reserva.");
            if (cantidad.HasValue && cantidad.Value <= 0)
                throw new InvalidOperationException("La cantidad a liberar debe ser mayor a cero.");

            _datos.Liberar(idStockReserva, cantidad, usuario, documentoReferencia, observacion);
        }

        private static void ValidarUsuario(string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario))
                throw new InvalidOperationException("Debe indicar el usuario responsable.");
        }
    }
}
