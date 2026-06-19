using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Seguridad.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class SeriesCorrelativosViewModel : BaseViewModel
    {
        private readonly SerieCorrelativoNegocio _negocio = new();
        private SerieCorrelativo? _seleccionada;
        private TipoDocumentoNumeracion? _tipoFiltro;
        private TipoDocumentoNumeracion? _tipoEdicion;
        private int _id;
        private string _serie = string.Empty;
        private long _ultimo;
        private byte _digitos = 6;
        private bool _activa = true;
        private bool _predeterminada;
        private bool _soloLectura;

        public ObservableCollection<TipoDocumentoNumeracion> Tipos { get; } = [];
        public ObservableCollection<SerieCorrelativo> Series { get; } = [];
        public ObservableCollection<SerieCorrelativoHistorial> Historial { get; } = [];
        public TipoDocumentoNumeracion? TipoFiltro { get => _tipoFiltro; set { _tipoFiltro=value; OnPropertyChanged(); CargarSeries(); } }
        public TipoDocumentoNumeracion? TipoEdicion { get => _tipoEdicion; set { _tipoEdicion=value; OnPropertyChanged(); } }
        public SerieCorrelativo? SerieSeleccionada
        {
            get => _seleccionada;
            set { _seleccionada=value; OnPropertyChanged(); }
        }
        public int IdSerieCorrelativo { get => _id; set { _id=value; OnPropertyChanged(); } }
        public string Serie { get => _serie; set { _serie=value; OnPropertyChanged(); OnPropertyChanged(nameof(VistaPrevia)); } }
        public long UltimoCorrelativo { get => _ultimo; set { _ultimo=value; OnPropertyChanged(); OnPropertyChanged(nameof(VistaPrevia)); } }
        public byte CantidadDigitos { get => _digitos; set { _digitos=value; OnPropertyChanged(); OnPropertyChanged(nameof(VistaPrevia)); } }
        public bool Activa { get => _activa; set { _activa=value; OnPropertyChanged(); } }
        public bool Predeterminada { get => _predeterminada; set { _predeterminada=value; if (value) Activa=true; OnPropertyChanged(); } }
        public bool SoloLectura { get => _soloLectura; private set { _soloLectura=value; OnPropertyChanged(); OnPropertyChanged(nameof(PuedeEditar)); OnPropertyChanged(nameof(TituloModal)); OnPropertyChanged(nameof(SubtituloModal)); } }
        public bool PuedeEditar => !SoloLectura;
        public string TituloModal => SoloLectura ? "Detalle de serie y correlativo" : IdSerieCorrelativo > 0 ? "Editar serie y correlativo" : "Nueva serie y correlativo";
        public string SubtituloModal => SoloLectura ? "Información registrada e historial de cambios." : "Configure la numeración automática del documento.";
        public string VistaPrevia => $"{Serie.Trim().ToUpperInvariant()}-{UltimoCorrelativo.ToString().PadLeft(CantidadDigitos, '0')}";

        public ICommand NuevoCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand QuitarFiltroCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand VerCommand { get; }
        public ICommand CerrarCommand { get; }

        public Action? CerrarVentana { get; set; }

        public SeriesCorrelativosViewModel()
        {
            NuevoCommand = new RelayCommand(_ => AbrirModal(null, false));
            GuardarCommand = new RelayCommand(_ => Guardar());
            ActualizarCommand = new RelayCommand(_ => Cargar());
            QuitarFiltroCommand = new RelayCommand(_ => TipoFiltro=null);
            EditarCommand = new RelayCommand(item => AbrirModal(item as SerieCorrelativo, false));
            VerCommand = new RelayCommand(item => AbrirModal(item as SerieCorrelativo, true));
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            Cargar();
        }

        private void Cargar()
        {
            string? filtro = TipoFiltro?.CodigoTipoDocumento;
            Tipos.Clear(); foreach (TipoDocumentoNumeracion tipo in _negocio.ListarTipos()) Tipos.Add(tipo);
            _tipoFiltro = Tipos.FirstOrDefault(t => t.CodigoTipoDocumento == filtro);
            OnPropertyChanged(nameof(TipoFiltro));
            CargarSeries();
        }

        private void CargarSeries()
        {
            Series.Clear();
            foreach (SerieCorrelativo item in _negocio.Listar(TipoFiltro?.CodigoTipoDocumento)) Series.Add(item);
        }

        private void Nuevo()
        {
            SerieSeleccionada=null; IdSerieCorrelativo=0; Serie=string.Empty; UltimoCorrelativo=0;
            CantidadDigitos=6; Activa=true; Predeterminada=false;
            TipoEdicion=TipoFiltro ?? Tipos.FirstOrDefault(); Historial.Clear();
            OnPropertyChanged(nameof(TituloModal));
        }

        private void Editar(SerieCorrelativo item)
        {
            IdSerieCorrelativo=item.IdSerieCorrelativo; TipoEdicion=Tipos.FirstOrDefault(t => t.CodigoTipoDocumento==item.CodigoTipoDocumento);
            Serie=item.Serie; UltimoCorrelativo=item.UltimoCorrelativo; CantidadDigitos=item.CantidadDigitos;
            Activa=item.Activa; Predeterminada=item.Predeterminada;
            Historial.Clear(); foreach (SerieCorrelativoHistorial h in _negocio.ListarHistorial(item.IdSerieCorrelativo)) Historial.Add(h);
            OnPropertyChanged(nameof(TituloModal));
        }

        private void AbrirModal(SerieCorrelativo? item, bool soloLectura)
        {
            SoloLectura = soloLectura;
            if (item == null) Nuevo(); else Editar(item);

            SerieCorrelativoEditorWindow ventana = new()
            {
                DataContext = this,
                Owner = Application.Current.MainWindow
            };
            CerrarVentana = ventana.Close;
            ventana.ShowDialog();
            CerrarVentana = null;
        }

        private void Guardar()
        {
            SerieCorrelativo item = new()
            {
                IdSerieCorrelativo=IdSerieCorrelativo, CodigoTipoDocumento=TipoEdicion?.CodigoTipoDocumento ?? string.Empty,
                Serie=Serie, UltimoCorrelativo=UltimoCorrelativo, CantidadDigitos=CantidadDigitos,
                Activa=Activa, Predeterminada=Predeterminada
            };
            string usuario=SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
            string mensaje=_negocio.Guardar(item,usuario);
            if (!mensaje.Contains("correctamente",StringComparison.OrdinalIgnoreCase)) { NotificationService.Warning(mensaje); return; }
            NotificationService.Success(mensaje); CargarSeries(); CerrarVentana?.Invoke();
        }
    }
}
