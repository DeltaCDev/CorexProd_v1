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

            if (filaEstadisticas.ColumnDefinitions.Count == 2)
                filaEstadisticas.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border? tarjetaOt = filaEstadisticas.Children
                .OfType<Border>()
                .FirstOrDefault(borde => Grid.GetColumn(borde) == 1);
            if (tarjetaOt != null)
                tarjetaOt.Margin = new Thickness(0, 0, 10, 0);

            Border tarjeta = new()
            {
                Style = TryFindResource("Card") as Style
            };
            Grid.SetColumn(tarjeta, 2);

            StackPanel contenido = new();
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
                Text = "3 OC con fecha más cercana",
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

            _proximasEntregasGrid = new UniformGrid { Columns = 1 };
            contenido.Children.Add(_proximasEntregasGrid);

            _proximasEntregasVacio = new TextBlock
            {
                Text = "No hay OC próximas a vencer.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 22, 0, 8),
                Visibility = Visibility.Collapsed
            };
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
                    NumeroOci = orden.NumeroOci,
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
                Margin = new Thickness(0, 0, 0, 6),
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
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(9, 6, 9, 6)
            };
            boton.Content = borde;

            Grid contenido = new();
            contenido.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contenido.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            borde.Child = contenido;

            StackPanel datos = new();
            datos.Children.Add(new TextBlock
            {
                Text = alerta.NumeroOci,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 29, 58)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            datos.Children.Add(new TextBlock
            {
                Text = alerta.NombreCliente,
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 8, 0)
            });
            contenido.Children.Add(datos);

            StackPanel entrega = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            entrega.Children.Add(new TextBlock
            {
                Text = alerta.FechaEntrega.ToString("dd/MM/yyyy"),
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = acento,
                HorizontalAlignment = HorizontalAlignment.Right
            });
            entrega.Children.Add(new TextBlock
            {
                Text = alerta.AlertaTexto,
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = acento,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(entrega, 1);
            contenido.Children.Add(entrega);

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
