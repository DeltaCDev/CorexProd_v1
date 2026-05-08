using System;

namespace CorexProd.Entidad.Entidades
{
    public class UnidadMedida
    {
        public int IdUnidadMedida { get; set; }
        public string NombreUnidad { get; set; } = string.Empty;
        public string Abreviatura { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}