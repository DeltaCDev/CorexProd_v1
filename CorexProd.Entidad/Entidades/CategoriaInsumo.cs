namespace CorexProd.Entidad
{
    public class CategoriaInsumo
    {
        public int IdCategoriaInsumo { get; set; }

        public string NombreCategoria { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public bool Estado { get; set; }
    }
}