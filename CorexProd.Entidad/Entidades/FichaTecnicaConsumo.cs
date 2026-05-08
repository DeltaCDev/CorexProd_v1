namespace CorexProd.Entidad.Entidades
{
    public class FichaTecnicaConsumo
    {
        public int IdFichaTecnica { get; set; }

        public int IdProducto { get; set; }

        public string CodigoProducto { get; set; } = string.Empty;

        public string NombreProducto { get; set; } = string.Empty;

        public int IdInsumo { get; set; }

        public string NombreInsumo { get; set; } = string.Empty;

        public decimal CantidadPorUnidad { get; set; }

        public decimal CantidadProducir { get; set; }

        public decimal CantidadTotalRequerida { get; set; }

        public int IdUnidadMedida { get; set; }

        public string NombreUnidad { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;
    }
}