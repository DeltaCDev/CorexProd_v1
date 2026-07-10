using CorexProd.Datos.Datos;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CorexProd.WPF.Views
{
    public partial class HomeView
    {
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
            TextBlock? tituloSeguimiento = BuscarDescendiente<TextBlock>(
                this,
                texto => string.Equals(texto.Text, "Seguimiento de fechas de entrega", StringComparison.Ordinal));

            if (tituloSeguimiento == null)
                return false;

            Grid? resumenOperativo = BuscarAncestro<Grid>(tituloSeguimiento, grid =>
                grid.Children.OfType<Border>().Any(borde => Grid.GetColumn(borde) == 2));

            if (resumenOperativo == null || VisualTreeHelper.GetParent(resumenOperativo) is not StackPanel contenedorPrincipal)
                return false;

            Border tarjeta = new()
            {
                Style = TryFindResource("Card") as Style,
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel contenido = new();
            tarjeta.Child = contenido;

            Grid cabecera = new() { Margin = new Thickness(0, 0, 0, 10) };
            cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cabecera.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock icono = new()
            {
                Text = "\uE787",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 19,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Margin = new Thickness(0, 0, 8, 0),
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
                Text = "Las 3 órdenes de compra con fecha de entrega más cercana",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 2, 0, 0)
            });
            cabecera.Children.Add(textosCabecera);

            TextBlock etiqueta = new()
            {
                Text = "Próximas 3",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(etiqueta, 2);
            cabecera.Children.Add(etiqueta);
            contenido.Children.Add(cabecera);

            _proximasEntregasGrid = new UniformGrid { Columns = 3 };
            contenido.Children.Add(_proximasEntregasGrid);

            _proximasEntregasVacio = new TextBlock
            {
                Text = "No hay órdenes de compra próximas a vencer.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 8),
                Visibility = Visibility.Collapsed
            };
            contenido.Children.Add(_proximasEntregasVacio);

            int indice = contenedorPrincipal.Children.IndexOf(resumenOperativo);
            contenedorPrincipal.Children.Insert(indice + 1, tarjeta);
            return true;
        }

        private void ActualizarTarjetaProximasEntregas()
        {
            if (_proximasEntregasGrid == null || _proximasEntregasVacio == null)
                return;

            var proximas = AlertasEntrega
                .Where(alerta => alerta.DiasRestantes >= 0)
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

        private Button CrearItemProximaEntrega(OrdenCompraAlertaEntrega alerta)
        {
            Brush acento = ConvertirColor(alerta.Color, Color.FromRgb(37, 99, 235));
            Brush fondo = alerta.DiasRestantes switch
            {
                0 => new SolidColorBrush(Color.FromRgb(255, 244, 232)),
                <= 3 => new SolidColorBrush(Color.FromRgb(255, 248, 222)),
                _ => new SolidColorBrush(Color.FromRgb(238, 244, 255))
            };

            Button boton = new()
            {
                DataContext = alerta,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                ToolTip = "Abrir detalle de la orden de compra"
            };
            boton.Click += VerOcAlerta_Click;

            Border borde = new()
            {
                Background = fondo,
                BorderBrush = acento,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 9)
            };
            boton.Content = borde;

            StackPanel contenido = new();
            borde.Child = contenido;

            Grid filaSuperior = new();
            filaSuperior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            filaSuperior.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            filaSuperior.Children.Add(new TextBlock
            {
                Text = alerta.NumeroOci,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 29, 58)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            TextBlock fecha = new()
            {
                Text = alerta.FechaEntrega.ToString("dd/MM/yyyy"),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = acento,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(fecha, 1);
            filaSuperior.Children.Add(fecha);
            contenido.Children.Add(filaSuperior);

            contenido.Children.Add(new TextBlock
            {
                Text = alerta.NombreCliente,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 5, 0, 7)
            });

            Border estado = new()
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            estado.Child = new TextBlock
            {
                Text = alerta.AlertaTexto,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = acento
            };
            contenido.Children.Add(estado);

            return boton;
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
