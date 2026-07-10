using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CorexProd.WPF.Views
{
    public partial class HomeView
    {
        private static readonly HashSet<string> EtiquetasHoverFilas = new(StringComparer.OrdinalIgnoreCase)
        {
            "Generadas",
            "Pendiente / Producción",
            "Con OT activa",
            "Con guía",
            "Entregadas",
            "Anuladas",
            "Pendientes / En proceso",
            "Terminadas",
            "Manuales",
            "Por OCI",
            "Vencidas",
            "Vencen hoy",
            "Próximas (1-3 días)",
            "Dentro de 4-7 días",
            "Más de 7 días"
        };

        private static readonly object SincronizacionHoverFilas = new();
        private static readonly ConditionalWeakTable<Border, EstadoHoverFila> EstadosHoverFilas = new();
        private static bool _hoverFilasRegistrado;

        // El inicializador registra una sola vez el comportamiento para las filas del dashboard.
        private readonly object _registroHoverFilasMejorado = RegistrarHoverFilasMejorado();

        private static object RegistrarHoverFilasMejorado()
        {
            lock (SincronizacionHoverFilas)
            {
                if (_hoverFilasRegistrado)
                    return new object();

                EventManager.RegisterClassHandler(
                    typeof(Border),
                    UIElement.MouseEnterEvent,
                    new MouseEventHandler(FilaDashboard_MouseEnter),
                    true);

                EventManager.RegisterClassHandler(
                    typeof(Border),
                    UIElement.MouseLeaveEvent,
                    new MouseEventHandler(FilaDashboard_MouseLeave),
                    true);

                _hoverFilasRegistrado = true;
                return new object();
            }
        }

        private static void FilaDashboard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Border fila || !EsFilaDashboardObjetivo(fila))
                return;

            e.Handled = true;
            EstadoHoverFila estado = ObtenerEstadoHoverFila(fila);

            fila.Opacity = 1;
            fila.Effect = null;
            Panel.SetZIndex(fila, 20);

            Duration duracion = new(TimeSpan.FromMilliseconds(145));
            CubicEase easing = new() { EasingMode = EasingMode.EaseOut };

            estado.Escala.BeginAnimation(
                ScaleTransform.ScaleXProperty,
                CrearAnimacionFila(1.012, duracion, easing));
            estado.Escala.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                CrearAnimacionFila(1.018, duracion, easing));
            estado.Desplazamiento.BeginAnimation(
                TranslateTransform.YProperty,
                CrearAnimacionFila(-1.5, duracion, easing));

            Thickness paddingHover = new(
                estado.PaddingOriginal.Left + 2,
                estado.PaddingOriginal.Top + 2,
                estado.PaddingOriginal.Right + 2,
                estado.PaddingOriginal.Bottom + 2);

            fila.BeginAnimation(
                Border.PaddingProperty,
                new ThicknessAnimation(paddingHover, duracion)
                {
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.HoldEnd
                },
                HandoffBehavior.SnapshotAndReplace);
        }

        private static void FilaDashboard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not Border fila || !EstadosHoverFilas.TryGetValue(fila, out EstadoHoverFila? estado))
                return;

            e.Handled = true;
            fila.Opacity = 1;
            fila.Effect = null;

            Duration duracion = new(TimeSpan.FromMilliseconds(165));
            CubicEase easing = new() { EasingMode = EasingMode.EaseInOut };

            estado.Escala.BeginAnimation(
                ScaleTransform.ScaleXProperty,
                CrearAnimacionFila(1, duracion, easing));
            estado.Escala.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                CrearAnimacionFila(1, duracion, easing));
            estado.Desplazamiento.BeginAnimation(
                TranslateTransform.YProperty,
                CrearAnimacionFila(0, duracion, easing));

            ThicknessAnimation restaurarPadding = new(estado.PaddingOriginal, duracion)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop
            };
            restaurarPadding.Completed += (_, _) =>
            {
                fila.BeginAnimation(Border.PaddingProperty, null);
                fila.Padding = estado.PaddingOriginal;
                Panel.SetZIndex(fila, 0);
            };

            fila.BeginAnimation(
                Border.PaddingProperty,
                restaurarPadding,
                HandoffBehavior.SnapshotAndReplace);
        }

        private static EstadoHoverFila ObtenerEstadoHoverFila(Border fila)
        {
            if (EstadosHoverFilas.TryGetValue(fila, out EstadoHoverFila? existente))
                return existente;

            fila.Opacity = 1;
            fila.Effect = null;
            fila.SnapsToDevicePixels = true;
            fila.UseLayoutRounding = true;
            fila.RenderTransformOrigin = new Point(0.5, 0.5);

            ScaleTransform escala = new(1, 1);
            TranslateTransform desplazamiento = new(0, 0);
            TransformGroup transformaciones = new();
            transformaciones.Children.Add(escala);
            transformaciones.Children.Add(desplazamiento);
            fila.RenderTransform = transformaciones;

            EstadoHoverFila estado = new(escala, desplazamiento, fila.Padding);
            EstadosHoverFilas.Add(fila, estado);
            return estado;
        }

        private static bool EsFilaDashboardObjetivo(Border fila)
        {
            if (fila.ActualHeight <= 0 || fila.ActualHeight > 60 || fila.Child is not Grid)
                return false;

            if (BuscarAncestroHover<HomeView>(fila) == null)
                return false;

            int coincidencias = ContarEtiquetasHover(fila, limite: 2);
            return coincidencias == 1;
        }

        private static int ContarEtiquetasHover(DependencyObject origen, int limite)
        {
            int coincidencias = 0;
            int cantidadHijos = VisualTreeHelper.GetChildrenCount(origen);

            for (int i = 0; i < cantidadHijos; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);

                if (hijo is TextBlock texto
                    && EtiquetasHoverFilas.Contains(NormalizarEtiquetaHover(texto.Text)))
                {
                    coincidencias++;
                    if (coincidencias >= limite)
                        return coincidencias;
                }

                coincidencias += ContarEtiquetasHover(hijo, limite - coincidencias);
                if (coincidencias >= limite)
                    return coincidencias;
            }

            return coincidencias;
        }

        private static string NormalizarEtiquetaHover(string texto) =>
            texto.Replace("●", string.Empty, StringComparison.Ordinal).Trim();

        private static T? BuscarAncestroHover<T>(DependencyObject origen)
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

        private static DoubleAnimation CrearAnimacionFila(
            double destino,
            Duration duracion,
            IEasingFunction easing) => new(destino, duracion)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            };

        private sealed class EstadoHoverFila
        {
            public EstadoHoverFila(
                ScaleTransform escala,
                TranslateTransform desplazamiento,
                Thickness paddingOriginal)
            {
                Escala = escala;
                Desplazamiento = desplazamiento;
                PaddingOriginal = paddingOriginal;
            }

            public ScaleTransform Escala { get; }
            public TranslateTransform Desplazamiento { get; }
            public Thickness PaddingOriginal { get; }
        }
    }
}
