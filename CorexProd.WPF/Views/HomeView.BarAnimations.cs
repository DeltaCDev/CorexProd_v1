using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace CorexProd.WPF.Views
{
    public partial class HomeView
    {
        private const double AlturaContenedorBarra = 112d;
        private const double AnchoBarraNormal = 30d;
        private const double AnchoBarraHover = 36d;

        private static readonly object SincronizacionAnimacionBarras = new();
        private static bool _animacionBarrasRegistrada;

        // El inicializador se ejecuta al crear HomeView y registra una sola vez
        // los eventos para todos los gráficos mensuales del dashboard.
        private readonly object _registroAnimacionBarras = RegistrarAnimacionBarras();

        private static object RegistrarAnimacionBarras()
        {
            lock (SincronizacionAnimacionBarras)
            {
                if (_animacionBarrasRegistrada)
                    return new object();

                EventManager.RegisterClassHandler(
                    typeof(Grid),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(ContenedorBarra_Loaded),
                    true);

                EventManager.RegisterClassHandler(
                    typeof(Grid),
                    Mouse.MouseEnterEvent,
                    new MouseEventHandler(ContenedorBarra_MouseEnter),
                    true);

                EventManager.RegisterClassHandler(
                    typeof(Grid),
                    Mouse.MouseLeaveEvent,
                    new MouseEventHandler(ContenedorBarra_MouseLeave),
                    true);

                _animacionBarrasRegistrada = true;
                return new object();
            }
        }

        private static void ContenedorBarra_Loaded(object sender, RoutedEventArgs e)
        {
            if (!EsContenedorBarra(sender, out Grid? contenedor, out BarraDashboard? dato))
                return;

            if (contenedor.Tag as string == "BarraDashboardAnimada")
                return;

            contenedor.Tag = "BarraDashboardAnimada";
            contenedor.Background = new SolidColorBrush(Colors.Transparent);
            contenedor.Cursor = Cursors.Hand;
            contenedor.ToolTip = $"{dato.Mes}: {dato.Total:N0} órdenes";
            contenedor.RenderTransformOrigin = new Point(0.5, 0.55);

            TransformGroup transformaciones = new();
            transformaciones.Children.Add(new ScaleTransform(1, 1));
            transformaciones.Children.Add(new TranslateTransform(0, 0));
            contenedor.RenderTransform = transformaciones;

            Border? barra = BuscarBarraVisual(contenedor);
            if (barra != null)
            {
                barra.RenderTransformOrigin = new Point(0.5, 1);
                barra.Effect = new DropShadowEffect
                {
                    BlurRadius = 8,
                    ShadowDepth = 2,
                    Direction = 270,
                    Color = Color.FromRgb(51, 65, 85),
                    Opacity = 0
                };
            }

            PrepararTextos(contenedor);
        }

        private static void ContenedorBarra_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!EsContenedorBarra(sender, out Grid? contenedor, out _))
                return;

            AnimarContenedor(contenedor, esHover: true);
        }

        private static void ContenedorBarra_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!EsContenedorBarra(sender, out Grid? contenedor, out _))
                return;

            AnimarContenedor(contenedor, esHover: false);
        }

        private static bool EsContenedorBarra(object sender, out Grid? contenedor, out BarraDashboard? dato)
        {
            contenedor = sender as Grid;
            dato = contenedor?.DataContext as BarraDashboard;

            return contenedor != null
                && dato != null
                && !double.IsNaN(contenedor.Height)
                && Math.Abs(contenedor.Height - AlturaContenedorBarra) < 0.1;
        }

        private static void AnimarContenedor(Grid contenedor, bool esHover)
        {
            Duration duracion = new(TimeSpan.FromMilliseconds(esHover ? 170 : 140));
            IEasingFunction easing = new CubicEase
            {
                EasingMode = esHover ? EasingMode.EaseOut : EasingMode.EaseInOut
            };

            if (contenedor.RenderTransform is TransformGroup grupo)
            {
                if (grupo.Children.OfType<ScaleTransform>().FirstOrDefault() is ScaleTransform escala)
                {
                    escala.BeginAnimation(
                        ScaleTransform.ScaleXProperty,
                        CrearAnimacion(esHover ? 1.035 : 1d, duracion, easing));
                    escala.BeginAnimation(
                        ScaleTransform.ScaleYProperty,
                        CrearAnimacion(esHover ? 1.035 : 1d, duracion, easing));
                }

                if (grupo.Children.OfType<TranslateTransform>().FirstOrDefault() is TranslateTransform traslado)
                {
                    traslado.BeginAnimation(
                        TranslateTransform.YProperty,
                        CrearAnimacion(esHover ? -4d : 0d, duracion, easing));
                }
            }

            if (contenedor.Background is SolidColorBrush fondo)
            {
                Color destino = esHover
                    ? Color.FromArgb(18, 37, 99, 235)
                    : Colors.Transparent;
                fondo.BeginAnimation(
                    SolidColorBrush.ColorProperty,
                    new ColorAnimation(destino, duracion) { EasingFunction = easing });
            }

            Border? barra = BuscarBarraVisual(contenedor);
            if (barra != null)
            {
                barra.BeginAnimation(
                    FrameworkElement.WidthProperty,
                    CrearAnimacion(esHover ? AnchoBarraHover : AnchoBarraNormal, duracion, easing));
                barra.BeginAnimation(
                    UIElement.OpacityProperty,
                    CrearAnimacion(esHover ? 1d : 0.82d, duracion, easing));

                if (barra.Effect is DropShadowEffect sombra)
                {
                    sombra.BeginAnimation(
                        DropShadowEffect.OpacityProperty,
                        CrearAnimacion(esHover ? 0.28d : 0d, duracion, easing));
                    sombra.BeginAnimation(
                        DropShadowEffect.BlurRadiusProperty,
                        CrearAnimacion(esHover ? 15d : 8d, duracion, easing));
                }
            }

            TextBlock[] textos = contenedor.Children.OfType<TextBlock>().ToArray();
            if (textos.Length > 0)
            {
                TextBlock total = textos[0];
                total.BeginAnimation(
                    TextBlock.FontSizeProperty,
                    CrearAnimacion(esHover ? 10.5d : 9d, duracion, easing));
                AnimarColor(total, esHover ? Color.FromRgb(15, 29, 58) : Color.FromRgb(51, 65, 95), duracion, easing);
            }

            if (textos.Length > 1)
            {
                TextBlock mes = textos[^1];
                mes.BeginAnimation(
                    TextBlock.FontSizeProperty,
                    CrearAnimacion(esHover ? 9.4d : 8.5d, duracion, easing));
                AnimarColor(mes, esHover ? Color.FromRgb(37, 99, 235) : Color.FromRgb(100, 116, 139), duracion, easing);
            }
        }

        private static DoubleAnimation CrearAnimacion(
            double destino,
            Duration duracion,
            IEasingFunction easing) => new(destino, duracion)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            };

        private static void PrepararTextos(Grid contenedor)
        {
            foreach (TextBlock texto in contenedor.Children.OfType<TextBlock>())
            {
                if (texto.Foreground is SolidColorBrush pincel)
                    texto.Foreground = new SolidColorBrush(pincel.Color);
            }
        }

        private static void AnimarColor(
            TextBlock texto,
            Color destino,
            Duration duracion,
            IEasingFunction easing)
        {
            if (texto.Foreground is not SolidColorBrush pincel)
                return;

            pincel.BeginAnimation(
                SolidColorBrush.ColorProperty,
                new ColorAnimation(destino, duracion) { EasingFunction = easing });
        }

        private static Border? BuscarBarraVisual(DependencyObject origen)
        {
            int cantidadHijos = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidadHijos; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);
                if (hijo is Border borde
                    && borde.DataContext is BarraDashboard
                    && Math.Abs(borde.Width - AnchoBarraNormal) < 0.1
                    && borde.VerticalAlignment == VerticalAlignment.Bottom)
                {
                    return borde;
                }

                Border? resultado = BuscarBarraVisual(hijo);
                if (resultado != null)
                    return resultado;
            }

            return null;
        }
    }
}
