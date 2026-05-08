using System;

namespace CorexProd.Entidad.Entidades
{
    public class FichaTecnica
    {
        public int IdFichaTecnica { get; set; }

        public int IdProducto { get; set; }

        public string CodigoProducto { get; set; } = string.Empty;

        public string NombreProducto { get; set; } = string.Empty;

        public int Version { get; set; }

        public string? Observacion { get; set; }

        public bool Estado { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}