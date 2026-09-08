using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public partial class SeguroEditorWindow : Window
    {
        private readonly SeguroNegocio _negocio = new();
        private readonly Seguro? _existente;
        private readonly List<SeguroPeriodo> _periodosOriginales = [];

        public SeguroEditorWindow(Seguro? existente = null)
        {
            _existente = existente;
            InitializeComponent();
            InicioCoberturaPicker.SelectedDate = FinCoberturaPicker.SelectedDate = VencimientoPicker.SelectedDate = DateTime.Today;
            PeriodicidadComboBox.ItemsSource = new[] { "UNICO", "MENSUAL", "BIMESTRAL", "TRIMESTRAL", "SEMESTRAL", "ANUAL" };
            MonedaComboBox.ItemsSource = new[] { "PEN", "USD" };

            try
            {
                var aseguradoras = _negocio.ListarAseguradoras();
                AseguradoraComboBox.ItemsSource = aseguradoras;
                if (existente == null)
                {
                    PeriodicidadComboBox.SelectedIndex = 0;
                    MonedaComboBox.SelectedIndex = 0;
                    return;
                }

                Title = "Editar Seguro";
                TipoTextBox.Text = existente.TipoSeguro;
                PolizaTextBox.Text = existente.NumeroPoliza;
                InicioCoberturaPicker.SelectedDate = existente.FechaInicioCobertura;
                FinCoberturaPicker.SelectedDate = existente.FechaFinCobertura;
                PeriodicidadComboBox.SelectedItem = existente.Periodicidad;
                MonedaComboBox.SelectedItem = existente.Obligacion.Moneda;
                MontoTextBox.Text = existente.Obligacion.MontoOriginal?.ToString("N2");
                TitularTextBox.Text = existente.Obligacion.NombreTitular;
                ConceptoTextBox.Text = existente.Obligacion.Concepto;
                ReferenciaTextBox.Text = existente.Obligacion.CodigoReferencia;
                ObservacionTextBox.Text = existente.Obligacion.Observacion;
                AseguradoraComboBox.SelectedItem = aseguradoras.FirstOrDefault(x => x.IdEntidadFinanciera == existente.Obligacion.IdEntidadFinanciera);

                _periodosOriginales.AddRange(existente.Periodos.Select(CopiarPeriodo));
                SeguroPeriodo? primerPeriodo = _periodosOriginales.OrderBy(p => p.NumeroCuota).FirstOrDefault();
                if (primerPeriodo != null)
                {
                    CuotasRadio.IsChecked = primerPeriodo.EsCuota;
                    UnicoRadio.IsChecked = !primerPeriodo.EsCuota;
                    TotalCuotasTextBox.Text = primerPeriodo.TotalCuotas.ToString();
                    VencimientoPicker.SelectedDate = primerPeriodo.FechaVencimiento;
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (AseguradoraComboBox.SelectedItem is not EntidadFinanciera aseguradora ||
                !decimal.TryParse(MontoTextBox.Text, NumberStyles.Number, new CultureInfo("es-PE"), out decimal monto) ||
                !int.TryParse(TotalCuotasTextBox.Text, out int total))
            {
                NotificationService.Warning("Complete aseguradora, monto y total de cuotas válidos.");
                return;
            }

            bool cuotas = CuotasRadio.IsChecked == true;
            total = cuotas ? total : 1;
            if (total < 1)
            {
                NotificationService.Warning("El total de cuotas debe ser mayor a cero.");
                return;
            }

            DateTime inicio = (InicioCoberturaPicker.SelectedDate ?? DateTime.Today).Date;
            DateTime fin = (FinCoberturaPicker.SelectedDate ?? DateTime.Today).Date;
            DateTime vence = (VencimientoPicker.SelectedDate ?? DateTime.Today).Date;
            if (inicio > fin)
            {
                NotificationService.Warning("La fecha de inicio de cobertura no puede ser posterior a la fecha de fin.");
                return;
            }

            List<SeguroPeriodo> periodos = MantieneCronogramaOriginal(cuotas, total, vence, monto)
                ? _periodosOriginales.Select(CopiarPeriodo).ToList()
                : GenerarPeriodos(cuotas, total, vence, monto);

            var seguro = new Seguro
            {
                IdSeguro = _existente?.IdSeguro ?? 0,
                IdObligacionFinanciera = _existente?.IdObligacionFinanciera ?? 0,
                TipoSeguro = TipoTextBox.Text,
                NumeroPoliza = PolizaTextBox.Text,
                FechaInicioCobertura = inicio,
                FechaFinCobertura = fin,
                Periodicidad = PeriodicidadComboBox.SelectedItem?.ToString() ?? "UNICO",
                Periodos = periodos,
                Obligacion = new ObligacionFinanciera
                {
                    IdObligacionFinanciera = _existente?.IdObligacionFinanciera ?? 0,
                    IdEntidadFinanciera = aseguradora.IdEntidadFinanciera,
                    TipoTitular = "OTRO",
                    NombreTitular = TitularTextBox.Text,
                    Concepto = ConceptoTextBox.Text,
                    CodigoReferencia = ReferenciaTextBox.Text,
                    Moneda = MonedaComboBox.SelectedItem?.ToString() ?? "PEN",
                    MontoOriginal = monto,
                    FechaInicio = inicio,
                    Observacion = ObservacionTextBox.Text,
                    Estado = _existente?.Obligacion.Estado ?? "PENDIENTE"
                }
            };

            SeguroResultado resultado = _existente == null
                ? _negocio.Guardar(seguro, SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema")
                : _negocio.ActualizarSeguro(seguro, SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema");
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                DialogResult = true;
            }
            else
                NotificationService.Warning(resultado.Mensaje);
        }

        private bool MantieneCronogramaOriginal(bool cuotas, int total, DateTime vence, decimal monto)
        {
            SeguroPeriodo? primero = _periodosOriginales.OrderBy(p => p.NumeroCuota).FirstOrDefault();
            return primero != null && primero.EsCuota == cuotas && primero.TotalCuotas == total &&
                   primero.FechaVencimiento.Date == vence &&
                   Math.Round(_periodosOriginales.Sum(p => p.Importe), 2) == Math.Round(monto, 2);
        }

        private static List<SeguroPeriodo> GenerarPeriodos(bool cuotas, int total, DateTime vence, decimal monto)
        {
            var periodos = new List<SeguroPeriodo>();
            decimal importeBase = Math.Round(monto / total, 2);
            decimal acumulado = 0;
            for (int i = 1; i <= total; i++)
            {
                decimal importe = i == total ? monto - acumulado : importeBase;
                acumulado += importe;
                periodos.Add(new SeguroPeriodo
                {
                    NumeroCuota = i,
                    TotalCuotas = total,
                    EsCuota = cuotas,
                    FechaVencimiento = vence.AddMonths(cuotas ? i - 1 : 0),
                    Importe = importe
                });
            }
            return periodos;
        }

        private static SeguroPeriodo CopiarPeriodo(SeguroPeriodo periodo) => new()
        {
            IdCuotaObligacionFinanciera = periodo.IdCuotaObligacionFinanciera,
            IdObligacionFinanciera = periodo.IdObligacionFinanciera,
            NumeroCuota = periodo.NumeroCuota,
            TotalCuotas = periodo.TotalCuotas,
            EsCuota = periodo.EsCuota,
            FechaVencimiento = periodo.FechaVencimiento,
            Importe = periodo.Importe,
            TotalPagado = periodo.TotalPagado,
            SaldoPendiente = periodo.SaldoPendiente,
            Estado = periodo.Estado,
            Observacion = periodo.Observacion,
            FechaRegistro = periodo.FechaRegistro
        };
    }
}
