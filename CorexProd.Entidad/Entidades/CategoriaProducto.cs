using System;

namespace CorexProd.Entidad.Entidades
{
    public class CategoriaProducto
    {
        public int IdCategoriaProducto { get; set; }

        public string NombreCategoria { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public bool Estado { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}