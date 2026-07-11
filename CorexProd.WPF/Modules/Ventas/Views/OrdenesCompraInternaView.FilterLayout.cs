using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class OrdenesCompraInternaView
    {
        private static readonly DependencyProperty AjusteFiltrosOcAplicadoProperty =
            DependencyProperty.RegisterAttached(
                "AjusteFiltrosOcAplicado",
                typeof(bool),
                typeof(OrdenesCompraInternaView),
                new PropertyMetadata(false));

        private static readonly object RegistroAjusteFiltrosOc = RegistrarAjusteFiltrosOc();

        private static object RegistrarAjusteFiltrosOc()
        {
            EventManager.RegisterClassHandler(
                typeof(OrdenesCompraInternaView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OrdenesCompraInternaView_AjusteFiltrosLoaded),
                true);

            return new object();
        }

        private static void OrdenesCompraInternaView_AjusteFiltrosLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not OrdenesCompraInternaView vista
                || (bool)vista.GetValue(AjusteFiltrosOcAplicadoProperty))
            {
                return;
            }

            vista.Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((bool)vista.GetValue(AjusteFiltrosOcAplicadoProperty))
                    return;

                TextBox? buscador = BuscarControlPorEtiquetaOc<TextBox>(vista, "Buscar");
                DatePicker? fechaDesde = BuscarControlPorEtiquetaOc<DatePicker>(vista, "Fecha desde");
                DatePicker? fechaHasta = BuscarControlPorEtiquetaOc<DatePicker>(vista, "Fecha hasta");
                Button? quitarFiltros = BuscarBotonOc(vista, texto =>
                    texto.Contains("Quitar filtros", StringComparison.OrdinalIgnoreCase));
                Button? nuevo = BuscarBotonOc(vista, texto =>
                    texto.Contains("Nuevo", StringComparison.OrdinalIgnoreCase)
                    || texto.Contains("Nueva OC", StringComparison.OrdinalIgnoreCase));

                ReducirAnchoOc(buscador, 0.80, 200, 250);
                ReducirAnchoOc(fechaDesde, 0.80, 108, 155);
                ReducirAnchoOc(fechaHasta, 0.80, 108, 155);
                ReducirAnchoOc(quitarFiltros, 0.90, 88, 100);

                if (quitarFiltros != null)
                    quitarFiltros.Padding = new Thickness(10, 0, 10, 0);

                if (nuevo != null)
                    AplicarEstiloNuevaOc(nuevo);

                vista.SetValue(AjusteFiltrosOcAplicadoProperty, true);
            }), DispatcherPriority.Loaded);
        }

        private static void ReducirAnchoOc(FrameworkElement? control, double factor, double minimo, double respaldo)
        {
            if (control == null)
                return;

            double baseAncho = control.ActualWidth > 0
                ? control.ActualWidth
                : (!double.IsNaN(control.Width) && control.Width > 0 ? control.Width : respaldo);
            double nuevoAncho = Math.Max(minimo, Math.Round(baseAncho * factor));
            control.Width = nuevoAncho;
            control.MinWidth = nuevoAncho;
        }

        private static void AplicarEstiloNuevaOc(Button boton)
        {
            boton.Width = 112;
            boton.Height = 36;
            boton.Margin = new Thickness(0, 0, 10, 0);
            boton.Padding = new Thickness(12, 0, 12, 0);
            boton.Background = new SolidColorBrush(Color.FromRgb(15, 118, 110));
            boton.BorderBrush = new SolidColorBrush(Color.FromRgb(15, 118, 110));
            boton.Foreground = Brushes.White;
            boton.FontWeight = FontWeights.SemiBold;
            boton.Cursor = System.Windows.Input.Cursors.Hand;

            StackPanel contenido = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            contenido.Children.Add(new TextBlock
            {
                Text = "\uE710",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 15,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            contenido.Children.Add(new TextBlock
            {
                Text = "Nueva OC",
                VerticalAlignment = VerticalAlignment.Center
            });
            boton.Content = contenido;
        }

        private static T? BuscarControlPorEtiquetaOc<T>(DependencyObject origen, string etiqueta)
            where T : FrameworkElement
        {
            TextBlock? textoEtiqueta = BuscarTextoVisualOc(origen, etiqueta);
            if (textoEtiqueta == null)
                return null;

            DependencyObject? contenedor = VisualTreeHelper.GetParent(textoEtiqueta);
            return contenedor == null ? null : BuscarPrimerControlOc<T>(contenedor);
        }

        private static TextBlock? BuscarTextoVisualOc(DependencyObject origen, string etiqueta)
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);
                if (hijo is TextBlock texto
                    && string.Equals(texto.Text?.Trim(), etiqueta, StringComparison.OrdinalIgnoreCase))
                {
                    return texto;
                }

                TextBlock? resultado = BuscarTextoVisualOc(hijo, etiqueta);
                if (resultado != null)
                    return resultado;
            }

            return null;
        }

        private static T? BuscarPrimerControlOc<T>(DependencyObject origen)
            where T : FrameworkElement
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);
                if (hijo is T control)
                    return control;

                T? resultado = BuscarPrimerControlOc<T>(hijo);
                if (resultado != null)
                    return resultado;
            }

            return null;
        }

        private static Button? BuscarBotonOc(DependencyObject origen, Func<string, bool> condicion)
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);
                if (hijo is Button boton && condicion(ObtenerTextoBotonOc(boton)))
                    return boton;

                Button? resultado = BuscarBotonOc(hijo, condicion);
                if (resultado != null)
                    return resultado;
            }

            return null;
        }

        private static string ObtenerTextoBotonOc(Button boton)
        {
            if (boton.Content is string texto)
                return texto.Trim();
            if (boton.Content is TextBlock bloque)
                return bloque.Text.Trim();
            if (boton.Content is Panel panel)
            {
                return string.Join(
                    " ",
                    panel.Children
                        .OfType<TextBlock>()
                        .Select(x => x.Text?.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            if (boton.Content is DependencyObject contenido)
            {
                TextBlock? textoInterno = BuscarPrimerControlOc<TextBlock>(contenido);
                return textoInterno?.Text.Trim() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
