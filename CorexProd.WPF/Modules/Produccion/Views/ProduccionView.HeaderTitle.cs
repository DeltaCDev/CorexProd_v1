using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ProduccionView
    {
        private static readonly object RegistroCabeceraListaOt = RegistrarCabeceraListaOt();

        private static object RegistrarCabeceraListaOt()
        {
            EventManager.RegisterClassHandler(
                typeof(ProduccionView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ProduccionView_CabeceraLoaded),
                true);

            return new object();
        }

        private static void ProduccionView_CabeceraLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ProduccionView vista)
                return;

            TextBlock? cabecera = BuscarTextoCabecera(vista);
            if (cabecera == null)
                return;

            cabecera.Text = "Lista de Órdenes de Trabajo";
            cabecera.FontSize = 19;
            cabecera.FontWeight = FontWeights.SemiBold;
            cabecera.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            cabecera.Margin = new Thickness(0, 2, 0, 0);
        }

        private static TextBlock? BuscarTextoCabecera(DependencyObject origen)
        {
            int cantidadHijos = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidadHijos; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);

                if (hijo is TextBlock texto
                    && string.Equals(
                        texto.Text,
                        "Consulta y seguimiento de las Ordenes de Trabajo",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return texto;
                }

                TextBlock? resultado = BuscarTextoCabecera(hijo);
                if (resultado != null)
                    return resultado;
            }

            return null;
        }
    }
}
