using CorexProd.Datos.Datos;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace CorexProd.WPF.Views
{
    public partial class HomeView
    {
        private const double AlturaTarjetasEstadisticas = 220;

        private static readonly HashSet<string> EtiquetasFilasAnimables = new(StringComparer.OrdinalIgnoreCase)
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

        private readonly HashSet<Border> _elementosAnimados = new();
        private bool _tarjetaProximasEntregasInicializada;
        private bool _actualizacionProximasPendiente;
        private UniformGrid? _proximasEntregasGrid;
        private TextBlock? _proximasEntregasVacio;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Loaded += HomeView_ProximasEntregasLoaded;
        }

        private void HomeView_ProximasEntregasLoaded(object sender, RoutedEventArgs e)
        {
            if (!_tarjetaProximasEntregasInicializada)
            {
                if (!AgregarTarjetaProximasEntregas())
                    return;

                AlertasEntrega.CollectionChanged += AlertasEntrega_CollectionChanged;
                _tarjetaProximasEntregasInicializada = true;
            }

            ActualizarTarjetaProximasEntregas();
            Dispatcher.BeginInvoke(new Action(ConfigurarAnimacionesDashboard), DispatcherPriority.Loaded);
        }

        private void AlertasEntrega_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_actualizacionProximasPendiente)
                return;

            _actualizacionProximasPendiente = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _actualizacionProximasPendiente = false;
                ActualizarTarjetaProximasEntregas();
            }), DispatcherPriority.DataBind);
        }

        private bool AgregarTarjetaProximasEntregas()
        {
            TextBlock? tituloEstadistica = BuscarDescendiente<TextBlock>(
                this,
                texto => string.Equals(texto.Text, "OCI últimos 6 meses", StringComparison.Ordinal));

            if (tituloEstadistica == null)
                return false;

            Grid? filaEstadisticas = BuscarAncestro<Grid>(tituloEstadistica, grid =>
                grid.ColumnDefinitions.Count == 2
                && grid.Children.OfType<Border>().Count() >= 2);

            if (filaEstadisticas == null)
                return false;

            filaEstadisticas.MinHeight = AlturaTarjetasEstadisticas;
            filaEstadisticas.VerticalAlignment = VerticalAlignment.Stretch;

            foreach (Border tarjetaEstadistica in filaEstadisticas.Children.OfType<Border>())
            {
                tarjetaEstadistica.MinHeight = AlturaTarjetasEstadisticas;
                tarjetaEstadistica.VerticalAlignment = VerticalAlignment.Stretch;
                tarjetaEstadistica.ClipToBounds = true;
            }

            if (filaEstadisticas.ColumnDefinitions.Count == 2)
                filaEstadisticas.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border? tarjetaOt = filaEstadisticas.Children
                .OfType<Border>()
                .FirstOrDefault(borde => Grid.GetColumn(borde) == 1);
            if (tarjetaOt != null)
                tarjetaOt.Margin = new Thickness(0, 0, 10, 0);

            Border tarjeta = new()
            {
                Style = TryFindResource("Card") as Style,
                MinHeight = AlturaTarjetasEstadisticas,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true
            };
            Grid.SetColumn(tarjeta, 2);

            Grid contenido = new();
            contenido.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contenido.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            tarjeta.Child = contenido;

            Grid cabecera = new() { Margin = new Thickness(0, 0, 0, 8) };
            cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock icono = new()
            {
                Text = "\uE787",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            cabecera.Children.Add(icono);

            StackPanel textosCabecera = new();
            Grid.SetColumn(textosCabecera, 1);
            textosCabecera.Children.Add(new TextBlock
            {
                Text = "Próximas entregas",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 29, 58))
            });
            textosCabecera.Children.Add(new TextBlock
            {
                Text = "3 OC cliente con fecha más cercana",
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 1, 0, 0)
            });
            cabecera.Children.Add(textosCabecera);

            TextBlock etiqueta = new()
            {
                Text = "Top 3",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(etiqueta, 2);
            cabecera.Children.Add(etiqueta);
            contenido.Children.Add(cabecera);

            _proximasEntregasGrid = new UniformGrid
            {
                Columns = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Grid.SetRow(_proximasEntregasGrid, 1);
            contenido.Children.Add(_proximasEntregasGrid);

            _proximasEntregasVacio = new TextBlock
            {
                Text = "No hay OC cliente próximas a vencer.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(_proximasEntregasVacio, 1);
            contenido.Children.Add(_proximasEntregasVacio);

            filaEstadisticas.Children.Add(tarjeta);
            return true;
        }

        private void ActualizarTarjetaProximasEntregas()
        {
            if (_proximasEntregasGrid == null || _proximasEntregasVacio == null)
                return;

            DateTime hoy = DateTime.Today;
            var proximas = _ordenesCompra
                .Where(orden => orden.FechaEntrega != default && orden.FechaEntrega.Date >= hoy)
                .Where(orden => NormalizarEstado(orden.Estado) is not ("ENTREGADO" or "ENTREGADA" or "ANULADO" or "ANULADA"))
                .Select(orden => new OrdenCompraAlertaEntrega
                {
                    IdOrdenCompraInterna = orden.IdOrdenCompraInterna,
                    NumeroOci = string.IsNullOrWhiteSpace(orden.OrdenCompraCliente)
                        ? "Sin OC cliente"
                        : orden.OrdenCompraCliente.Trim(),
                    NombreCliente = orden.NombreCliente,
                    Estado = orden.Estado,
                    FechaEntrega = orden.FechaEntrega.Date,
                    DiasRestantes = (orden.FechaEntrega.Date - hoy).Days
                })
                .OrderBy(alerta => alerta.DiasRestantes)
                .ThenBy(alerta => alerta.FechaEntrega)
                .ThenBy(alerta => alerta.IdOrdenCompraInterna)
                .Take(3)
                .ToList();

            _proximasEntregasGrid.Children.Clear();
            foreach (OrdenCompraAlertaEntrega alerta in proximas)
                _proximasEntregasGrid.Children.Add(CrearItemProximaEntrega(alerta));

            _proximasEntregasGrid.Visibility = proximas.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            _proximasEntregasVacio.Visibility = proximas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private Border CrearItemProximaEntrega(OrdenCompraAlertaEntrega alerta)
        {
            Brush acento = ConvertirColor(alerta.Color, Color.FromRgb(37, 99, 235));
            Brush fondo = alerta.DiasRestantes switch
            {
                0 => new SolidColorBrush(Color.FromRgb(255, 247, 237)),
                <= 3 => new SolidColorBrush(Color.FromRgb(255, 251, 235)),
                _ => new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };

            string ordenCompraCliente = string.IsNullOrWhiteSpace(alerta.NumeroOci)
                ? "Sin OC cliente"
                : alerta.NumeroOci;
            string cliente = string.IsNullOrWhiteSpace(alerta.NombreCliente)
                ? "Cliente no especificado"
                : alerta.NombreCliente;

            Border borde = new()
            {
                DataContext = alerta,
                Background = fondo,
                BorderBrush = acento,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 6),
                MinHeight = 44,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Cursor = Cursors.Hand,
                ToolTip = "Abrir detalle de la orden de compra"
            };
            borde.MouseLeftButtonUp += (sender, args) =>
            {
                VerOcAlerta_Click(sender, args);
                args.Handled = true;
            };

            Grid contenido = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            contenido.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contenido.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contenido.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contenido.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            borde.Child = contenido;

            TextBlock codigo = new()
            {
                Text = ordenCompraCliente,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 29, 58)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = ordenCompraCliente
            };
            contenido.Children.Add(codigo);

            TextBlock fecha = new()
            {
                Text = alerta.FechaEntrega.ToString("dd/MM/yyyy"),
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = acento,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(fecha, 1);
            contenido.Children.Add(fecha);

            TextBlock nombreCliente = new()
            {
                Text = cliente,
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 3, 10, 0),
                ToolTip = cliente
            };
            Grid.SetRow(nombreCliente, 1);
            contenido.Children.Add(nombreCliente);

            TextBlock estadoEntrega = new()
            {
                Text = alerta.AlertaTexto,
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = acento,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 3, 0, 0)
            };
            Grid.SetRow(estadoEntrega, 1);
            Grid.SetColumn(estadoEntrega, 1);
            contenido.Children.Add(estadoEntrega);

            AplicarAnimacionHover(borde);
            return borde;
        }

        private void ConfigurarAnimacionesDashboard()
        {
            ConfigurarAnimacionesDashboard(this);
        }

        private void ConfigurarAnimacionesDashboard(DependencyObject origen)
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);

                if (hijo is TextBlock texto
                    && EtiquetasFilasAnimables.Contains(NormalizarEtiquetaAnimable(texto.Text)))
                {
                    Border? fila = BuscarAncestro<Border>(texto, _ => true);
                    if (fila != null)
                        AplicarAnimacionHover(fila);
                }

                ConfigurarAnimacionesDashboard(hijo);
            }
        }

        private static string NormalizarEtiquetaAnimable(string texto) =>
            texto.Replace("●", string.Empty, StringComparison.Ordinal).Trim();

        private void AplicarAnimacionHover(Border borde)
        {
            if (!_elementosAnimados.Add(borde))
                return;

            borde.RenderTransformOrigin = new Point(0.5, 0.5);

            ScaleTransform escala = new(1, 1);
            TranslateTransform desplazamiento = new(0, 0);
            TransformGroup transformaciones = new();
            transformaciones.Children.Add(escala);
            transformaciones.Children.Add(desplazamiento);
            borde.RenderTransform = transformaciones;

            DropShadowEffect sombra = new()
            {
                Color = Color.FromRgb(30, 64, 175),
                BlurRadius = 0,
                ShadowDepth = 0,
                Opacity = 0,
                RenderingBias = RenderingBias.Performance
            };
            borde.Effect = sombra;

            borde.MouseEnter += (_, _) =>
            {
                AnimarPropiedad(escala, ScaleTransform.ScaleXProperty, 1.012, 150);
                AnimarPropiedad(escala, ScaleTransform.ScaleYProperty, 1.012, 150);
                AnimarPropiedad(desplazamiento, TranslateTransform.YProperty, -2, 150);
                AnimarPropiedad(sombra, DropShadowEffect.BlurRadiusProperty, 14, 170);
                AnimarPropiedad(sombra, DropShadowEffect.OpacityProperty, 0.18, 170);
            };

            borde.MouseLeave += (_, _) =>
            {
                AnimarPropiedad(escala, ScaleTransform.ScaleXProperty, 1, 180);
                AnimarPropiedad(escala, ScaleTransform.ScaleYProperty, 1, 180);
                AnimarPropiedad(desplazamiento, TranslateTransform.YProperty, 0, 180);
                AnimarPropiedad(sombra, DropShadowEffect.BlurRadiusProperty, 0, 180);
                AnimarPropiedad(sombra, DropShadowEffect.OpacityProperty, 0, 180);
            };
        }

        private static void AnimarPropiedad(
            Animatable elemento,
            DependencyProperty propiedad,
            double valorDestino,
            int duracionMilisegundos)
        {
            DoubleAnimation animacion = new()
            {
                To = valorDestino,
                Duration = TimeSpan.FromMilliseconds(duracionMilisegundos),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            };

            elemento.BeginAnimation(propiedad, animacion, HandoffBehavior.SnapshotAndReplace);
        }

        private static Brush ConvertirColor(string valor, Color alternativo)
        {
            try
            {
                return (Brush)new BrushConverter().ConvertFromString(valor)!;
            }
            catch
            {
                return new SolidColorBrush(alternativo);
            }
        }

        private static T? BuscarDescendiente<T>(DependencyObject origen, Func<T, bool> condicion)
            where T : DependencyObject
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);
                if (hijo is T encontrado && condicion(encontrado))
                    return encontrado;

                T? resultado = BuscarDescendiente(hijo, condicion);
                if (resultado != null)
                    return resultado;
            }

            return null;
        }

        private static T? BuscarAncestro<T>(DependencyObject origen, Func<T, bool> condicion)
            where T : DependencyObject
        {
            DependencyObject? actual = VisualTreeHelper.GetParent(origen);
            while (actual != null)
            {
                if (actual is T encontrado && condicion(encontrado))
                    return encontrado;
                actual = VisualTreeHelper.GetParent(actual);
            }

            return null;
        }
    }
}
