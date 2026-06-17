using System;

namespace CorexProd.Entidad.Entidades
{
    public class Cliente
    {
        public int IdCliente { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string ClienteBusqueda => string.IsNullOrWhiteSpace(NumeroDocumento)
            ? NombreRazonSocial
            : $"{NumeroDocumento} - {NombreRazonSocial}";
    }
}
