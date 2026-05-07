namespace CorexProd.Entidad.Entidades
{
    public class MenuPermitido
    {
        public int IdMenu { get; set; }

        public string NombreMenu { get; set; } = string.Empty;

        public bool PuedeVer { get; set; }
    }
}