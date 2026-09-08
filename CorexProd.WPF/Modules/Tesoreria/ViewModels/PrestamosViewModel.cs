using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Tesoreria.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Tesoreria.ViewModels
{
    public class PrestamosViewModel : BaseViewModel
    {
        private readonly ObligacionFinancieraNegocio _negocio = new();
        private string _textoBusqueda = string.Empty;
        private string _estadoFiltro = "Todos";
        private string _monedaFiltro = "Todos";
        private EntidadFinancieraFiltroItem? _entidadFiltro;
        private ObligacionFinancieraResumen? _prestamoSeleccionado;
        private bool _cargando;

        public PrestamosViewModel()
        {
            Estados = new ObservableCollection<string> { "Todos", "PENDIENTE", "PARCIAL", "CANCELADA", "ANULADA" };
            Monedas = new ObservableCollection<string> { "Todos", "PEN", "USD" };

            NuevaCommand = new RelayCommand(_ => Nuevo());
            ActualizarCommand = new RelayCommand(_ => Cargar());
            VerCommand = new RelayCommand(p => Ver(p as ObligacionFinancieraResumen));
            EditarCommand = new RelayCommand(p => Editar(p as ObligacionFinancieraResumen));
            AnularCommand = new RelayCommand(p => Anular(p as ObligacionFinancieraResumen));

            CargarFiltros();
            Cargar();
        }

        public ObservableCollection<ObligacionFinancieraResumen> Prestamos { get; } = [];
        public ObservableCollection<string> Estados { get; }
        public ObservableCollection<string> Monedas { get; }
        public ObservableCollection<EntidadFinancieraFiltroItem> Entidades { get; } = [];
        public ICommand NuevaCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand VerCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand AnularCommand { get; }

        public string Resumen => Cargando ? "Cargando prestamos..." : $"{Prestamos.Count} prestamo(s) encontrado(s)";

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value ?? string.Empty; OnPropertyChanged(); Cargar(); }
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set { _estadoFiltro = value ?? "Todos"; OnPropertyChanged(); Cargar(); }
        }

        public string MonedaFiltro
        {
            get => _monedaFiltro;
            set { _monedaFiltro = value ?? "Todos"; OnPropertyChanged(); Cargar(); }
        }

        public EntidadFinancieraFiltroItem? EntidadFiltro
        {
            get => _entidadFiltro;
            set { _entidadFiltro = value; OnPropertyChanged(); Cargar(); }
        }

        public ObligacionFinancieraResumen? PrestamoSeleccionado
        {
            get => _prestamoSeleccionado;
            set { _prestamoSeleccionado = value; OnPropertyChanged(); }
        }

        public bool Cargando
        {
            get => _cargando;
            set { _cargando = value; OnPropertyChanged(); OnPropertyChanged(nameof(Resumen)); }
        }

        private void CargarFiltros()
        {
            try
            {
                Entidades.Clear();
                Entidades.Add(new EntidadFinancieraFiltroItem(null, "Todas"));
                foreach (EntidadFinanciera entidad in _negocio.ListarEntidadesFinancieras().OrderBy(e => e.Nombre))
                    Entidades.Add(new EntidadFinancieraFiltroItem(entidad.IdEntidadFinanciera, entidad.EntidadBusqueda));

                EntidadFiltro = Entidades.FirstOrDefault();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar las entidades financieras: {ex.Message}");
            }
        }

        private void Cargar()
        {
            try
            {
                Cargando = true;
                Prestamos.Clear();

                string? estado = EstadoFiltro == "Todos" ? null : EstadoFiltro;
                int? idEntidad = EntidadFiltro?.IdEntidadFinanciera;

                var lista = _negocio.Listar(TextoBusqueda, estado, idEntidadFinanciera: idEntidad);
                if (MonedaFiltro != "Todos")
                    lista = lista.Where(p => p.Moneda.Equals(MonedaFiltro, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (ObligacionFinancieraResumen prestamo in lista.Where(p => p.CodigoTipoObligacionFinanciera.Equals("PRESTAMO", StringComparison.OrdinalIgnoreCase)))
                    Prestamos.Add(prestamo);
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar los prestamos: {ex.Message}");
            }
            finally
            {
                Cargando = false;
                OnPropertyChanged(nameof(Resumen));
            }
        }

        private void Nuevo()
        {
            PrestamoEditorWindow ventana = new()
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() == true)
                Cargar();
        }

        private void Ver(ObligacionFinancieraResumen? prestamo)
        {
            prestamo ??= PrestamoSeleccionado;
            if (prestamo == null)
                return;

            ObligacionFinancieraDetalle? detalle = _negocio.ObtenerDetalle(prestamo.IdObligacionFinanciera);
            if (detalle?.Cabecera == null)
            {
                NotificationService.Warning("No se encontro el prestamo seleccionado.");
                return;
            }

            PrestamoDetalleWindow ventana = new(detalle.Cabecera)
            {
                Owner = Application.Current.MainWindow
            };
            ventana.ShowDialog();
            Cargar();
        }

        private void Editar(ObligacionFinancieraResumen? prestamo)
        {
            prestamo ??= PrestamoSeleccionado;
            if (prestamo == null)
                return;

            ObligacionFinanciera? detalle = _negocio.Obtener(prestamo.IdObligacionFinanciera);
            if (detalle == null)
            {
                NotificationService.Warning("No se encontro el prestamo seleccionado.");
                return;
            }

            if (detalle.EstaAnulada)
            {
                NotificationService.Warning("No se puede editar un prestamo anulado.");
                return;
            }

            PrestamoEditorWindow ventana = new(detalle)
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() == true)
                Cargar();
        }

        private void Anular(ObligacionFinancieraResumen? prestamo)
        {
            prestamo ??= PrestamoSeleccionado;
            if (prestamo == null)
                return;

            if (prestamo.Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Warning("El prestamo ya se encuentra anulado.");
                return;
            }

            if (!ConfirmDialogService.Confirmar("Desea anular el prestamo seleccionado?", "Confirmar anulacion"))
                return;

            AnularPrestamoWindow ventana = new($"#{prestamo.IdObligacionFinanciera} - {prestamo.EntidadFinanciera}")
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() != true)
                return;

            string usuario = SessionManager.UsuarioActual?.NombreCompleto
                ?? SessionManager.UsuarioActual?.NombreUsuario
                ?? "Sistema";

            ObligacionFinancieraResultado resultado = _negocio.Anular(prestamo.IdObligacionFinanciera, usuario, ventana.MotivoAnulacion);
            if (resultado.Resultado)
            {
                NotificationService.Success(resultado.Mensaje);
                Cargar();
            }
            else
            {
                NotificationService.Warning(resultado.Mensaje);
            }
        }
    }

    public class EntidadFinancieraFiltroItem
    {
        public EntidadFinancieraFiltroItem(int? idEntidadFinanciera, string nombre)
        {
            IdEntidadFinanciera = idEntidadFinanciera;
            Nombre = nombre;
        }

        public int? IdEntidadFinanciera { get; }
        public string Nombre { get; }
    }
}
