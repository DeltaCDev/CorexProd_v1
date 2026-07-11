using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ProduccionView
    {
        private const double AnchoBotonPrincipalOt = 112d;

        static ProduccionView()
        {
            // El constructor estático fuerza el registro antes de crear la vista.
            EventManager.RegisterClassHandler(
                typeof(Button),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(BotonAccionOt_Loaded),
                true);
        }

        private static void BotonAccionOt_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton || BuscarAncestro<ProduccionView>(boton) == null)
                return;

            string tooltip = boton.ToolTip?.ToString() ?? string.Empty;
            bool esBotonPrincipal =
                tooltip.Equals(
                    "Ingresar a las áreas y realizar transferencias",
                    StringComparison.OrdinalIgnoreCase)
                || tooltip.Equals(
                    "Consultar las áreas, cantidades y recorrido de la OT",
                    StringComparison.OrdinalIgnoreCase);

            if (!esBotonPrincipal)
                return;

            // Producción y Detalle siempre conservan exactamente el mismo ancho.
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
