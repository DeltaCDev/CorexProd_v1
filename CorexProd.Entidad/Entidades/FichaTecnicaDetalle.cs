using System;

namespace CorexProd.Entidad.Entidades
{
    public class FichaTecnicaDetalle
    {
        public int IdFichaTecnicaDetalle { get; set; }

        public int IdFichaTecnica { get; set; }

        public int IdInsumo { get; set; }

        public string NombreInsumo { get; set; } = string.Empty;

        public string CodigoInsumo { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public int IdUnidadMedida { get; set; }

        public string NombreUnidad { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;

        public bool Estado { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}