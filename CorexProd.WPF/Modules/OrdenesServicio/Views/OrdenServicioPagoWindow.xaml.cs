using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CorexProd.WPF.Modules.OrdenesServicio.Views
{
    public partial class OrdenServicioPagoWindow : Window, INotifyPropertyChanged
    {
        public OrdenServicio Orden { get; }
        public ObservableCollection<FormaPagoOs> FormasPago { get; } = [];
        private FormaPagoOs? _formaSeleccionada;
        public decimal Importe { get; set; }
        public string DestinoPago { get; set; } = string.Empty;
        public string NumeroOperacion { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public bool Confirmado { get; private set; }
        public OrdenServicioPago? Pago { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public FormaPagoOs? FormaSeleccionada
        {
            get => _formaSeleccionada;
            set
            {
                _formaSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EsEfectivo));
                OnPropertyChanged(nameof(EsTransferencia));
                OnPropertyChanged(nameof(DestinoLabel));
                OnPropertyChanged(nameof(DestinoVisibility));
                OnPropertyChanged(nameof(OperacionVisibility));
            }
        }

        public bool EsEfectivo => FormaSeleccionada?.Nombre.Equals("EFECTIVO", StringComparison.OrdinalIgnoreCase) == true;
        public bool EsTransferencia => FormaSeleccionada?.Nombre.Equals("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase) == true;
        public string DestinoLabel
        {
            get
            {
                string nombre = FormaSeleccionada?.Nombre ?? string.Empty;
                if (nombre.Equals("YAPE", StringComparison.OrdinalIgnoreCase))
                    return "Numero Yape";
                if (nombre.Equals("PLIN", StringComparison.OrdinalIgnoreCase))
                    return "Numero Plin";
                return EsTransferencia ? "Cuenta" : "Numero";
            }
        }
        public Visibility DestinoVisibility => EsEfectivo ? Visibility.Collapsed : Visibility.Visible;
        public Visibility OperacionVisibility => EsEfectivo ? Visibility.Collapsed : Visibility.Visible;

        public OrdenServicioPagoWindow(OrdenServicio orden)
        {
            InitializeComponent();
            Orden = orden;
            Importe = Math.Round(orden.SaldoPendiente, 2);
            CargarFormasPago();
            DataContext = this;
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (Importe <= 0)
            {
                NotificationService.Warning("El importe debe ser mayor a cero.");
                return;
            }

            if (Importe > Orden.SaldoPendiente)
            {
                NotificationService.Warning("El importe no puede exceder el saldo pendiente.");
                return;
            }

            if (FormaSeleccionada == null)
            {
                NotificationService.Warning("Seleccione la forma de pago.");
                return;
            }

            if (!EsEfectivo && string.IsNullOrWhiteSpace(DestinoPago))
            {
                NotificationService.Warning(EsTransferencia ? "Ingrese la cuenta de transferencia." : "Ingrese el numero.");
                return;
            }

            string destino = DestinoPago.Trim();
            string observacion = Observacion.Trim();
            if (!EsEfectivo)
            {
                string etiqueta = DestinoLabel;
                observacion = string.IsNullOrWhiteSpace(observacion)
                    ? $"{etiqueta}: {destino}"
                    : $"{etiqueta}: {destino} | {observacion}";
            }

            Pago = new OrdenServicioPago
            {
                IdOrdenServicio = Orden.IdOrdenServicio,
                Fecha = DateTime.Today,
                TipoPago = Importe >= Orden.SaldoPendiente ? "Pago final" : "Pago parcial",
                Importe = Math.Round(Importe, 2),
                MedioPago = FormaSeleccionada.Nombre.Trim(),
                NumeroOperacion = NumeroOperacion.Trim(),
                Observacion = observacion
            };
            Confirmado = true;
            DialogResult = true;
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void FormaPago_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (EsEfectivo)
            {
                DestinoPago = string.Empty;
                NumeroOperacion = string.Empty;
                OnPropertyChanged(nameof(DestinoPago));
                OnPropertyChanged(nameof(NumeroOperacion));
            }
        }

        private void CargarFormasPago()
        {
            try
            {
                foreach (FormaPagoOs forma in new FormaPagoOsNegocio().Listar(soloActivos: true))
                    FormasPago.Add(forma);
            }
            catch
            {
                FormasPago.Add(new FormaPagoOs { Nombre = "YAPE", Estado = true });
                FormasPago.Add(new FormaPagoOs { Nombre = "PLIN", Estado = true });
                FormasPago.Add(new FormaPagoOs { Nombre = "TRANSFERENCIA", Estado = true });
                FormasPago.Add(new FormaPagoOs { Nombre = "EFECTIVO", Estado = true });
            }

            FormaSeleccionada = FormasPago.FirstOrDefault(x => x.Nombre.Equals("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase))
                ?? FormasPago.FirstOrDefault();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
