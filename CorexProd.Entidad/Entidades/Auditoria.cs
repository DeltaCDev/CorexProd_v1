using System;

namespace CorexProd.Entidad.Entidades
{
    public class Auditoria
    {
        public int IdAuditoria { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public string Accion { get; set; } = string.Empty;

        public string Modulo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public string Equipo { get; set; } = string.Empty;
    }
}