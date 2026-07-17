using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class FormasPagoOsViewModel : BaseViewModel
    {
        private readonly FormaPagoOsNegocio _negocio = new();
        private FormaPagoOs? _formaSeleccionada;
        private int _idFormaPagoOs;
        private string _nombre = string.Empty;
        private bool _estado = true;

        public ObservableCollection<FormaPagoOs> Formas { get; } = [];

        public FormaPagoOs? FormaSeleccionada
        {
            get => _formaSeleccionada;
            set
            {
                _formaSeleccionada = value;
                OnPropertyChanged();
                if (value == null)
                    return;
                IdFormaPagoOs = value.IdFormaPagoOs;
                Nombre = value.Nombre;
                Estado = value.Estado;
            }
        }

        public int IdFormaPagoOs { get => _idFormaPagoOs; set { _idFormaPagoOs = value; OnPropertyChanged(); } }
        public string Nombre { get => _nombre; set { _nombre = value ?? string.Empty; OnPropertyChanged(); } }
        public bool Estado { get => _estado; set { _estado = value; OnPropertyChanged(); } }
        public string ResumenRegistros => $"Mostrando {Formas.Count} formas de pago OS";

        public ICommand GuardarCommand { get; }
        public ICommand NuevoCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand RefrescarCommand { get; }

        public FormasPagoOsViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            NuevoCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(_ => Eliminar());
            RefrescarCommand = new RelayCommand(_ => Cargar());
            Cargar();
        }

        private void Cargar()
        {
            Formas.Clear();
            foreach (FormaPagoOs forma in _negocio.Listar())
                Formas.Add(forma);
            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void Guardar()
        {
            FormaPagoOs forma = new()
            {
                IdFormaPagoOs = IdFormaPagoOs,
                Nombre = Nombre,
                Estado = Estado
            };

            string mensaje = _negocio.Guardar(forma);
            MostrarResultado(mensaje);
            if (mensaje.Contains("correctamente", System.StringComparison.OrdinalIgnoreCase))
            {
                Cargar();
                Limpiar();
            }
        }

        private void Eliminar()
        {
            if (IdFormaPagoOs <= 0)
            {
                NotificationService.Warning("Debe seleccionar una forma de pago OS.");
                return;
            }

            string mensaje = _negocio.Eliminar(IdFormaPagoOs);
            MostrarResultado(mensaje);
            if (mensaje.Contains("correctamente", System.StringComparison.OrdinalIgnoreCase))
            {
                Cargar();
                Limpiar();
            }
        }

        private void Limpiar()
        {
            IdFormaPagoOs = 0;
            Nombre = string.Empty;
            Estado = true;
            FormaSeleccionada = null;
        }

        private static void MostrarResultado(string mensaje)
        {
            if (mensaje.Contains("correctamente", System.StringComparison.OrdinalIgnoreCase))
                NotificationService.Success(mensaje);
            else
                NotificationService.Warning(mensaje);
        }
    }
}
