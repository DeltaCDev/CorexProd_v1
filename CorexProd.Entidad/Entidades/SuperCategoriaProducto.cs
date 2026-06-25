using System;

namespace CorexProd.Entidad.Entidades
{
    public class SuperCategoriaProducto
    {
        public int IdSuperCategoriaProducto { get; set; }

        public string NombreSuperCategoria { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public bool Estado { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}
