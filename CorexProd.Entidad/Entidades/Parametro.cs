using System;

namespace CorexProd.Entidad.Entidades
{
    public class Parametro
    {
        public int IdParametro { get; set; }
        public string CodigoParametro { get; set; } = string.Empty;
        public string NombreParametro { get; set; } = string.Empty;
        public string ValorParametro { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}