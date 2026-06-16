using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Ventas.Views;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Ventas.ViewModels
{
    public class ProformasViewModel : BaseViewModel
    {
        private readonly ProformaNegocio _proformaNegocio = new();
        private Proforma? _proformaSeleccionada;

        public ObservableCollection<Proforma> Proformas { get; set; } = [];

        public Proforma? ProformaSeleccionada
        {
            get => _proformaSeleccionada;
            set
            {
                _proformaSeleccionada = value;
                OnPropertyChanged();
            }
        }

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand CopiarCommand { get; }
        public ICommand AnularCommand { get; }
        public ICommand GenerarOciCommand { get; }
        public ICommand RefrescarCommand { get; }

        public ProformasViewModel()
        {
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null, false));
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            CopiarCommand = new RelayCommand(parametro => Copiar(parametro));
            AnularCommand = new RelayCommand(parametro => Anular(parametro));
            GenerarOciCommand = new RelayCommand(_ => NotificationService.Info("Generar orden de compra interna se encuentra en mantenimiento"));
            RefrescarCommand = new RelayCommand(_ => CargarProformas());

            CargarProformas();
        }

        private void CargarProformas()
        {
            Proformas.Clear();

            foreach (Proforma proforma in _proformaNegocio.Listar())
            {
                Proformas.Add(proforma);
            }
        }

        private void Editar(object? parametro)
        {
            Proforma? proforma = ObtenerProforma(parametro);

            if (proforma == null)
            {
                return;
            }

            if (proforma.Estado == "Anulado")
            {
                NotificationService.Warning("No se puede editar una proforma anulada");
                return;
            }

            AbrirEditor(proforma, false);
        }

        private void Copiar(object? parametro)
        {
            Proforma? proforma = ObtenerProforma(parametro);

            if (proforma != null)
            {
                AbrirEditor(proforma, true);
            }
        }

        private void Anular(object? parametro)
        {
            Proforma? proforma = parametro as Proforma;

            if (proforma == null)
            {
                NotificationService.Warning("Debe seleccionar una proforma");
                return;
            }

            if (proforma.TieneOrdenCompraInterna)
            {
                NotificationService.Warning("No se puede anular porque ya tiene orden de compra interna");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Desea anular esta proforma?",
                "Anular proforma");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _proformaNegocio.Anular(proforma.IdProforma);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarProformas();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private Proforma? ObtenerProforma(object? parametro)
        {
            if (parametro is not Proforma fila)
            {
                NotificationService.Warning("Debe seleccionar una proforma");
                return null;
            }

            Proforma? proforma = _proformaNegocio.Obtener(fila.IdProforma);

            if (proforma == null)
            {
                NotificationService.Warning("No se encontro la proforma");
            }

            return proforma;
        }

        private void AbrirEditor(Proforma? proforma, bool copiar)
        {
            ProformaEditorViewModel viewModel = new(proforma, copiar);
            ProformaEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                CargarProformas();
            }
        }
    }
}
