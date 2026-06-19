using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Ventas.Views;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Ventas.ViewModels
{
    public class ProformasViewModel : BaseViewModel
    {
        private readonly ProformaNegocio _proformaNegocio = new();
        private readonly OrdenCompraInternaNegocio _ordenCompraInternaNegocio = new();
        private readonly EmpresaNegocio _empresaNegocio = new();
        private readonly List<Proforma> _todasLasProformas = [];
        private Proforma? _proformaSeleccionada;
        private string _textoBusqueda = string.Empty;
        private string _estadoFiltro = "Todos";
        private DateTime? _fechaDesdeFiltro;
        private DateTime? _fechaHastaFiltro;

        public ObservableCollection<Proforma> Proformas { get; set; } = [];
        public ObservableCollection<string> EstadosFiltro { get; } = ["Todos", "Emitido", "Registrado", "Anulado"];

        public Proforma? ProformaSeleccionada
        {
            get => _proformaSeleccionada;
            set
            {
                _proformaSeleccionada = value;
                OnPropertyChanged();
            }
        }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set
            {
                _estadoFiltro = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public DateTime? FechaDesdeFiltro
        {
            get => _fechaDesdeFiltro;
            set
            {
                _fechaDesdeFiltro = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public DateTime? FechaHastaFiltro
        {
            get => _fechaHastaFiltro;
            set
            {
                _fechaHastaFiltro = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public string ResumenRegistros => $"Mostrando {Proformas.Count} de {_todasLasProformas.Count} registros";

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand VerCommand { get; }
        public ICommand CopiarCommand { get; }
        public ICommand ImprimirCommand { get; }
        public ICommand AnularCommand { get; }
        public ICommand GenerarOciCommand { get; }
        public ICommand RefrescarCommand { get; }
        public ICommand QuitarFiltrosCommand { get; }

        public ProformasViewModel()
        {
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null, false));
            VerCommand = new RelayCommand(parametro => Ver(parametro));
            EditarCommand = new RelayCommand(parametro => Editar(parametro), PuedeModificar);
            CopiarCommand = new RelayCommand(parametro => Copiar(parametro));
            ImprimirCommand = new RelayCommand(parametro => Imprimir(parametro));
            AnularCommand = new RelayCommand(parametro => Anular(parametro), PuedeAnular);
            GenerarOciCommand = new RelayCommand(GenerarOci, PuedeGenerarOci);
            RefrescarCommand = new RelayCommand(_ => CargarProformas());
            QuitarFiltrosCommand = new RelayCommand(_ => QuitarFiltros());

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                CargarProformas();
            }
        }

        private void CargarProformas()
        {
            _todasLasProformas.Clear();
            Proformas.Clear();

            IEnumerable<Proforma> proformas = _proformaNegocio.Listar()
                .OrderByDescending(p => p.FechaEmision)
                .ThenByDescending(p => p.IdProforma);

            foreach (Proforma proforma in proformas)
            {
                _todasLasProformas.Add(proforma);
            }

            AplicarFiltros();
        }

        private void QuitarFiltros()
        {
            _textoBusqueda = string.Empty;
            _estadoFiltro = "Todos";
            _fechaDesdeFiltro = null;
            _fechaHastaFiltro = null;

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(EstadoFiltro));
            OnPropertyChanged(nameof(FechaDesdeFiltro));
            OnPropertyChanged(nameof(FechaHastaFiltro));

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            string busqueda = TextoBusqueda.Trim();
            IEnumerable<Proforma> filtradas = _todasLasProformas;

            if (!string.IsNullOrWhiteSpace(EstadoFiltro) && EstadoFiltro != "Todos")
            {
                filtradas = filtradas.Where(p => string.Equals(p.Estado, EstadoFiltro, StringComparison.OrdinalIgnoreCase));
            }

            if (FechaDesdeFiltro.HasValue)
            {
                DateTime fechaDesde = FechaDesdeFiltro.Value.Date;
                filtradas = filtradas.Where(p => p.FechaEmision.Date >= fechaDesde);
            }

            if (FechaHastaFiltro.HasValue)
            {
                DateTime fechaHasta = FechaHastaFiltro.Value.Date;
                filtradas = filtradas.Where(p => p.FechaEmision.Date <= fechaHasta);
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                filtradas = filtradas.Where(p =>
                    Contiene(p.NombreCliente, busqueda) ||
                    Contiene(p.Estado, busqueda) ||
                    Contiene(p.SerieNumero, busqueda) ||
                    Contiene(p.OrdenCompraCliente, busqueda));
            }

            Proformas.Clear();

            foreach (Proforma proforma in filtradas)
            {
                Proformas.Add(proforma);
            }

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return valor?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool EsAnulada(Proforma? proforma)
        {
            return proforma?.Estado.Equals("Anulado", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool PuedeModificar(object? parametro)
        {
            return parametro is Proforma proforma
                && !EsAnulada(proforma)
                && !proforma.TieneOrdenCompraInterna;
        }

        private static bool PuedeAnular(object? parametro)
        {
            return parametro is Proforma proforma && !EsAnulada(proforma) && !proforma.TieneOrdenCompraInterna;
        }

        private static bool PuedeGenerarOci(object? parametro)
        {
            return parametro is Proforma proforma
                && !EsAnulada(proforma)
                && !proforma.TieneOrdenCompraInterna;
        }

        private void GenerarOci(object? parametro)
        {
            if (parametro is not Proforma proforma)
            {
                NotificationService.Warning("Debe seleccionar una proforma.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                $"¿Desea generar la OCI desde la proforma {proforma.SerieNumero}?",
                "Generar orden de compra interna");

            if (!confirmar) return;

            string usuario = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
            string mensaje = _ordenCompraInternaNegocio.Generar(proforma.IdProforma, usuario);

            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Success(mensaje);
                CargarProformas();
            }
            else
            {
                NotificationService.Warning(mensaje);
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

            if (proforma.TieneOrdenCompraInterna)
            {
                NotificationService.Warning("La proforma ya tiene una OCI emitida y solo esta disponible para consulta");
                return;
            }

            AbrirEditor(proforma, false);
        }

        private void Ver(object? parametro)
        {
            Proforma? proforma = ObtenerProforma(parametro);

            if (proforma == null)
            {
                return;
            }

            ProformaDetalleWindow ventana = new(proforma)
            {
                Owner = Application.Current.MainWindow
            };

            ventana.ShowDialog();
        }

        private void Copiar(object? parametro)
        {
            Proforma? proforma = ObtenerProforma(parametro);

            if (proforma != null)
            {
                AbrirEditor(proforma, true);
            }
        }

        private void Imprimir(object? parametro)
        {
            Proforma? proforma = ObtenerProforma(parametro);

            if (proforma == null)
            {
                return;
            }

            Empresa? empresa = _empresaNegocio.ObtenerPredeterminada();

            if (empresa == null)
            {
                NotificationService.Warning("Debe registrar una empresa predeterminada antes de imprimir");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Guardar proforma",
                FileName = $"Proforma_{proforma.SerieNumero}.pdf",
                Filter = "PDF|*.pdf"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                ProformaPdfExporter.Exportar(dialog.FileName, empresa, proforma);
                NotificationService.Success("Proforma generada correctamente");

                Process.Start(new ProcessStartInfo(dialog.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo generar la proforma: {ex.Message}");
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

            if (EsAnulada(proforma))
            {
                NotificationService.Warning("La proforma ya se encuentra anulada");
                return;
            }

            if (proforma.TieneOrdenCompraInterna)
            {
                NotificationService.Warning("No se puede anular porque ya tiene orden de compra interna");
                return;
            }

            AnularProformaWindow ventana = new(proforma.SerieNumero)
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() != true)
            {
                return;
            }

            string usuarioAnulacion = SessionManager.UsuarioActual?.NombreUsuario ?? string.Empty;
            string mensaje = _proformaNegocio.Anular(proforma.IdProforma, ventana.MotivoAnulacion, usuarioAnulacion);

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
