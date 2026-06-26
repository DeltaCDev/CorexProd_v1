using System;

namespace CorexProd.Entidad.Entidades
{
    public class Producto
    {
        public int IdProducto { get; set; }

        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string EtiquetaCliente { get; set; } = string.Empty;
        public string ProductoBusqueda => string.IsNullOrWhiteSpace(Codigo)
            ? ProductoBusquedaBase
            : $"{Codigo} - {ProductoBusquedaBase}";
        private string ProductoBusquedaBase => string.IsNullOrWhiteSpace(EtiquetaCliente)
            ? NombreProducto
            : $"{NombreProducto} [{EtiquetaCliente}]";
        public bool TieneFichaTecnica { get; set; }
        public string ProductoFichaTecnicaBusqueda => TieneFichaTecnica
            ? $"{ProductoBusqueda} (Ya tiene ficha tecnica)"
            : ProductoBusqueda;
        public string Descripcion { get; set; } = string.Empty;

        public int IdSuperCategoriaProducto { get; set; }
        public string NombreSuperCategoria { get; set; } = string.Empty;

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
