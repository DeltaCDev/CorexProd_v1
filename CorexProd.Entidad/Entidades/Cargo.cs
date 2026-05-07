using System;

namespace CorexProd.Entidad.Entidades
{
    public class Cargo
    {
        public int IdCargo { get; set; }
        public string NombreCargo { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}