namespace CorexProd.Entidad.Entidades
{
    public class AreaProduccion
    {
        public int IdAreaProduccion { get; set; }
        public string CodigoArea { get; set; } = string.Empty;
        public string NombreArea { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int OrdenSecuencia { get; set; }
        public bool EsInicio { get; set; }
        public bool ManejaMerma { get; set; }
        public bool EsTermino { get; set; }
        public string ModoEnvio { get; set; } = "UNICO";
        public bool Activo { get; set; } = true;
        public int UsuarioRegistro { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int? UsuarioModificacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public string EstadoDescripcion => Activo ? "Activo" : "Inactivo";
        public string InicioDescripcion => EsInicio ? "Sí" : "No";
        public string TerminoDescripcion => EsTermino ? "Sí" : "No";
        public string MermaDescripcion => ManejaMerma ? "Sí" : "No";
    }
}
