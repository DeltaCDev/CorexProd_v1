using System;

namespace CorexProd.Entidad.Entidades
{
    public class GuiaInternaImpresion
    {
        public int IdGuiaInterna { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public DateTime FechaImpresion { get; set; }
        public string TipoImpresion { get; set; } = string.Empty;
        public string NombreImpresora { get; set; } = string.Empty;
        public bool EsReimpresion => TipoImpresion.Equals("REIMPRESION", StringComparison.OrdinalIgnoreCase);
    }
}
