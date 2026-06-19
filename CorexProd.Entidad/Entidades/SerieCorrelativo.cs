using System;

namespace CorexProd.Entidad.Entidades
{
    public class TipoDocumentoNumeracion
    {
        public string CodigoTipoDocumento { get; set; } = string.Empty;
        public string NombreTipoDocumento { get; set; } = string.Empty;
        public bool Estado { get; set; }
    }

    public class SerieCorrelativo
    {
        public int IdSerieCorrelativo { get; set; }
        public string CodigoTipoDocumento { get; set; } = string.Empty;
        public string NombreTipoDocumento { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public long UltimoCorrelativo { get; set; }
        public byte CantidadDigitos { get; set; } = 6;
        public bool Activa { get; set; } = true;
        public bool Predeterminada { get; set; }
        public string UsuarioModificacion { get; set; } = string.Empty;
        public DateTime FechaModificacion { get; set; }
        public DateTime? FechaUltimoUso { get; set; }
        public string UltimoNumeroGenerado { get; set; } = string.Empty;
    }

    public class SerieCorrelativoHistorial
    {
        public long IdHistorial { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string SerieAnterior { get; set; } = string.Empty;
        public string SerieNueva { get; set; } = string.Empty;
        public long? CorrelativoAnterior { get; set; }
        public long? CorrelativoNuevo { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
