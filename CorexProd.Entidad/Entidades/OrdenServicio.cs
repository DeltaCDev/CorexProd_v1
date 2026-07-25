using System;
using System.Collections.Generic;
using System.IO;
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
        public string ObservacionesInternas { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public string DistribucionFotosPdf { get; set; } = "1 x 2";
        public decimal Subtotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public decimal ACuenta { get; set; }
        public string PagoInicialMedio { get; set; } = string.Empty;
        public string PagoInicialDestino { get; set; } = string.Empty;
        public string PagoInicialNumeroOperacion { get; set; } = string.Empty;
        public string PagoInicialObservacion { get; set; } = string.Empty;
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
                if (EstadoServicio.Equals("Recibida", StringComparison.OrdinalIgnoreCase))
                    return EstadoPago.Equals("Pagada", StringComparison.OrdinalIgnoreCase) ? "Pagada" : "Recibida";
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
        public bool MostrarImprimir => IdOrdenServicio > 0;
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
        public bool EsFilaRelleno { get; set; }
        public int NumeroFila { get; set; }
        public string NumeroVisual => EsFilaRelleno ? string.Empty : (NumeroFila > 0 ? NumeroFila.ToString() : string.Empty);
        public string ProductoVisual => EsFilaRelleno ? string.Empty : Producto;
        public string DescripcionVisual => EsFilaRelleno ? string.Empty : Descripcion;
        public string CantidadVisual => EsFilaRelleno ? string.Empty : Cantidad.ToString("N2");
        public string UnidadVisual => EsFilaRelleno ? string.Empty : Unidad;
        public string PrecioUnitarioVisual => EsFilaRelleno ? string.Empty : PrecioUnitario.ToString("N2");
        public string TotalVisual => EsFilaRelleno ? string.Empty : Total.ToString("N2");
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
        public string DestinoPagoVisual => ExtraerValorPrefijado(Observacion, "Numero Yape")
            ?? ExtraerValorPrefijado(Observacion, "Numero Plin")
            ?? ExtraerValorPrefijado(Observacion, "Numero")
            ?? ExtraerValorPrefijado(Observacion, "Cuenta")
            ?? string.Empty;
        public string ObservacionUsuarioVisual
        {
            get
            {
                string texto = Observacion?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(texto))
                    return string.Empty;

                string[] partes = texto.Split('|', 2, StringSplitOptions.TrimEntries);
                if (partes.Length == 2 && EsPrefijoDestino(partes[0]))
                    return partes[1];
                return EsPrefijoDestino(texto) ? string.Empty : texto;
            }
        }

        private static bool EsPrefijoDestino(string texto) =>
            texto.StartsWith("Numero Yape:", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("Numero Plin:", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("Numero:", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("Cuenta:", StringComparison.OrdinalIgnoreCase);

        private static string? ExtraerValorPrefijado(string texto, string prefijo)
        {
            texto = texto?.Trim() ?? string.Empty;
            string etiqueta = $"{prefijo}:";
            if (!texto.StartsWith(etiqueta, StringComparison.OrdinalIgnoreCase))
                return null;

            string valor = texto[etiqueta.Length..].Trim();
            int separador = valor.IndexOf('|');
            return separador >= 0 ? valor[..separador].Trim() : valor;
        }
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
        public byte[]? Imagen { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string UbicacionPdf { get; set; } = "Abajo";
        public string Descripcion { get; set; } = string.Empty;
        public int Orden { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string Nivel => IdOrdenServicioDetalle.HasValue ? "Detalle" : "General";
        public Uri? RutaArchivoUri
        {
            get
            {
                string ruta = ObtenerRutaLocal();
                return string.IsNullOrWhiteSpace(ruta) ? null : new Uri(ruta, UriKind.Absolute);
            }
        }

        public string ObtenerRutaLocal()
        {
            if (!string.IsNullOrWhiteSpace(RutaArchivo) && File.Exists(RutaArchivo))
                return RutaArchivo;

            if (Imagen == null || Imagen.Length == 0)
                return string.Empty;

            try
            {
                string extension = Path.GetExtension(NombreArchivo);
                if (string.IsNullOrWhiteSpace(extension))
                    extension = ".jpg";

                string carpeta = Path.Combine(Path.GetTempPath(), "CorexProd", "OrdenServicioFotos");
                Directory.CreateDirectory(carpeta);
                string nombre = string.IsNullOrWhiteSpace(NombreArchivo)
                    ? $"{IdOrdenServicioFoto}_{Orden}{extension}"
                    : Path.GetFileName(NombreArchivo);
                string rutaTemporal = Path.Combine(carpeta, $"{IdOrdenServicioFoto}_{nombre}");

                if (!File.Exists(rutaTemporal) || new FileInfo(rutaTemporal).Length != Imagen.Length)
                    File.WriteAllBytes(rutaTemporal, Imagen);

                return rutaTemporal;
            }
            catch
            {
                return string.Empty;
            }
        }
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
