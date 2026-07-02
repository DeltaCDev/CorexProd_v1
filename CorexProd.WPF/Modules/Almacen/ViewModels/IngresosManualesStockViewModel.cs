using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Almacen.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Almacen.ViewModels
{
    public class IngresosManualesStockViewModel : BaseViewModel
    {
        private readonly IngresoManualStockNegocio _negocio = new();
        private DateTime? _fechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime? _fechaHasta = DateTime.Today;
        private ProveedorStock? _proveedorFiltro;
        private AlmacenStock? _almacenFiltro;
        private string _estadoFiltro = "Todos";
        private string _numeroDocumentoFiltro = string.Empty;
        private IngresoManualStock? _ingresoSeleccionado;

        public IngresosManualesStockViewModel()
        {
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            EditarCommand = new RelayCommand(parametro => Editar(parametro), PuedeEditar);
            VerCommand = new RelayCommand(parametro => Ver(parametro));
            AbastecerCommand = new RelayCommand(parametro => Abastecer(parametro), PuedeAbastecer);
            AnularCommand = new RelayCommand(parametro => Anular(parametro), PuedeAnular);
            BuscarCommand = new RelayCommand(_ => CargarIngresos());
            QuitarFiltrosCommand = new RelayCommand(_ => QuitarFiltros());
            RefrescarCommand = new RelayCommand(_ => CargarIngresos());

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                CargarCombos();
                CargarIngresos();
            }
        }

        public ObservableCollection<IngresoManualStock> Ingresos { get; } = [];
        public ObservableCollection<ProveedorStock> ProveedoresFiltro { get; } = [];
        public ObservableCollection<AlmacenStock> AlmacenesFiltro { get; } = [];
        public ObservableCollection<string> EstadosFiltro { get; } = ["Todos", "Pendiente", "Abastecido", "Anulado"];

        public DateTime? FechaDesde
        {
            get => _fechaDesde;
            set { _fechaDesde = value; OnPropertyChanged(); }
        }

        public DateTime? FechaHasta
        {
            get => _fechaHasta;
            set { _fechaHasta = value; OnPropertyChanged(); }
        }

        public ProveedorStock? ProveedorFiltro
        {
            get => _proveedorFiltro;
            set { _proveedorFiltro = value; OnPropertyChanged(); }
        }

        public AlmacenStock? AlmacenFiltro
        {
            get => _almacenFiltro;
            set { _almacenFiltro = value; OnPropertyChanged(); }
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set { _estadoFiltro = value; OnPropertyChanged(); }
        }

        public string NumeroDocumentoFiltro
        {
            get => _numeroDocumentoFiltro;
            set { _numeroDocumentoFiltro = value; OnPropertyChanged(); }
        }

        public IngresoManualStock? IngresoSeleccionado
        {
            get => _ingresoSeleccionado;
            set { _ingresoSeleccionado = value; OnPropertyChanged(); }
        }

        public string ResumenRegistros => $"Mostrando {Ingresos.Count} ingresos manuales";

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand VerCommand { get; }
        public ICommand AbastecerCommand { get; }
        public ICommand AnularCommand { get; }
        public ICommand BuscarCommand { get; }
        public ICommand QuitarFiltrosCommand { get; }
        public ICommand RefrescarCommand { get; }

        private void CargarCombos()
        {
            ProveedoresFiltro.Clear();
            ProveedoresFiltro.Add(new ProveedorStock { IdProveedor = 0, NombreRazonSocial = "Todos" });
            foreach (ProveedorStock proveedor in _negocio.ListarProveedores())
            {
                ProveedoresFiltro.Add(proveedor);
            }

            AlmacenesFiltro.Clear();
            AlmacenesFiltro.Add(new AlmacenStock { IdAlmacen = 0, NombreAlmacen = "Todos" });
            foreach (AlmacenStock almacen in _negocio.ListarAlmacenes())
            {
                AlmacenesFiltro.Add(almacen);
            }

            ProveedorFiltro = ProveedoresFiltro.FirstOrDefault();
            AlmacenFiltro = AlmacenesFiltro.FirstOrDefault();
        }

        private void CargarIngresos()
        {
            try
            {
                Ingresos.Clear();

                int? idProveedor = ProveedorFiltro?.IdProveedor > 0 ? ProveedorFiltro.IdProveedor : null;
                int? idAlmacen = AlmacenFiltro?.IdAlmacen > 0 ? AlmacenFiltro.IdAlmacen : null;

                foreach (IngresoManualStock ingreso in _negocio.Listar(FechaDesde, FechaHasta, idProveedor, idAlmacen, EstadoFiltro, NumeroDocumentoFiltro))
                {
                    Ingresos.Add(ingreso);
                }

                OnPropertyChanged(nameof(ResumenRegistros));
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar los ingresos manuales: {ex.Message}");
            }
        }

        private void QuitarFiltros()
        {
            FechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FechaHasta = DateTime.Today;
            ProveedorFiltro = ProveedoresFiltro.FirstOrDefault();
            AlmacenFiltro = AlmacenesFiltro.FirstOrDefault();
            EstadoFiltro = "Todos";
            NumeroDocumentoFiltro = string.Empty;
            CargarIngresos();
        }

        private static bool PuedeEditar(object? parametro)
        {
            return parametro is IngresoManualStock ingreso && ingreso.EsPendiente;
        }

        private static bool PuedeAbastecer(object? parametro)
        {
            return parametro is IngresoManualStock ingreso && ingreso.EsPendiente;
        }

        private static bool PuedeAnular(object? parametro)
        {
            return parametro is IngresoManualStock ingreso && !ingreso.EsAnulado;
        }

        private IngresoManualStock? ObtenerIngreso(object? parametro)
        {
            if (parametro is not IngresoManualStock fila)
            {
                NotificationService.Warning("Debe seleccionar un ingreso manual");
                return null;
            }

            IngresoManualStock? ingreso = _negocio.Obtener(fila.IdIngresoManualStock);

            if (ingreso == null)
            {
                NotificationService.Warning("No se encontro el ingreso manual");
            }

            return ingreso;
        }

        private void AbrirEditor(IngresoManualStock? ingreso)
        {
            try
            {
                IngresoManualStockEditorViewModel viewModel = new(ingreso);
                IngresoManualStockEditorWindow ventana = new()
                {
                    DataContext = viewModel,
                    Owner = Application.Current.MainWindow
                };

                viewModel.CerrarVentana = ventana.Close;
                ventana.ShowDialog();

                if (viewModel.Guardado)
                {
                    CargarCombos();
                    CargarIngresos();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo abrir el ingreso manual de stock: {ex.Message}");
            }
        }

        private void Editar(object? parametro)
        {
            IngresoManualStock? ingreso = ObtenerIngreso(parametro);

            if (ingreso == null)
            {
                return;
            }

            if (!ingreso.EsPendiente)
            {
                NotificationService.Warning("Solo se pueden editar ingresos pendientes");
                return;
            }

            AbrirEditor(ingreso);
        }

        private void Ver(object? parametro)
        {
            IngresoManualStock? ingreso = ObtenerIngreso(parametro);

            if (ingreso == null)
            {
                return;
            }

            IngresoManualStockDetalleWindow ventana = new(ingreso)
            {
                Owner = Application.Current.MainWindow
            };
            ventana.ShowDialog();
        }

        private void Abastecer(object? parametro)
        {
            IngresoManualStock? ingreso = ObtenerIngreso(parametro);

            if (ingreso == null)
            {
                return;
            }

            if (!ingreso.EsPendiente)
            {
                NotificationService.Warning("Solo se pueden abastecer ingresos pendientes");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                $"¿Desea abastecer el documento {ingreso.NumeroDocumento}? Esta accion incrementara el stock.",
                "Abastecer ingreso manual");

            if (!confirmar)
            {
                return;
            }

            string usuario = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
            string mensaje = _negocio.Abastecer(ingreso.IdIngresoManualStock, usuario);

            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Success(mensaje);
                CargarIngresos();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Anular(object? parametro)
        {
            IngresoManualStock? ingreso = ObtenerIngreso(parametro);

            if (ingreso == null)
            {
                return;
            }

            if (ingreso.EsAnulado)
            {
                NotificationService.Warning("El ingreso manual ya se encuentra anulado");
                return;
            }

            AnularIngresoManualStockWindow ventana = new(ingreso.NumeroDocumento)
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() != true)
            {
                return;
            }

            string usuario = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
            string mensaje = _negocio.Anular(ingreso.IdIngresoManualStock, usuario, ventana.MotivoAnulacion);

            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Success(mensaje);
                CargarIngresos();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }
    }
}
