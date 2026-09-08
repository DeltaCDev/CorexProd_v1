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
    public class SegurosViewModel : BaseViewModel
    {
        private readonly SeguroNegocio _negocio = new();
        private string _texto = string.Empty, _estado = "Todos", _moneda = "Todos";
        private EntidadFinancieraFiltroItem? _aseguradora;

        public SegurosViewModel()
        {
            Estados = new(["Todos", "PENDIENTE", "PARCIAL", "CANCELADA", "ANULADA"]);
            Monedas = new(["Todos", "PEN", "USD"]);
            NuevoCommand = new RelayCommand(_ => Nuevo()); ActualizarCommand = new RelayCommand(_ => Cargar());
            VerCommand = new RelayCommand(p => Ver(p as SeguroResumen)); EditarCommand = new RelayCommand(p => Editar(p as SeguroResumen));
            AnularCommand = new RelayCommand(p => Anular(p as SeguroResumen)); CargarFiltros(); Cargar();
        }
        public ObservableCollection<SeguroResumen> Seguros { get; } = [];
        public ObservableCollection<string> Estados { get; } public ObservableCollection<string> Monedas { get; }
        public ObservableCollection<EntidadFinancieraFiltroItem> Aseguradoras { get; } = [];
        public ICommand NuevoCommand { get; } public ICommand ActualizarCommand { get; } public ICommand VerCommand { get; } public ICommand EditarCommand { get; } public ICommand AnularCommand { get; }
        public string Resumen => $"{Seguros.Count} seguro(s) encontrado(s)";
        public string TextoBusqueda { get => _texto; set { _texto = value ?? ""; OnPropertyChanged(); Cargar(); } }
        public string EstadoFiltro { get => _estado; set { _estado = value ?? "Todos"; OnPropertyChanged(); Cargar(); } }
        public string MonedaFiltro { get => _moneda; set { _moneda = value ?? "Todos"; OnPropertyChanged(); Cargar(); } }
        public EntidadFinancieraFiltroItem? AseguradoraFiltro { get => _aseguradora; set { _aseguradora = value; OnPropertyChanged(); Cargar(); } }
        private void CargarFiltros() { try { Aseguradoras.Add(new(null, "Todas")); foreach (var a in _negocio.ListarAseguradoras().OrderBy(a => a.Nombre)) Aseguradoras.Add(new(a.IdEntidadFinanciera, a.Nombre)); _aseguradora = Aseguradoras.FirstOrDefault(); } catch (Exception ex) { NotificationService.Error($"No se pudieron cargar las aseguradoras: {ex.Message}"); } }
        private void Cargar()
        {
            try
            {
                Seguros.Clear();
                foreach (var s in _negocio.Listar(TextoBusqueda, EstadoFiltro == "Todos" ? null : EstadoFiltro, AseguradoraFiltro?.IdEntidadFinanciera)
                    .Where(s => MonedaFiltro == "Todos" || string.Equals(s.Moneda, MonedaFiltro, StringComparison.OrdinalIgnoreCase)))
                    Seguros.Add(s);
                OnPropertyChanged(nameof(Resumen));
            }
            catch (Exception ex)
            {
                NotificationService.Error(EsProcedimientoNoInstalado(ex)
                    ? "No se pudo cargar Seguros. Verifique que el procedimiento USP_TES_SEGURO_LISTAR esté instalado en la base de datos."
                    : $"No se pudieron cargar los seguros: {ex.Message}");
            }
        }
        private void Nuevo() { var w = new SeguroEditorWindow { Owner = Application.Current.MainWindow }; if (w.ShowDialog() == true) Cargar(); }
        private void Ver(SeguroResumen? s) { if (s == null) return; var w = new SeguroDetalleWindow(s.IdObligacionFinanciera) { Owner = Application.Current.MainWindow }; w.ShowDialog(); Cargar(); }
        private void Editar(SeguroResumen? s) { if (s == null) return; if(s.Estado=="ANULADA"){NotificationService.Warning("No se puede editar un seguro anulado.");return;} var seguro=_negocio.Obtener(s.IdObligacionFinanciera); if(seguro==null){NotificationService.Warning("No se encontró el seguro.");return;} var w=new SeguroEditorWindow(seguro){Owner=Application.Current.MainWindow};if(w.ShowDialog()==true)Cargar(); }
        private void Anular(SeguroResumen? s) { if (s == null || s.Estado == "ANULADA") return; if (!ConfirmDialogService.Confirmar("¿Desea anular el seguro?", "Confirmar anulación")) return; var w = new MotivoAnulacionWindow("Anular seguro", s.NumeroPoliza) { Owner = Application.Current.MainWindow }; if (w.ShowDialog() != true) return; var r = _negocio.Anular(s.IdObligacionFinanciera, SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema", w.Motivo); if (r.Resultado) { NotificationService.Success(r.Mensaje); Cargar(); } else NotificationService.Warning(r.Mensaje); }
        private static bool EsProcedimientoNoInstalado(Exception ex) =>
            ex.Message.Contains("USP_TES_SEGURO_LISTAR", StringComparison.OrdinalIgnoreCase) &&
            (ex.Message.Contains("procedure", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("procedimiento", StringComparison.OrdinalIgnoreCase));
    }
}
