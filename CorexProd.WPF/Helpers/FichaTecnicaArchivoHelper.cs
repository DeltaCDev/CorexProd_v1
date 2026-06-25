using CorexProd.Entidad.Entidades;
using CorexProd.Entidad.Utilidades;
using CorexProd.Negocio.Negocio;
using System.IO;

namespace CorexProd.WPF.Helpers
{
    internal static class FichaTecnicaArchivoHelper
    {
        public static string ObtenerCodigoModelo(string codigoProducto) =>
            CodigoModeloProducto.Obtener(codigoProducto);

        public static string? BuscarRutaPdf(string rutaBase, string codigoProducto)
        {
            if (string.IsNullOrWhiteSpace(rutaBase) || string.IsNullOrWhiteSpace(codigoProducto))
                return null;

            try
            {
                FichaTecnicaDocumento? documento =
                    new FichaTecnicaDocumentoNegocio().ObtenerPorProducto(codigoProducto);

                if (documento != null)
                {
                    string rutaRegistrada = Path.IsPathRooted(documento.RutaRelativa)
                        ? documento.RutaRelativa
                        : Path.Combine(rutaBase, documento.RutaRelativa);

                    if (File.Exists(rutaRegistrada))
                        return rutaRegistrada;
                }
            }
            catch
            {
                // Compatibilidad con instalaciones que aún no ejecutaron el script documental.
            }

            string codigoCompleto = codigoProducto.Trim().ToUpperInvariant();
            string codigoModelo = ObtenerCodigoModelo(codigoCompleto);

            string rutaModelo = Path.Combine(rutaBase, $"{codigoModelo}.pdf");
            if (File.Exists(rutaModelo))
                return rutaModelo;

            string rutaCompleta = Path.Combine(rutaBase, $"{codigoCompleto}.pdf");
            return File.Exists(rutaCompleta) ? rutaCompleta : null;
        }

        public static string RutaEsperada(string rutaBase, string codigoProducto)
        {
            string codigoModelo = ObtenerCodigoModelo(codigoProducto);
            return Path.Combine(rutaBase, $"{codigoModelo}.pdf");
        }
    }
}
