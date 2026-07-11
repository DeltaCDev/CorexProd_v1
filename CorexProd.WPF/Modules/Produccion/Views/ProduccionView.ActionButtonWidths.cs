using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ProduccionView
    {
        private const double AnchoBotonPrincipalOt = 112d;

        private static readonly object RegistroAnchoBotonesOt = RegistrarAnchoBotonesOt();

        private static object RegistrarAnchoBotonesOt()
        {
            EventManager.RegisterClassHandler(
                typeof(Button),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(BotonAccionOt_Loaded),
                true);

            return new object();
        }

        private static void BotonAccionOt_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton || BuscarAncestro<ProduccionView>(boton) == null)
                return;

            string tooltip = boton.ToolTip?.ToString() ?? string.Empty;
            if (!tooltip.Equals(
                    "Consultar las áreas, cantidades y recorrido de la OT",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Mantiene alineados los botones principales, sin importar el estado de la OT.
            boton.MinWidth = AnchoBotonPrincipalOt;
            boton.Width = AnchoBotonPrincipalOt;
        }

        private static T? BuscarAncestro<T>(DependencyObject origen)
            where T : DependencyObject
        {
            DependencyObject? actual = VisualTreeHelper.GetParent(origen);
            while (actual != null)
            {
                if (actual is T encontrado)
                    return encontrado;

                actual = VisualTreeHelper.GetParent(actual);
            }

            return null;
        }
    }
}
