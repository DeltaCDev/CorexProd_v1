using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Data;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Reportes.ViewModels
{
    public class ReportesViewModel : BaseViewModel
    {
        private readonly ReporteComercialNegocio _negocio = new();
        private DateTime? _fechaDesde = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime? _fechaHasta = DateTime.Today;
        private DataView? _productos;
        private DataView? _clientes;
        private DataView? _ocis;
        private DataView? _ociDetalle;
        private DataView? _usuarios;

        public DateTime? FechaDesde { get => _fechaDesde; set { _fechaDesde = value; OnPropertyChanged(); } }
        public DateTime? FechaHasta { get => _fechaHasta; set { _fechaHasta = value; OnPropertyChanged(); } }
        public DataView? Productos { get => _productos; private set { _productos = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalProductos)); } }
        public DataView? Clientes { get => _clientes; private set { _clientes = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalClientes)); } }
        public DataView? Ocis { get => _ocis; private set { _ocis = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalOcis)); } }
        public DataView? OciDetalle { get => _ociDetalle; private set { _ociDetalle = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalOciDetalle)); } }
        public DataView? Usuarios { get => _usuarios; private set { _usuarios = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalUsuarios)); } }

        public int TotalProductos => Productos?.Count ?? 0;
        public int TotalClientes => Clientes?.Count ?? 0;
        public int TotalOcis => Ocis?.Count ?? 0;
        public int TotalOciDetalle => OciDetalle?.Count ?? 0;
        public int TotalUsuarios => Usuarios?.Count ?? 0;

        public ICommand ConsultarCommand { get; }
        public ICommand VerDetalleOciCommand { get; }

        public ReportesViewModel()
        {
            ConsultarCommand = new RelayCommand(_ => Consultar());
            VerDetalleOciCommand = new RelayCommand(VerDetalleOci);
            Consultar();
        }

        private void Consultar()
        {
            try
            {
                Productos = _negocio.ProductosMasDespachados(FechaDesde, FechaHasta).DefaultView;
                Clientes = _negocio.Clientes(FechaDesde, FechaHasta).DefaultView;
                Ocis = _negocio.OciDespachadas(FechaDesde, FechaHasta).DefaultView;
                OciDetalle = null;
                Usuarios = _negocio.UsuariosConMasProformas(FechaDesde, FechaHasta).DefaultView;
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar los reportes: {ex.Message}");
            }
        }

        private void VerDetalleOci(object? parametro)
        {
            if (parametro is not DataRowView fila)
                return;

            if (!int.TryParse(fila["IdOrdenCompraInterna"]?.ToString(), out int idOrdenCompraInterna))
                return;

            try
            {
                OciDetalle = _negocio.OciDespachadaDetalle(idOrdenCompraInterna).DefaultView;
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo cargar el detalle de la OCI: {ex.Message}");
            }
        }
    }
}
