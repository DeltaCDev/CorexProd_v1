using System;
using System.Collections.Generic;

namespace CorexProd.Entidad.Entidades
{
    public class IngresoManualStockInsumo
    {
        public int IdIngresoManualStockInsumo { get; set; }
        public DateTime FechaEmision { get; set; } = DateTime.Today;
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; } = string.Empty;
        public int IdTipoDocumento { get; set; }
        public string NombreTipoDocumento { get; set; } = string.Empty;
        public string TipoNumeracion { get; set; } = "Automatica";
        public string Serie { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public int IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public string Estado { get; set; } = "Pendiente";
        public decimal Subtotal { get; set; }
        public decimal DescuentoTotal { get; set; }
        public decimal Total { get; set; }
        public string UsuarioCreador { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string UsuarioAbastecimiento { get; set; } = string.Empty;
        public DateTime? FechaAbastecimiento { get; set; }
        public string UsuarioAnulacion { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string MotivoAnulacion { get; set; } = string.Empty;
        public List<IngresoManualStockInsumoDetalle> Detalles { get; set; } = [];

        public bool EsPendiente => Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase);
        public bool EsAbastecido => Estado.Equals("Abastecido", StringComparison.OrdinalIgnoreCase);
        public bool EsAnulado => Estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase);
    }

    public class IngresoManualStockInsumoDetalle
    {
        public int IdIngresoManualStockInsumoDetalle { get; set; }
        public int IdIngresoManualStockInsumo { get; set; }
        public int IdInsumo { get; set; }
        public string CodigoInsumo { get; set; } = string.Empty;
        public string NombreInsumo { get; set; } = string.Empty;
        public int IdUnidadMedida { get; set; }
        public string NombreUnidad { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Importe { get; set; }
    }

    public class InsumoStockBusqueda
    {
        public int IdInsumo { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreInsumo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int IdUnidadMedida { get; set; }
        public string NombreUnidad { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public string InsumoBusqueda => $"{Codigo} | {NombreInsumo} | {NombreUnidad} | Stock: {StockActual:N2}";
    }
}


