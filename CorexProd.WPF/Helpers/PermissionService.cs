using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CorexProd.WPF.Helpers
{
    public static class PermissionService
    {
        public static bool PuedeGenerarOrdenTrabajo => TieneMenu("Orden de Trabajo");

        public static bool PuedeGenerarGuiaInterna => TieneMenu("Guía de Salida", "Guia de Salida");

        public static bool PuedeOperarOrdenTrabajo => PuedeGenerarOrdenTrabajo;

        public static bool TieneMenu(params string[] nombres)
        {
            if (SessionManager.MenusPermitidos == null || nombres.Length == 0)
                return false;

            string[] buscados = nombres.Select(Normalizar).ToArray();

            return SessionManager.MenusPermitidos
                .Select(Normalizar)
                .Any(menu => buscados.Contains(menu));
        }

        public static void MostrarSinPermiso()
        {
            NotificationService.Warning("No tiene permisos para realizar esta acción.");
        }

        private static string Normalizar(string texto)
        {
            string forma = (texto ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder limpio = new();

            foreach (char caracter in forma)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                    limpio.Append(caracter);
            }

            return limpio.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
