using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ServicioExternoWindow : Window
    {
        private readonly bool _retorno;
        private readonly decimal _maximo;
        private readonly List<OrdenTrabajoServicioExternoDetalle> _retornos = [];
        public string Proveedor => ProveedorTextBox.Text.Trim();
        public DateTime? FechaRetornoEsperada => FechaDatePicker.SelectedDate;
        public decimal Cantidad { get; private set; }
        public string Observacion => ObservacionTextBox.Text.Trim();
        public bool EsRetornoMultiple => _retornos.Count > 0;
        public IReadOnlyList<OrdenTrabajoServicioExternoDetalle> Retornos => _retornos;

        public ServicioExternoWindow(IEnumerable<OrdenTrabajoDetalleArea> areas, bool retorno)
        {
            InitializeComponent();
            _retorno = retorno;
            List<OrdenTrabajoDetalleArea> lista = areas.ToList();
            _maximo = lista.Sum(x => x.CantidadOperacion);
            TituloText.Text = "Enviar a servicio externo";
            ProductoText.Text = string.Join(Environment.NewLine, lista.Select(x => $"{x.CodigoProducto} - {x.NombreProducto}: {x.CantidadOperacion:N0} unidades desde {x.NombreArea}"));
            FechaDatePicker.SelectedDate = DateTime.Today.AddDays(7);
            CantidadTextBox.Text = _maximo.ToString("0", CultureInfo.CurrentCulture);
            Loaded += (_, _) => ProveedorTextBox.Focus();
        }

        public ServicioExternoWindow(OrdenTrabajoServicioExternoDetalle detalle)
        {
            InitializeComponent();
            _retorno = true;
            _maximo = detalle.SaldoExterno;
            TituloText.Text = "Registrar retorno de servicio externo";
            ProductoText.Text = $"{detalle.CodigoProducto} - {detalle.NombreProducto}\nProveedor: {detalle.Proveedor}\nEnviado: {detalle.CantidadEnviada:N0} | Retornado: {detalle.CantidadRetornada:N0} | Saldo: {detalle.SaldoExterno:N0}";
            ProveedorPanel.Visibility = Visibility.Collapsed;
            FechaPanel.Visibility = Visibility.Collapsed;
            CantidadLabel.Text = "Cantidad retornada:";
            CantidadTextBox.Text = _maximo.ToString("0", CultureInfo.CurrentCulture);
            Loaded += (_, _) => CantidadTextBox.Focus();
        }

        public ServicioExternoWindow(IEnumerable<OrdenTrabajoServicioExternoDetalle> detalles)
        {
            InitializeComponent();
            _retorno = true;
            _retornos = detalles.Where(x => x.SaldoExterno > 0).ToList();
            if (_retornos.Count == 0)
                throw new InvalidOperationException("No hay productos seleccionados con saldo pendiente de retorno.");

            TituloText.Text = "Registrar retornos de servicio externo";
            ProductoText.Text = string.Join(Environment.NewLine,
                _retornos.Select(x => $"{x.CodigoProducto} - {x.NombreProducto}: {x.SaldoExterno:N0} unidades | {x.Proveedor}"));
            ProveedorPanel.Visibility = Visibility.Collapsed;
            FechaPanel.Visibility = Visibility.Collapsed;
            CantidadPanel.Visibility = Visibility.Collapsed;
            Loaded += (_, _) => ObservacionTextBox.Focus();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            if (!_retorno && string.IsNullOrWhiteSpace(Proveedor))
            {
                MessageBox.Show(this, "Ingrese el proveedor o servicio externo.", "Dato requerido", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (EsRetornoMultiple)
            {
                DialogResult = true;
                return;
            }
            if (!decimal.TryParse(CantidadTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal cantidad) || cantidad <= 0 || cantidad > _maximo)
            {
                MessageBox.Show(this, $"Ingrese una cantidad entera mayor que cero y no mayor que {_maximo:N0}.", "Cantidad inválida", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            Cantidad = cantidad;
            DialogResult = true;
        }
    }
}
