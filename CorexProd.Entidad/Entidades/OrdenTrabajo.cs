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
        public int? IdOrdenTrabajoRelacionada { get; set; }
        public string NumeroOTRelacionada { get; set; } = string.Empty;
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int IdUsuarioCreacion { get; set; }
        public string UsuarioCreacion { get; set; } = string.Empty;
        public string UsuarioAutoriza { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public int CantidadProductos { get; set; }
        public decimal TotalPlanificado { get; set; }
        public decimal TotalLanzado { get; set; }
        public List<OrdenTrabajoDetalle> Detalles { get; } = [];
        public List<OrdenTrabajoDetalleArea> Areas { get; } = [];
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
        public string ModoEnvio { get; set; } = string.Empty;
        public decimal CantidadRecibida { get; set; }
        public decimal CantidadEnviada { get; set; }
        public decimal CantidadMerma { get; set; }
        public decimal CantidadPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public bool Seleccionado { get; set; }
        public decimal CantidadOperacion { get; set; }
        public bool Disponible => CantidadPendiente > 0 && Estado is not ("FINALIZADA" or "BLOQUEADA" or "ANULADA");
    }

    public class OrdenTrabajoPlanificacion
    {
        public int IdOrdenCompraInternaDetalle { get; set; }
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
        public DateTime FechaHora { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string Producto => string.IsNullOrWhiteSpace(CodigoProducto)
            ? NombreProducto
            : $"{CodigoProducto} - {NombreProducto}";
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
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
