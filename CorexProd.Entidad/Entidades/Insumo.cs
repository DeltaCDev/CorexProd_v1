namespace CorexProd.Entidad
{
    public class Insumo
    {
        public int IdInsumo { get; set; }

        public string Codigo { get; set; }  = string.Empty;

        public string NombreInsumo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public int IdCategoriaInsumo { get; set; }  

        public string NombreCategoria { get; set; } = string.Empty;

        public int IdUnidadMedida { get; set; }

        public string NombreUnidad { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;

        public decimal StockMinimo { get; set; }

        public bool Estado { get; set; }
    }
}