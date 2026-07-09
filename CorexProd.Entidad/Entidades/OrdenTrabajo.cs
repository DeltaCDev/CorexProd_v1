using System;
using System.Collections.Generic;

namespace CorexProd.Entidad.Entidades
{
    public class OrdenTrabajo
    {
        public int IdOrdenTrabajo { get; set; }
        public string NumeroOT { get; set; } = string.Empty;
        public int IdOrdenCompraInterna { get; set; }
        public string NumeroOci { get; set; } = string.Empty;
        public string OrdenCompraCliente { get; set; } = string.Empty;
        public string TipoOT { get; set; } = "OCI";
        public string TipoOTDescripcion => TipoOT.Equals("OCI", StringComparison.OrdinalIgnoreCase) ? "OCI" : "Manual";
        public int? IdOrdenTrabajoRelacionada { get; set; }
        public string NumeroOTRelacionada { get; set; } = string.Empty;
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string EstadoOperativo
        {
            get
            {
                string estado = Estado.Trim().ToUpperInvariant();
                if (estado is "ANULADA" or "ANULADO") return "Anulado";
                if (TotalPendiente <= 0 && TotalProducido > 0) return "Terminado";
                if (TotalPendiente > 0 && TotalProducido > 0 && estado is not ("EN_PROCESO" or "PROCESO")) return "Terminado Parcial";
                if (estado is "EN_PROCESO" or "PROCESO" || TotalLanzado > 0) return "En Proceso";
                return "Pendiente";
            }
        }
        public bool PuedeAnular => EstadoOperativo is "Pendiente" or "En Proceso";
        public int IdUsuarioCreacion { get; set; }
        public string UsuarioCreacion { get; set; } = string.Empty;
        public string UsuarioAutoriza { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string MotivoAnulacion { get; set; } = string.Empty;
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string DetalleAnulacion => EstadoOperativo.Equals("Anulado", StringComparison.OrdinalIgnoreCase)
            ? $"Motivo: {TextoOmitido(MotivoAnulacion)}\nFecha y Hora: {(FechaAnulacion.HasValue ? FechaAnulacion.Value.ToString("dd/MM/yyyy HH:mm") : "No registrada")}\nUsuario: {TextoOmitido(UsuarioAnulacion)}"
            : string.Empty;
        public int CantidadProductos { get; set; }
        public decimal TotalPlanificado { get; set; }
        public decimal TotalLanzado { get; set; }
        public decimal TotalProducido { get; set; }
        public decimal TotalPendiente { get; set; }
        public bool TieneRegularizacionTerminada { get; set; }
        public List<OrdenTrabajoDetalle> Detalles { get; } = [];
        public List<OrdenTrabajoDetalleArea> Areas { get; } = [];

        private static string TextoOmitido(string valor) => string.IsNullOrWhiteSpace(valor) ? "No registrado" : valor.Trim();
    }

    public class OrdenTrabajoDetalle
    {
        public int IdDetalleOT { get; set; }
        public int IdOrdenTrabajo { get; set; }
        public int IdOrdenCompraInternaDetalle { get; set; }
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public decimal CantidadRequerida { get; set; }
        public decimal CantidadPlanificada { get; set; }
        public decimal CantidadLanzada { get; set; }
        public decimal CantidadProducida { get; set; }
        public decimal CantidadAplicada { get; set; }
        public decimal CantidadExcedente { get; set; }
        public decimal CantidadPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string MotivoDiferencia { get; set; } = string.Empty;
        public string ObservacionDiferencia { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }
        public decimal CantidadOperacion { get; set; }
    }

    public class OrdenTrabajoDetalleArea
    {
        public long IdDetalleArea { get; set; }
        public int IdOrdenTrabajo { get; set; }
        public int IdDetalleOT { get; set; }
        public int IdAreaProduccion { get; set; }
        public string CodigoArea { get; set; } = string.Empty;
        public string NombreArea { get; set; } = string.Empty;
        public int OrdenSecuencia { get; set; }
        public bool EsInicio { get; set; }
        public bool EsTermino { get; set; }
        public bool ManejaMerma { get; set; }
        public bool PermiteReservarStockProceso { get; set; }
        public string ModoEnvio { get; set; } = string.Empty;
        public decimal CantidadRecibida { get; set; }
        public decimal CantidadEnviada { get; set; }
        public decimal CantidadMerma { get; set; }
        public decimal CantidadReservada { get; set; }
        public decimal CantidadPendiente { get; set; }
        public decimal CantidadPendienteDisponible => Math.Max(0, CantidadPendiente - CantidadReservada);
        public string Estado { get; set; } = string.Empty;
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }
        public decimal CantidadOperacion { get; set; }
        public bool Disponible => CantidadPendienteDisponible > 0 && Estado is not ("FINALIZADA" or "BLOQUEADA" or "ANULADA");
    }

    public class OrdenTrabajoPlanificacion
    {
        public int IdOrdenCompraInternaDetalle { get; set; }
        public decimal CantidadPlanificada { get; set; }
    }

    public class OrdenTrabajoManualPlanificacion
    {
        public int IdProducto { get; set; }
        public decimal CantidadPlanificada { get; set; }
    }

    public class OrdenTrabajoLanzamiento
    {
        public int IdDetalleOT { get; set; }
        public decimal CantidadLanzada { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }

    public class OrdenTrabajoTransferenciaItem
    {
        public int IdDetalleOT { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class OrdenTrabajoValidacionProducto
    {
        public int IdOrdenCompraInternaDetalle { get; set; }
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string Producto => $"{CodigoProducto} {NombreProducto}";
        public string Observacion { get; set; } = string.Empty;
        public decimal CantidadRequerida { get; set; }
        public int? IdFichaTecnica { get; set; }
        public decimal StockAlmacen { get; set; }
        public decimal StockCorte { get; set; }
        public decimal StockConfeccion { get; set; }
        public decimal StockAcabado { get; set; }
        public decimal StockTotal { get; set; }
        public decimal Deficit { get; set; }
        public decimal StockProcesoDisponible => Math.Max(0, StockTotal - StockAlmacen);
        public decimal CantidadReservaNecesaria => Math.Min(CantidadRequerida, StockProcesoDisponible);
        public decimal CantidadReservaExcedente => Math.Max(0, StockProcesoDisponible - CantidadReservaNecesaria);
        public bool TieneStockProcesoReservado => StockProcesoDisponible > 0;
        public string EstadoInsumos { get; set; } = string.Empty;
        public bool TieneFichaTecnica =>
            !EstadoInsumos.Equals("Sin ficha tecnica", StringComparison.OrdinalIgnoreCase);
        public bool TieneSuministrosDisponibles =>
            EstadoInsumos.Equals("Completo para producir", StringComparison.OrdinalIgnoreCase);
    }

    public class OrdenTrabajoInsumoDetalle
    {
        public int IdInsumo { get; set; }
        public string CodigoInsumo { get; set; } = string.Empty;
        public string NombreInsumo { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal ConsumoUnitario { get; set; }
        public decimal CantidadProduccion { get; set; }
        public decimal CantidadNecesaria { get; set; }
        public decimal StockActual { get; set; }
        public decimal StockProyectado { get; set; }
        public decimal CantidadFaltante { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class OrdenTrabajoMovimiento
    {
        private string _accion = string.Empty;

        public DateTime FechaHora { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string Producto => string.IsNullOrWhiteSpace(CodigoProducto)
            ? NombreProducto
            : $"{CodigoProducto} - {NombreProducto}";
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Accion
        {
            get => FormatearAccion(_accion);
            set => _accion = value ?? string.Empty;
        }
        public string AccionTecnica => _accion;
        public string Usuario { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;

        private static string FormatearAccion(string accion)
        {
            return accion.Trim().ToUpperInvariant() switch
            {
                "AVANCE_AREA" => "🔄 Avance de Área",
                "REGISTRO_MERMA" => "⚠️ Registro de Merma",
                "CONSUMO_INSUMOS" => "📦 Consumo de Insumos",
                "CIERRE_PRODUCCION" => "✅ Cierre de Producción",
                "INGRESO_KARDEX" => "📥 Ingreso a Almacén",
                _ => accion
            };
        }
    }

    public class OrdenTrabajoKardexIngreso
    {
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Almacen { get; set; } = string.Empty;
        public DateTime FechaMovimiento { get; set; }
        public string Usuario { get; set; } = string.Empty;
    }
}