using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Produccion.ViewModels
{
    public class AreaProduccionEditorViewModel : BaseViewModel
    {
        private readonly AreaProduccionNegocio _negocio = new();
        private int _idAreaProduccion;
        private string _codigoArea = string.Empty;
        private string _nombreArea = string.Empty;
        private string _descripcion = string.Empty;
        private int _ordenSecuencia = 1;
        private bool _esInicio;
        private bool _manejaMerma;
        private bool _esTermino;
        private string _modoEnvio = "UNICO";
        private bool _activo = true;

        public int IdAreaProduccion { get => _idAreaProduccion; set { _idAreaProduccion = value; OnPropertyChanged(); OnPropertyChanged(nameof(Titulo)); } }
        public string CodigoArea { get => _codigoArea; set { _codigoArea = value; OnPropertyChanged(); } }
        public string NombreArea { get => _nombreArea; set { _nombreArea = value; OnPropertyChanged(); } }
        public string Descripcion { get => _descripcion; set { _descripcion = value; OnPropertyChanged(); } }
        public int OrdenSecuencia { get => _ordenSecuencia; set { _ordenSecuencia = value; OnPropertyChanged(); } }
        public bool EsInicio { get => _esInicio; set { _esInicio = value; OnPropertyChanged(); } }
        public bool ManejaMerma { get => _manejaMerma; set { _manejaMerma = value; OnPropertyChanged(); } }
        public bool EsTermino { get => _esTermino; set { _esTermino = value; OnPropertyChanged(); } }
        public string ModoEnvio { get => _modoEnvio; set { _modoEnvio = value; OnPropertyChanged(); } }
        public bool Activo { get => _activo; set { _activo = value; OnPropertyChanged(); } }

        public bool SoloLectura { get; }
        public bool PuedeEditar => !SoloLectura;
        public bool Guardado { get; private set; }
        public string Titulo => SoloLectura ? "Detalle del área de producción" : IdAreaProduccion > 0 ? "Editar área de producción" : "Nueva área de producción";
        public IReadOnlyList<string> ModosEnvio { get; } = ["UNICO", "PARCIAL"];
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public Action? CerrarVentana { get; set; }

        public AreaProduccionEditorViewModel(AreaProduccion? area, bool soloLectura)
        {
            SoloLectura = soloLectura;
            GuardarCommand = new RelayCommand(_ => Guardar(), _ => PuedeEditar);
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());

            if (area != null)
            {
                IdAreaProduccion = area.IdAreaProduccion;
                CodigoArea = area.CodigoArea;
                NombreArea = area.NombreArea;
                Descripcion = area.Descripcion;
                OrdenSecuencia = area.OrdenSecuencia;
                EsInicio = area.EsInicio;
                ManejaMerma = area.ManejaMerma;
                EsTermino = area.EsTermino;
                ModoEnvio = area.ModoEnvio;
                Activo = area.Activo;
            }
        }

        private void Guardar()
        {
            try
            {
                if (IdAreaProduccion > 0 && !ConfirmDialogService.Confirmar("¿Desea guardar los cambios del área?", "Confirmar edición"))
                    return;

                int idUsuario = SessionManager.UsuarioActual?.IdUsuario ?? 0;
                AreaProduccion area = new()
                {
                    IdAreaProduccion = IdAreaProduccion,
                    CodigoArea = CodigoArea,
                    NombreArea = NombreArea,
                    Descripcion = Descripcion,
                    OrdenSecuencia = OrdenSecuencia,
                    EsInicio = EsInicio,
                    ManejaMerma = ManejaMerma,
                    EsTermino = EsTermino,
                    ModoEnvio = ModoEnvio,
                    Activo = Activo,
                    UsuarioRegistro = idUsuario,
                    UsuarioModificacion = IdAreaProduccion > 0 ? idUsuario : null
                };

                string mensaje = _negocio.Guardar(area);
                if (mensaje.StartsWith("OK|", StringComparison.Ordinal))
                {
                    NotificationService.Success(mensaje[3..]);
                    Guardado = true;
                    CerrarVentana?.Invoke();
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
