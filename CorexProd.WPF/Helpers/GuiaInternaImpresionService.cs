using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Modules.Ventas.Views;
using System;
using System.Configuration;
using System.Printing;

namespace CorexProd.WPF.Helpers
{
    internal static class GuiaInternaImpresionService
    {
        private const string ClaveImpresora = "ImpresoraGuiaInternaSalida";

        public static string? ImprimirOriginal(string numeroGuia)
        {
            try
            {
                GuiaInternaNegocio negocio = new();
                GuiaInterna? guia = negocio.ObtenerPorNumero(numeroGuia);
                if (guia == null)
                    return "La guía fue guardada, pero no se pudo recuperar para imprimirla.";
                if (negocio.ExisteImpresionOriginal(guia.IdGuiaInterna))
                    return null;

                return Imprimir(guia, false);
            }
            catch (Exception ex)
            {
                return $"La guía permanece guardada, pero no se pudo iniciar la impresión automática: {ex.Message}";
            }
        }

        public static string? Reimprimir(GuiaInterna guia) => Imprimir(guia, true);

        private static string? Imprimir(GuiaInterna guia, bool esReimpresion)
        {
            Usuario? usuario = SessionManager.UsuarioActual;
            if (usuario == null || usuario.IdUsuario <= 0)
                return "No se pudo identificar al usuario conectado. La guía no fue enviada a impresión.";

            string nombreImpresora = ConfigurationManager.AppSettings[ClaveImpresora]?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nombreImpresora))
                return $"La guía quedó guardada, pero no hay una impresora configurada en App.config ({ClaveImpresora}).";

            Empresa? empresa = new EmpresaNegocio().ObtenerPredeterminada();
            if (empresa == null)
                return "La guía quedó guardada, pero no existe una empresa predeterminada para generar el documento.";

            try
            {
                using LocalPrintServer servidor = new();
                using PrintQueue cola = servidor.GetPrintQueue(nombreImpresora);
                cola.Refresh();
                if (cola.IsOffline || cola.IsNotAvailable || cola.IsInError)
                    return $"La impresora '{nombreImpresora}' no está disponible. La guía permanece guardada.";

                DateTime fechaImpresion = DateTime.Now;
                GuiaInternaImpresion impresion = new()
                {
                    IdGuiaInterna = guia.IdGuiaInterna,
                    IdUsuario = usuario.IdUsuario,
                    NombreUsuario = string.IsNullOrWhiteSpace(usuario.NombreCompleto) ? usuario.NombreUsuario : usuario.NombreCompleto,
                    FechaImpresion = fechaImpresion,
                    TipoImpresion = esReimpresion ? "REIMPRESION" : "ORIGINAL",
                    NombreImpresora = cola.FullName
                };

                GuiaInternaDocumentoWindow documento = new(guia, empresa);
                try
                {
                    documento.EnviarAImpresora(cola, impresion);
                }
                finally
                {
                    documento.Close();
                }

                try
                {
                    new GuiaInternaNegocio().RegistrarImpresion(impresion);
                }
                catch (Exception ex)
                {
                    return $"La guía fue enviada a la impresora, pero no se pudo registrar la auditoría: {ex.Message}";
                }
                return null;
            }
            catch (PrintQueueException ex)
            {
                return $"No se pudo enviar la guía a la impresora '{nombreImpresora}': {ex.Message}. La guía permanece guardada.";
            }
            catch (Exception ex)
            {
                return $"La guía permanece guardada, pero ocurrió un error al imprimir: {ex.Message}";
            }
        }
    }
}
