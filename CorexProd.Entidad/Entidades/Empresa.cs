using System;

namespace CorexProd.Entidad.Entidades
{
    public class Empresa
    {
        public int IdEmpresa { get; set; }
        public string Ruc { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string NombreComercial { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;
        public string Distrito { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public string CodigoCliente { get; set; } = string.Empty;
        public string LicenciaActivacion { get; set; } = string.Empty;
        public bool EsPredeterminada { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
