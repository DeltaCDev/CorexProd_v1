using System;

namespace CorexProd.Entidad.Entidades
{
    public class Rol
    {
        public int IdRol { get; set; }

        public string NombreRol { get; set; } = string.Empty;

        public bool Estado { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}