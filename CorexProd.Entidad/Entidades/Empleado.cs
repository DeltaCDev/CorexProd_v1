using System;

namespace CorexProd.Entidad.Entidades
{
    public class Empleado
    {
        public int IdEmpleado { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public int IdCargo { get; set; }
        public string NombreCargo { get; set; } = string.Empty;
        public DateTime? FechaNacimiento { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Estado { get; set; }

        public string NombreCompleto
        {
            get
            {
                return $"{Nombre} {Apellido}".Trim();
            }
        }
    }
}