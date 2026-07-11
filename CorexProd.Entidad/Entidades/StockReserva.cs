using System;

namespace CorexProd.Entidad.Entidades
{
    public class StockDisponibilidad
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string EtiquetaCliente { get; set; } = string.Empty;
        public int IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; } = string.Empty;
        public decimal StockFisico { get; set; }
        public decimal StockReservado { get; set; }
        public decimal StockDisponible { get; set; }
    }

    public class StockReserva
    {
        public long IdStockReserva { get; set; }
        public int IdOrdenCompraInterna { get; set; }
        public string NumeroOci { get; set; } = string.Empty;
        public int IdOrdenCompraInternaDetalle { get; set; }
        public int IdProducto { get; set; }
        public int? IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; } = string.Empty;
        public int? IdOrdenTrabajo { get; set; }
        public string NumeroOT { get; set; } = string.Empty;
        public int? IdDetalleOT { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public decimal CantidadReservada { get; set; }
        public decimal CantidadConsumida { get; set; }
        public decimal CantidadLiberada { get; set; }
        public decimal CantidadPendiente { get; set; }
        public string TipoOrigen { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaReserva { get; set; }
        public string UsuarioReserva { get; set; } = string.Empty;
        public DateTime FechaActualizacion { get; set; }
        public string UsuarioActualizacion { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }

    public class StockReservaMovimiento
    {
        public long IdStockReservaMovimiento { get; set; }
        public long IdStockReserva { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string DocumentoReferencia { get; set; } = string.Empty;
        public string UsuarioMovimiento { get; set; } = string.Empty;
        public DateTime FechaMovimiento { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }

    public class StockReservaHistorico
    {
        public long IdStockReservaMovimiento { get; set; }
        public long IdStockReserva { get; set; }
        public int IdOrdenCompraInterna { get; set; }
        public string NumeroOci { get; set; } = string.Empty;
        public string OrdenCompraCliente { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public int IdOrdenCompraInternaDetalle { get; set; }
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string EtiquetaCliente { get; set; } = string.Empty;
        public int? IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; } = string.Empty;
        public int? IdOrdenTrabajo { get; set; }
        public string NumeroOT { get; set; } = string.Empty;
        public int? IdDetalleOT { get; set; }
        public string TipoOrigen { get; set; } = string.Empty;
        public string EstadoReserva { get; set; } = string.Empty;
        public decimal CantidadReservada { get; set; }
        public decimal CantidadConsumida { get; set; }
        public decimal CantidadLiberada { get; set; }
        public decimal CantidadPendiente { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public decimal CantidadMovimiento { get; set; }
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string DocumentoReferencia { get; set; } = string.Empty;
        public string UsuarioMovimiento { get; set; } = string.Empty;
        public DateTime FechaMovimiento { get; set; }
        public string ObservacionMovimiento { get; set; } = string.Empty;
        public string ObservacionReserva { get; set; } = string.Empty;
    }

    public class StockReservaCrearRequest
    {
        public int IdOrdenCompraInterna { get; set; }
        public int IdOrdenCompraInternaDetalle { get; set; }
        public int IdProducto { get; set; }
        public int? IdAlmacen { get; set; }
        public int? IdOrdenTrabajo { get; set; }
        public int? IdDetalleOT { get; set; }
        public decimal Cantidad { get; set; }
        public string TipoOrigen { get; set; } = "STOCK_FISICO";
        public string Usuario { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }
}
