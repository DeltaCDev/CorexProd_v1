using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        public ObservableCollection<TipoDocumentoNumeracion> Tipos { get; } = [];
        public ObservableCollection<SerieCorrelativo> Series { get; } = [];
        public ObservableCollection<SerieCorrelativoHistorial> Historial { get; } = [];
        public TipoDocumentoNumeracion? TipoFiltro { get => _tipoFiltro; set { _tipoFiltro=value; OnPropertyChanged(); CargarSeries(); } }
        public TipoDocumentoNumeracion? TipoEdicion { get => _tipoEdicion; set { _tipoEdicion=value; OnPropertyChanged(); } }
        public SerieCorrelativo? SerieSeleccionada
        {
            get => _seleccionada;
            set { _seleccionada=value; OnPropertyChanged(); if (value != null) Editar(value); }
        }
        public int IdSerieCorrelativo { get => _id; set { _id=value; OnPropertyChanged(); } }
        public string Serie { get => _serie; set { _serie=value; OnPropertyChanged(); OnPropertyChanged(nameof(VistaPrevia)); } }
        public long UltimoCorrelativo { get => _ultimo; set { _ultimo=value; OnPropertyChanged(); OnPropertyChanged(nameof(VistaPrevia)); } }
        public byte CantidadDigitos { get => _digitos; set { _digitos=value; OnPropertyChanged(); OnPropertyChanged(nameof(VistaPrevia)); } }
        public bool Activa { get => _activa; set { _activa=value; OnPropertyChanged(); } }
        public bool Predeterminada { get => _predeterminada; set { _predeterminada=value; if (value) Activa=true; OnPropertyChanged(); } }
        public string VistaPrevia => $"{Serie.Trim().ToUpperInvariant()}-{UltimoCorrelativo.ToString().PadLeft(CantidadDigitos, '0')}";

        public ICommand NuevoCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand QuitarFiltroCommand { get; }

        public SeriesCorrelativosViewModel()
        {
            NuevoCommand = new RelayCommand(_ => Nuevo());
            GuardarCommand = new RelayCommand(_ => Guardar());
            ActualizarCommand = new RelayCommand(_ => Cargar());
            QuitarFiltroCommand = new RelayCommand(_ => TipoFiltro=null);
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
        }

        private void Editar(SerieCorrelativo item)
        {
            IdSerieCorrelativo=item.IdSerieCorrelativo; TipoEdicion=Tipos.FirstOrDefault(t => t.CodigoTipoDocumento==item.CodigoTipoDocumento);
            Serie=item.Serie; UltimoCorrelativo=item.UltimoCorrelativo; CantidadDigitos=item.CantidadDigitos;
            Activa=item.Activa; Predeterminada=item.Predeterminada;
            Historial.Clear(); foreach (SerieCorrelativoHistorial h in _negocio.ListarHistorial(item.IdSerieCorrelativo)) Historial.Add(h);
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
            NotificationService.Success(mensaje); CargarSeries(); Nuevo();
        }
    }
}
