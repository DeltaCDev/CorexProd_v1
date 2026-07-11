using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ProduccionView
    {
        private static readonly DependencyProperty FiltrosCompactosAplicadosProperty =
            DependencyProperty.RegisterAttached(
                "FiltrosCompactosAplicados",
                typeof(bool),
                typeof(ProduccionView),
                new PropertyMetadata(false));

        private static readonly object RegistroFiltrosCompactosOt = RegistrarFiltrosCompactosOt();

        private static object RegistrarFiltrosCompactosOt()
        {
            EventManager.RegisterClassHandler(
                typeof(ProduccionView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ProduccionView_FiltrosCompactosLoaded),
                true);

            return new object();
        }

        private static void ProduccionView_FiltrosCompactosLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ProduccionView vista
                || (bool)vista.GetValue(FiltrosCompactosAplicadosProperty))
            {
                return;
            }

            vista.Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((bool)vista.GetValue(FiltrosCompactosAplicadosProperty))
                    return;

                // Buscador: reducción del 20 % (250 px -> 200 px).
                vista.BuscarTextBox.Width = 200;
                vista.BuscarTextBox.MinWidth = 200;

                // Fechas: reducción del 20 % respecto al ancho base de 135 px.
                AjustarFechaOt(vista.FechaDesdePicker, 108);
                AjustarFechaOt(vista.FechaHastaPicker, 108);

                // Botón Quitar filtros: reducción aproximada del 10 %.
                Button? quitarFiltros = BuscarBotonOt(vista, "Quitar filtros");
                if (quitarFiltros != null)
                {
                    double anchoActual = quitarFiltros.ActualWidth > 0
                        ? quitarFiltros.ActualWidth
                        : 105;
                    quitarFiltros.Width = Math.Max(88, Math.Round(anchoActual * 0.90));
                    quitarFiltros.Padding = new Thickness(10, 0, 10, 0);
                }

                vista.SetValue(FiltrosCompactosAplicadosProperty, true);
            }), DispatcherPriority.Loaded);
        }

        private static void AjustarFechaOt(DatePicker control, double ancho)
        {
            control.Width = ancho;
            control.MinWidth = ancho;
        }

        private static Button? BuscarBotonOt(DependencyObject origen, string texto)
        {
            int cantidad = System.Windows.Media.VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = System.Windows.Media.VisualTreeHelper.GetChild(origen, i);
                if (hijo is Button boton
                    && string.Equals(ObtenerTextoBotonOt(boton), texto, StringComparison.OrdinalIgnoreCase))
                {
                    return boton;
                }

                Button? resultado = BuscarBotonOt(hijo, texto);
                if (resultado != null)
                    return resultado;
            }

            return null;
        }

        private static string ObtenerTextoBotonOt(Button boton)
        {
            if (boton.Content is string texto)
                return texto.Trim();

            if (boton.Content is TextBlock bloque)
                return bloque.Text.Trim();

            if (boton.Content is DependencyObject contenido)
            {
                TextBlock? textoInterno = BuscarTextoOt(contenido);
                return textoInterno?.Text.Trim() ?? string.Empty;
            }

            return string.Empty;
        }

        private static TextBlock? BuscarTextoOt(DependencyObject origen)
        {
            int cantidad = System.Windows.Media.VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = System.Windows.Media.VisualTreeHelper.GetChild(origen, i);
                if (hijo is TextBlock texto)
                    return texto;

                TextBlock? resultado = BuscarTextoOt(hijo);
                if (resultado != null)
                    return resultado;
            }

            return null;
        }
    }
}
