namespace CorexProd.Entidad.Entidades
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        public string NombreUsuario { get; set; }

        public string Clave { get; set; }

        public string NombreCompleto { get; set; }

        public int IdRol { get; set; }

        public string NombreRol { get; set; }

        public bool Estado { get; set; }
    }
}