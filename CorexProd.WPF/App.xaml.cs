using HandyControl.Tools;
using CorexProd.WPF.Helpers;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CorexProd.WPF
{
    public partial class App : Application
    {
        public App()
        {
            CultureInfo cultura = new("es-PE");

            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;

            ConfigHelper.Instance.SetLang("es");

            InitializeComponent();

            EventManager.RegisterClassHandler(
                typeof(Window),
                UIElement.PreviewMouseWheelEvent,
                new MouseWheelEventHandler(DesplazarContenidoBajoCursor),
                true);

            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(AplicarEscalaResponsive),
                true);

            EventManager.RegisterClassHandler(
                typeof(TextBlock),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(AplicarTextoLegible),
                true);

            EventManager.RegisterClassHandler(
                typeof(Control),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(AplicarTextoLegible),
                true);
        }

        private static void AplicarEscalaResponsive(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                ResponsiveScaleHelper.Apply(window);
            }
        }

        private static void AplicarTextoLegible(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject element)
            {
                ReadableTextHelper.Apply(element);
            }
        }

        private static void DesplazarContenidoBajoCursor(object sender, MouseWheelEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject origen)
            {
                return;
            }

            ScrollViewer? scrollViewer = BuscarScrollViewerDisponible(origen, e.Delta);
            if (scrollViewer == null)
            {
                return;
            }

            int lineas = SystemParameters.WheelScrollLines;
            double desplazamiento;

            if (lineas == -1)
            {
                desplazamiento = scrollViewer.ViewportHeight;
            }
            else if (scrollViewer.CanContentScroll)
            {
                desplazamiento = Math.Max(1, lineas);
            }
            else
            {
                desplazamiento = Math.Max(16, lineas * 16);
            }

            double destino = e.Delta > 0
                ? scrollViewer.VerticalOffset - desplazamiento
                : scrollViewer.VerticalOffset + desplazamiento;

            scrollViewer.ScrollToVerticalOffset(destino);
            e.Handled = true;
        }

        private static ScrollViewer? BuscarScrollViewerDisponible(DependencyObject origen, int delta)
        {
            DependencyObject? actual = origen;

            while (actual != null)
            {
                if (actual is ScrollViewer scrollViewer && PuedeDesplazarse(scrollViewer, delta))
                {
                    return scrollViewer;
                }

                actual = ObtenerPadre(actual);
            }

            return null;
        }

        private static bool PuedeDesplazarse(ScrollViewer scrollViewer, int delta)
        {
            if (scrollViewer.ScrollableHeight <= 0)
            {
                return false;
            }

            return delta > 0
                ? scrollViewer.VerticalOffset > 0
                : scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;
        }

        private static DependencyObject? ObtenerPadre(DependencyObject elemento)
        {
            if (elemento is Visual or Visual3D)
            {
                return VisualTreeHelper.GetParent(elemento);
            }

            return LogicalTreeHelper.GetParent(elemento);
        }
    }
}
