using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Views;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private UserControl _vistaActual;
        private string _titulo;
        private string _nombreUsuario;
        private bool _verVentas;
        private bool _verAlmacen;
        private bool _verProductos;
        private bool _verProduccion;
        private bool _verReportes;
        private bool _verSeguridad;

        public UserControl VistaActual
        {
            get { return _vistaActual; }
            set
            {
                _vistaActual = value;
                OnPropertyChanged();
            }
        }

        public string Titulo
        {
            get { return _titulo; }
            set
            {
                _titulo = value;
                OnPropertyChanged();
            }
        }

        public string NombreUsuario
        {
            get { return _nombreUsuario; }
            set
            {
                _nombreUsuario = value;
                OnPropertyChanged();
            }
        }

        public bool VerVentas
        {
            get => _verVentas;
            set
            {
                _verVentas = value;
                OnPropertyChanged();
            }
        }

        public bool VerAlmacen
        {
            get => _verAlmacen;
            set
            {
                _verAlmacen = value;
                OnPropertyChanged();
            }
        }

        public bool VerProductos
        {
            get => _verProductos;
            set
            {
                _verProductos = value;
                OnPropertyChanged();
            }
        }

        public bool VerProduccion
        {
            get => _verProduccion;
            set
            {
                _verProduccion = value;
                OnPropertyChanged();
            }
        }

        public bool VerReportes
        {
            get => _verReportes;
            set
            {
                _verReportes = value;
                OnPropertyChanged();
            }
        }

        public bool VerSeguridad
        {
            get => _verSeguridad;
            set
            {
                _verSeguridad = value;
                OnPropertyChanged();
            }
        }


        public ICommand IrInicioCommand { get; }
        public ICommand IrVentasCommand { get; }
        public ICommand IrAlmacenCommand { get; }
        public ICommand IrProductosCommand { get; }
        public ICommand IrProduccionCommand { get; }
        public ICommand IrReportesCommand { get; }
        public ICommand IrSeguridadCommand { get; }

        public MainViewModel()
        {
            NombreUsuario = SessionManager.UsuarioActual?.NombreCompleto ?? "Usuario";

            var menus = SessionManager.MenusPermitidos ?? new List<string>();

            VerVentas = menus.Contains("Ventas");
            VerAlmacen = menus.Contains("Almacén");
            VerProductos = menus.Contains("Productos");
            VerProduccion = menus.Contains("Producción");
            VerReportes = menus.Contains("Reportes");
            VerSeguridad = menus.Contains("Seguridad");

            IrInicioCommand = new RelayCommand(_ => IrInicio());
            IrVentasCommand = new RelayCommand(_ => IrVentas());
            IrAlmacenCommand = new RelayCommand(_ => IrAlmacen());
            IrProductosCommand = new RelayCommand(_ => IrProductos());
            IrProduccionCommand = new RelayCommand(_ => IrProduccion());
            IrReportesCommand = new RelayCommand(_ => IrReportes());
            IrSeguridadCommand = new RelayCommand(_ => IrSeguridad());

            IrInicio();
        }

        private void IrInicio()
        {
            Titulo = "Panel principal";
            VistaActual = new HomeView();
        }

        private void IrVentas()
        {
            Titulo = "Ventas";
            VistaActual = new VentasView();
        }

        private void IrAlmacen()
        {
            Titulo = "Almacén";
            VistaActual = new AlmacenView();
        }

        private void IrProductos()
        {
            Titulo = "Productos";
            VistaActual = new ProductosView();
        }

        private void IrProduccion()
        {
            Titulo = "Producción";
            VistaActual = new ProduccionView();
        }

        private void IrReportes()
        {
            Titulo = "Reportes";
            VistaActual = new ReportesView();
        }

        private void IrSeguridad()
        {
            Titulo = "Seguridad";
            VistaActual = new SeguridadView();
        }
    }
}