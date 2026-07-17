using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Entidad.Entidades
{
    public class TipoServicio
    {
        public int IdTipoServicio { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool RequiereEntrega { get; set; }
        public bool Estado { get; set; } = true;

        public string RequiereEntregaTexto => RequiereEntrega ? "Si" : "No";
        public string EstadoTexto => Estado ? "Activo" : "Inactivo";
    }

    public class FormaPagoOs
    {
        public int IdFormaPagoOs { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public string EstadoTexto => Estado ? "Activo" : "Inactivo";
    }

    public class OrdenServicio
    {
        public int IdOrdenServicio { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        public DateTime? FechaComprometida { get; set; }
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = string.Empty;
        public string RucProveedor { get; set; } = string.Empty;
        public int IdTipoServicio { get; set; }
        public string TipoServicioNombre { get; set; } = string.Empty;
        public bool RequiereEntrega { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string OciRelacionada { get; set; } = string.Empty;
        public string OtRelacionada { get; set; } = string.Empty;
        public string Responsable { get; set; } = string.Empty;
        public string FormaPago { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public decimal ACuenta { get; set; }
        public decimal TotalPagado { get; set; }
        public string Estado { get; set; } = "Borrador";
        public string EstadoServicio { get; set; } = "Borrador";
        public string EstadoPago { get; set; } = "Pendiente";
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string MotivoAnulacion { get; set; } = string.Empty;
        public List<OrdenServicioDetalle> Detalles { get; set; } = [];
        public List<OrdenServicioPago> Pagos { get; set; } = [];
        public List<OrdenServicioMovimiento> Entregas { get; set; } = [];
        public List<OrdenServicioMovimiento> Recepciones { get; set; } = [];
        public List<OrdenServicioFoto> Fotos { get; set; } = [];
        public List<OrdenServicioHistorial> Historial { get; set; } = [];

        public decimal SaldoPendiente => Math.Max(0, Total - TotalPagado);
        public bool PuedeEditar => Estado.Equals("Borrador", StringComparison.OrdinalIgnoreCase);
        public bool PuedeAprobar => Estado.Equals("Borrador", StringComparison.OrdinalIgnoreCase);
        public bool PuedePagar => !EstaAnulada && Total > 0 && SaldoPendiente > 0;
        public bool PuedeRegistrarEntrega => RequiereEntrega && !EstaAnulada && !EstadoServicio.Equals("Borrador", StringComparison.OrdinalIgnoreCase);
        public bool PuedeRegistrarRecepcion => !EstaAnulada && (!RequiereEntrega || Entregas.Count > 0 || EstadoServicio.Equals("Enviada al proveedor", StringComparison.OrdinalIgnoreCase) || EstadoServicio.Equals("Recepcion Parcial", StringComparison.OrdinalIgnoreCase));
        public bool EstaAnulada => Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase);
        public string EstadoOrden
        {
            get
            {
                if (EstaAnulada) return "Anulada";
                if (EstadoPago.Equals("Pagada", StringComparison.OrdinalIgnoreCase) || Estado.Equals("Pagada", StringComparison.OrdinalIgnoreCase)) return "Pagada";
                if (EstadoServicio.Equals("Recibida", StringComparison.OrdinalIgnoreCase)) return "Recibida";
                if (EstadoServicio.Equals("Recepcion Parcial", StringComparison.OrdinalIgnoreCase)) return "Recepcion Parcial";
                if (Estado.Equals("Aprobada", StringComparison.OrdinalIgnoreCase) || EstadoServicio.Equals("Aprobada", StringComparison.OrdinalIgnoreCase) || EstadoServicio.Equals("Enviada al proveedor", StringComparison.OrdinalIgnoreCase)) return "Aprobada";
                return "Borrador";
            }
        }
        public string EstadoVisual => EstadoOrden switch
        {
            "Borrador" => "🟡 Borrador",
            "Aprobada" => "🟢 Aprobada",
            "Recepcion Parcial" => "🟠 Recepción Parcial",
            "Recibida" => "🔵 Recibida",
            "Pagada" => "🟣 Pagada",
            "Anulada" => "🔴 Anulada",
            _ => EstadoOrden
        };
        public bool MostrarVer => true;
        public bool MostrarEditar => EstadoOrden == "Borrador";
        public bool MostrarAprobar => EstadoOrden == "Borrador";
        public bool MostrarImprimir => EstadoOrden != "Borrador";
        public bool MostrarEntrega => EstadoOrden == "Aprobada" && PuedeRegistrarEntrega;
        public bool MostrarRecepcion => (EstadoOrden == "Aprobada" || EstadoOrden == "Recepcion Parcial") && PuedeRegistrarRecepcion;
        public bool MostrarPago => (EstadoOrden == "Aprobada" || EstadoOrden == "Recepcion Parcial" || EstadoOrden == "Recibida") && PuedePagar;
        public bool MostrarHistorial => EstadoOrden != "Borrador";
        public bool MostrarCopiar => EstadoOrden is "Borrador" or "Aprobada" or "Recibida" or "Pagada";
        public bool MostrarAnular => EstadoOrden is "Borrador" or "Aprobada";
        public string FechaVisual => Fecha.ToString("dd/MM/yyyy");
        public string FechaComprometidaVisual => FechaComprometida.HasValue ? FechaComprometida.Value.ToString("dd/MM/yyyy") : "-";
    }

    public class OrdenServicioDetalle
    {
        public int IdOrdenServicioDetalle { get; set; }
        public int IdOrdenServicio { get; set; }
        public int? IdProducto { get; set; }
        public string Producto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; } = "UND";
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    public class OrdenServicioPago
    {
        public int IdOrdenServicioPago { get; set; }
        public int IdOrdenServicio { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string TipoPago { get; set; } = "Pago parcial";
        public decimal Importe { get; set; }
        public string MedioPago { get; set; } = string.Empty;
        public string NumeroOperacion { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string UsuarioRegistro { get; set; } = string.Empty;
    }

    public class OrdenServicioMovimiento
    {
        public int IdMovimiento { get; set; }
        public int IdOrdenServicio { get; set; }
        public int? IdOrdenServicioDetalle { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string Producto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CantidadAnterior { get; set; }
        public decimal CantidadMovimiento { get; set; }
        public decimal CantidadPendiente { get; set; }
        public string Unidad { get; set; } = "UND";
        public string Observacion { get; set; } = string.Empty;
        public string OtRelacionada { get; set; } = string.Empty;
        public string UsuarioRegistro { get; set; } = string.Empty;
    }

    public class OrdenServicioFoto
    {
        public int IdOrdenServicioFoto { get; set; }
        public int IdOrdenServicio { get; set; }
        public int? IdOrdenServicioDetalle { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string UbicacionPdf { get; set; } = "Abajo";
        public string Descripcion { get; set; } = string.Empty;
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string Nivel => IdOrdenServicioDetalle.HasValue ? "Detalle" : "General";
    }

    public class OrdenServicioHistorial
    {
        public int IdOrdenServicioHistorial { get; set; }
        public int IdOrdenServicio { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string FechaHoraVisual => FechaHora.ToString("dd/MM/yyyy HH:mm");
    }
}
