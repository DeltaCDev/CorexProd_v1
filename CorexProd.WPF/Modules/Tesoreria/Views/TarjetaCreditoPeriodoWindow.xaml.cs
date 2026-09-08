using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class TarjetaCreditoPeriodoWindow : Window
    {
        private readonly TarjetaCreditoNegocio _negocio = new();
        private readonly TarjetaCredito _tarjeta;
        private readonly TarjetaCreditoPeriodo _periodo;

        public TarjetaCreditoPeriodoWindow(TarjetaCredito tarjeta, TarjetaCreditoPeriodo? periodo)
        {
            InitializeComponent();
            _tarjeta = tarjeta;
            _periodo = periodo ?? new TarjetaCreditoPeriodo
            {
                IdTarjetaCredito = tarjeta.IdTarjetaCredito,
                PeriodoAnio = (short)DateTime.Today.Year,
                PeriodoMes = (byte)DateTime.Today.Month,
                FechaCorte = DateTime.Today,
                FechaVencimiento = DateTime.Today,
                Estado = "ABIERTO"
            };

            AnioTextBox.Text = _periodo.PeriodoAnio.ToString();
            MesTextBox.Text = _periodo.PeriodoMes.ToString();
            CorteDatePicker.SelectedDate = _periodo.FechaCorte;
            VencimientoDatePicker.SelectedDate = _periodo.FechaVencimiento;
            ImportePenTextBox.Text = _periodo.ImportePEN?.ToString("N2") ?? string.Empty;
            ImporteUsdTextBox.Text = _periodo.ImporteUSD?.ToString("N2") ?? string.Empty;
            ObservacionTextBox.Text = _periodo.Observacion;
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (!_tarjeta.EstaActiva && _periodo.IdTarjetaPeriodo == 0)
            {
                NotificationService.Warning("Una tarjeta INACTIVA no puede crear períodos.");
                return;
            }

            if (!short.TryParse(AnioTextBox.Text, out short anio) ||
                !byte.TryParse(MesTextBox.Text, out byte mes) ||
                CorteDatePicker.SelectedDate == null ||
                VencimientoDatePicker.SelectedDate == null)
            {
                NotificationService.Warning("Complete año, mes y fechas válidas.");
                return;
            }

            if (!IntentarLeerImporte(ImportePenTextBox.Text, "PEN", out decimal? importePen) ||
                !IntentarLeerImporte(ImporteUsdTextBox.Text, "USD", out decimal? importeUsd))
                return;

            _periodo.PeriodoAnio = anio;
            _periodo.PeriodoMes = mes;
            _periodo.FechaCorte = CorteDatePicker.SelectedDate.Value;
            _periodo.FechaVencimiento = VencimientoDatePicker.SelectedDate.Value;
            _periodo.ImportePEN = importePen;
            _periodo.ImporteUSD = importeUsd;
            _periodo.Monto = importePen ?? importeUsd;
            _periodo.Estado = importePen.HasValue || importeUsd.HasValue ? "PENDIENTE" : "ABIERTO";
            _periodo.Observacion = ObservacionTextBox.Text;

            TarjetaCreditoPeriodoResultado resultado = _negocio.GuardarPeriodo(
                _periodo, SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema");
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                DialogResult = true;
            }
            else
                NotificationService.Warning(resultado.Mensaje);
        }

        private static bool IntentarLeerImporte(string texto, string moneda, out decimal? importe)
        {
            importe = null;
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            if (!decimal.TryParse(texto, out decimal valor) || valor < 0)
            {
                NotificationService.Warning($"Importe {moneda} inválido.");
                return false;
            }

            importe = valor;
            return true;
        }
    }
}
