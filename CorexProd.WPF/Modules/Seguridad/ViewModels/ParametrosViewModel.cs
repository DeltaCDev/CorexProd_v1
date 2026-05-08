using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class ParametrosViewModel : BaseViewModel
    {
        private readonly ParametroNegocio _parametroNegocio = new();

        private int _idParametro;
        private string _codigoParametro = string.Empty;
        private string _nombreParametro = string.Empty;
        private string _valorParametro = string.Empty;
        private string _descripcion = string.Empty;
        private bool _estado = true;
        private Parametro? _parametroSeleccionado;

        public ObservableCollection<Parametro> Parametros { get; set; } = [];

        public int IdParametro
        {
            get => _idParametro;
            set
            {
                _idParametro = value;
                OnPropertyChanged();
            }
        }

        public string CodigoParametro
        {
            get => _codigoParametro;
            set
            {
                _codigoParametro = value;
                OnPropertyChanged();
            }
        }

        public string NombreParametro
        {
            get => _nombreParametro;
            set
            {
                _nombreParametro = value;
                OnPropertyChanged();
            }
        }

        public string ValorParametro
        {
            get => _valorParametro;
            set
            {
                _valorParametro = value;
                OnPropertyChanged();
            }
        }

        public string Descripcion
        {
            get => _descripcion;
            set
            {
                _descripcion = value;
                OnPropertyChanged();
            }
        }

        public bool Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged();
            }
        }

        public Parametro? ParametroSeleccionado
        {
            get => _parametroSeleccionado;
            set
            {
                _parametroSeleccionado = value;
                OnPropertyChanged();

                if (_parametroSeleccionado != null)
                {
                    IdParametro = _parametroSeleccionado.IdParametro;
                    CodigoParametro = _parametroSeleccionado.CodigoParametro;
                    NombreParametro = _parametroSeleccionado.NombreParametro;
                    ValorParametro = _parametroSeleccionado.ValorParametro;
                    Descripcion = _parametroSeleccionado.Descripcion;
                    Estado = _parametroSeleccionado.Estado;
                }
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand EliminarCommand { get; }

        public ParametrosViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));

            CargarParametros();
        }

        private void CargarParametros()
        {
            Parametros.Clear();

            foreach (Parametro parametro in _parametroNegocio.Listar())
            {
                Parametros.Add(parametro);
            }
        }

        private void Guardar()
        {
            if (IdParametro > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información del parámetro?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }

            Parametro parametro = new()
            {
                IdParametro = IdParametro,
                CodigoParametro = CodigoParametro,
                NombreParametro = NombreParametro,
                ValorParametro = ValorParametro,
                Descripcion = Descripcion,
                Estado = Estado
            };

            string mensaje = _parametroNegocio.Guardar(parametro);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarParametros();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Eliminar(object? parametro)
        {
            if (parametro == null)
            {
                NotificationService.Warning("Debe seleccionar un parámetro.");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idParametro))
            {
                NotificationService.Warning("Id de parámetro inválido.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar este parámetro?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _parametroNegocio.Eliminar(idParametro);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarParametros();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            IdParametro = 0;
            CodigoParametro = string.Empty;
            NombreParametro = string.Empty;
            ValorParametro = string.Empty;
            Descripcion = string.Empty;
            Estado = true;
            ParametroSeleccionado = null;
        }
    }
}