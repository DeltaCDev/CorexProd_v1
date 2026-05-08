using System;

namespace CorexProd.Entidad.Entidades
{
    public class Producto
    {
        public int IdProducto { get; set; }

        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public int IdCategoriaProducto { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;

        public int IdUnidadMedida { get; set; }
        public string NombreUnidad { get; set; } = string.Empty;
        public string AbreviaturaUnidad { get; set; } = string.Empty;

        public decimal StockMinimo { get; set; }

        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}