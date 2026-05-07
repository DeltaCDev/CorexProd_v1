using System;

namespace CorexProd.Entidad.Entidades
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        public int IdEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;

        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }
        public bool Estado { get; set; }
    }
}