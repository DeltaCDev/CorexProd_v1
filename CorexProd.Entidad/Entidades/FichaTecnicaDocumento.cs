using System;

namespace CorexProd.Entidad.Entidades
{
    public class FichaTecnicaDocumento
    {
        public int IdFichaTecnicaDocumento { get; set; }
        public string CodigoModelo { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaRelativa { get; set; } = string.Empty;
        public int Version { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
    }
}
