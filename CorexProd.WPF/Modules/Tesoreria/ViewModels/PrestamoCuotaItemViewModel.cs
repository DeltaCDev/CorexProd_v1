using CorexProd.Entidad.Entidades;
using CorexProd.WPF.ViewModels;
using System;

namespace CorexProd.WPF.Modules.Tesoreria.ViewModels
{
    public class PrestamoCuotaItemViewModel : BaseViewModel
    {
        private int _numeroCuota;
        private int _totalCuotas;
        private DateTime _fechaVencimiento;
        private decimal _importe;
        private string _observacion = string.Empty;
        private bool _puedeEditarNumeroImporte = true;
        private bool _puedeQuitar = true;

        public PrestamoCuotaItemViewModel()
        {
            FechaVencimiento = DateTime.Today;
        }

        public PrestamoCuotaItemViewModel(ObligacionFinancieraCuota cuota, bool puedeEditarNumeroImporte, bool puedeQuitar)
        {
            IdCuotaObligacionFinanciera = cuota.IdCuotaObligacionFinanciera;
            IdObligacionFinanciera = cuota.IdObligacionFinanciera;
            NumeroCuota = cuota.NumeroCuota;
            TotalCuotas = cuota.TotalCuotas;
            FechaVencimiento = cuota.FechaVencimiento;
            Importe = cuota.Importe;
            TotalPagado = cuota.TotalPagado;
            SaldoPendiente = cuota.SaldoPendiente;
            Estado = cuota.Estado;
            Observacion = cuota.Observacion;
            PuedeEditarNumeroImporte = puedeEditarNumeroImporte;
            PuedeQuitar = puedeQuitar;
        }

        public int IdCuotaObligacionFinanciera { get; set; }
        public int IdObligacionFinanciera { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = "PENDIENTE";

        public int NumeroCuota
        {
            get => _numeroCuota;
            set { _numeroCuota = value; OnPropertyChanged(); }
        }

        public int TotalCuotas
        {
            get => _totalCuotas;
            set { _totalCuotas = value; OnPropertyChanged(); }
        }

        public DateTime FechaVencimiento
        {
            get => _fechaVencimiento;
            set { _fechaVencimiento = value.Date; OnPropertyChanged(); }
        }

        public decimal Importe
        {
            get => _importe;
            set { _importe = value; OnPropertyChanged(); }
        }

        public string Observacion
        {
            get => _observacion;
            set { _observacion = value ?? string.Empty; OnPropertyChanged(); }
        }

        public bool PuedeEditarNumeroImporte
        {
            get => _puedeEditarNumeroImporte;
            set { _puedeEditarNumeroImporte = value; OnPropertyChanged(); }
        }

        public bool PuedeQuitar
        {
            get => _puedeQuitar;
            set { _puedeQuitar = value; OnPropertyChanged(); }
        }

        public ObligacionFinancieraCuota ToEntity() => new()
        {
            IdCuotaObligacionFinanciera = IdCuotaObligacionFinanciera,
            IdObligacionFinanciera = IdObligacionFinanciera,
            NumeroCuota = NumeroCuota,
            TotalCuotas = TotalCuotas,
            FechaVencimiento = FechaVencimiento,
            Importe = Importe,
            TotalPagado = TotalPagado,
            SaldoPendiente = SaldoPendiente,
            Estado = Estado,
            Observacion = Observacion
        };
    }
}
