using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Produccion.Views;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Produccion.ViewModels
{
    public class AreasProduccionViewModel : BaseViewModel
    {
        private readonly AreaProduccionNegocio _negocio = new();
        private string _textoBusqueda = string.Empty;
        private string _filtro = "Todos";

        public ObservableCollection<AreaProduccion> Areas { get; } = [];
        public ICollectionView AreasView { get; }
        public IReadOnlyList<string> Filtros { get; } =
            ["Todos", "Activos", "Inactivos", "Inicio", "Término", "Maneja merma", "UNICO", "PARCIAL"];

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value; OnPropertyChanged(); AreasView.Refresh(); OnPropertyChanged(nameof(ResumenRegistros)); }
        }

        public string Filtro
        {
            get => _filtro;
            set { _filtro = value; OnPropertyChanged(); AreasView.Refresh(); OnPropertyChanged(nameof(ResumenRegistros)); }
        }

        public string ResumenRegistros => $"Mostrando {AreasView.Cast<object>().Count()} de {Areas.Count} áreas";

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand CambiarEstadoCommand { get; }
        public ICommand RefrescarCommand { get; }

        public AreasProduccionViewModel()
        {
            AreasView = CollectionViewSource.GetDefaultView(Areas);
            AreasView.Filter = Filtrar;
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null, false));
            EditarCommand = new RelayCommand(area => AbrirEditor(area as AreaProduccion, false));
            VerDetalleCommand = new RelayCommand(area => AbrirEditor(area as AreaProduccion, true));
            CambiarEstadoCommand = new RelayCommand(area => CambiarEstado(area as AreaProduccion));
            RefrescarCommand = new RelayCommand(_ => Cargar());
            Cargar();
        }

        private void Cargar()
        {
            try
            {
                Areas.Clear();
                foreach (AreaProduccion area in _negocio.Listar())
                    Areas.Add(area);
                AreasView.Refresh();
                OnPropertyChanged(nameof(ResumenRegistros));
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudieron cargar las áreas de producción. {ex.Message}");
            }
        }

        private bool Filtrar(object item)
        {
            if (item is not AreaProduccion area)
                return false;

            string buscar = TextoBusqueda?.Trim() ?? string.Empty;
            bool coincideTexto = buscar.Length == 0 ||
                area.CodigoArea.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                area.NombreArea.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                area.Descripcion.Contains(buscar, StringComparison.OrdinalIgnoreCase);

            bool coincideFiltro = Filtro switch
            {
                "Activos" => area.Activo,
                "Inactivos" => !area.Activo,
                "Inicio" => area.EsInicio,
                "Término" => area.EsTermino,
                "Maneja merma" => area.ManejaMerma,
                "UNICO" => area.ModoEnvio == "UNICO",
                "PARCIAL" => area.ModoEnvio == "PARCIAL",
                _ => true
            };
            return coincideTexto && coincideFiltro;
        }

        private void AbrirEditor(AreaProduccion? area, bool soloLectura)
        {
            if (area == null && soloLectura)
                return;

            AreaProduccionEditorViewModel viewModel = new(area, soloLectura);
            AreaProduccionEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };
            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();
            if (viewModel.Guardado)
                Cargar();
        }

        private void CambiarEstado(AreaProduccion? area)
        {
            if (area == null)
            {
                NotificationService.Warning("Seleccione un área de producción.");
                return;
            }

            bool nuevoEstado = !area.Activo;
            string accion = nuevoEstado ? "activar" : "desactivar";
            if (!ConfirmDialogService.Confirmar($"¿Está seguro de {accion} el área {area.NombreArea}?", $"Confirmar {accion}"))
                return;

            try
            {
                int idUsuario = SessionManager.UsuarioActual?.IdUsuario ?? 0;
                string mensaje = _negocio.CambiarEstado(area, nuevoEstado, idUsuario);
                if (mensaje.StartsWith("OK|", StringComparison.Ordinal))
                {
                    NotificationService.Success(mensaje[3..]);
                    Cargar();
                }
                else
                {
                    NotificationService.Warning(mensaje);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }
    }
}
