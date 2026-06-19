using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Ventas.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Ventas.ViewModels
{
    public class GuiasInternasViewModel : BaseViewModel
    {
        private readonly GuiaInternaNegocio _negocio = new();
        private readonly IngresoManualStockNegocio _almacenNegocio = new();
        private DateTime? _desde=DateTime.Today.AddDays(-30),_hasta=DateTime.Today;
        private AlmacenStock? _almacen;
        private string _estado="Todos",_origen="Todos",_texto=string.Empty;
        public ObservableCollection<GuiaInterna> Guias { get; }=[];
        public ObservableCollection<AlmacenStock> Almacenes { get; }=[];
        public ObservableCollection<string> Estados { get; }=["Todos","Emitida","Anulada"];
        public ObservableCollection<string> Origenes { get; }=["Todos","OCI","Manual"];
        public DateTime? FechaDesde { get=>_desde; set { _desde=value; OnPropertyChanged(); } }
        public DateTime? FechaHasta { get=>_hasta; set { _hasta=value; OnPropertyChanged(); } }
        public AlmacenStock? AlmacenFiltro { get=>_almacen; set { _almacen=value; OnPropertyChanged(); } }
        public string EstadoFiltro { get=>_estado; set { _estado=value; OnPropertyChanged(); } }
        public string OrigenFiltro { get=>_origen; set { _origen=value; OnPropertyChanged(); } }
        public string TextoBusqueda { get=>_texto; set { _texto=value; OnPropertyChanged(); } }
        public string Resumen => $"Mostrando {Guias.Count} guías internas";
        public ICommand NuevaManualCommand { get; }
        public ICommand VerCommand { get; }
        public ICommand AnularCommand { get; }
        public ICommand BuscarCommand { get; }
        public ICommand LimpiarCommand { get; }

        public GuiasInternasViewModel()
        {
            NuevaManualCommand=new RelayCommand(_=>NuevaManual()); VerCommand=new RelayCommand(Ver);
            AnularCommand=new RelayCommand(Anular,p=>p is GuiaInterna g && g.PuedeAnular);
            BuscarCommand=new RelayCommand(_=>Cargar()); LimpiarCommand=new RelayCommand(_=>Limpiar());
            if(!DesignerProperties.GetIsInDesignMode(new DependencyObject())) { CargarAlmacenes(); Cargar(); }
        }
        private void CargarAlmacenes()
        {
            Almacenes.Clear(); Almacenes.Add(new AlmacenStock{IdAlmacen=0,NombreAlmacen="Todos"});
            foreach(AlmacenStock a in _almacenNegocio.ListarAlmacenes()) Almacenes.Add(a);
            AlmacenFiltro=Almacenes.FirstOrDefault();
        }
        private void Cargar()
        {
            try
            {
                Guias.Clear(); int? id=AlmacenFiltro?.IdAlmacen>0?AlmacenFiltro.IdAlmacen:null;
                foreach(GuiaInterna g in _negocio.Listar(FechaDesde,FechaHasta,id,EstadoFiltro,OrigenFiltro,TextoBusqueda)) Guias.Add(g);
                OnPropertyChanged(nameof(Resumen));
            }
            catch(Exception ex){ NotificationService.Error($"No se pudieron cargar las guías internas: {ex.Message}"); }
        }
        private void Limpiar(){ FechaDesde=null;FechaHasta=null;AlmacenFiltro=Almacenes.FirstOrDefault();EstadoFiltro="Todos";OrigenFiltro="Todos";TextoBusqueda=string.Empty;Cargar(); }
        private void NuevaManual()
        {
            GuiaInterna? guia=_negocio.PrepararManual();
            if(guia==null){NotificationService.Warning("No existe un almacén activo para emitir la guía.");return;}
            if(new GuiaInternaManualWindow(guia){Owner=Application.Current.MainWindow}.ShowDialog()==true) Cargar();
        }
        private void Ver(object? p)
        {
            if(p is not GuiaInterna fila)return; GuiaInterna? guia=_negocio.Obtener(fila.IdGuiaInterna);
            if(guia==null){NotificationService.Warning("No se encontró la guía interna.");return;}
            new GuiaInternaDetalleWindow(guia){Owner=Application.Current.MainWindow}.ShowDialog();
        }
        private void Anular(object? p)
        {
            if(p is not GuiaInterna guia)return;
            AnularGuiaInternaWindow w=new(guia.NumeroGuia){Owner=Application.Current.MainWindow}; if(w.ShowDialog()!=true)return;
            string usuario=SessionManager.UsuarioActual?.NombreUsuario??"Sistema";
            string mensaje=_negocio.Anular(guia.IdGuiaInterna,usuario,w.MotivoAnulacion);
            if(mensaje.Contains("correctamente",StringComparison.OrdinalIgnoreCase)){NotificationService.Success(mensaje);Cargar();}else NotificationService.Warning(mensaje);
        }
    }
}
