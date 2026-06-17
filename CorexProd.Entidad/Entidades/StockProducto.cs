namespace CorexProd.Entidad.Entidades
{
    public class StockProducto
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public int IdCategoriaProducto { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }
}
