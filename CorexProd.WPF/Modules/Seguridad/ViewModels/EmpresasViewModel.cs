using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Seguridad.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class EmpresasViewModel : BaseViewModel
    {
        private readonly EmpresaNegocio _empresaNegocio = new();
        private readonly List<Empresa> _todasLasEmpresas = [];
        private Empresa? _empresaSeleccionada;
        private string _textoBusqueda = string.Empty;

        public ObservableCollection<Empresa> Empresas { get; } = [];

        public Empresa? EmpresaSeleccionada
        {
            get => _empresaSeleccionada;
            set
            {
                _empresaSeleccionada = value;
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

        public string ResumenRegistros => $"Mostrando {Empresas.Count} de {_todasLasEmpresas.Count} empresas";

        public ICommand NuevoCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand RefrescarCommand { get; }

        public EmpresasViewModel()
        {
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            EditarCommand = new RelayCommand(parametro => AbrirEditor(parametro as Empresa));
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            RefrescarCommand = new RelayCommand(_ => CargarEmpresas());

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                CargarEmpresas();
            }
        }

        private void CargarEmpresas()
        {
            _todasLasEmpresas.Clear();

            foreach (Empresa empresa in _empresaNegocio.Listar())
            {
                _todasLasEmpresas.Add(empresa);
            }

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            string busqueda = TextoBusqueda.Trim();
            IEnumerable<Empresa> filtradas = _todasLasEmpresas;

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                filtradas = filtradas.Where(e =>
                    Contiene(e.Ruc, busqueda) ||
                    Contiene(e.Nombre, busqueda) ||
                    Contiene(e.NombreComercial, busqueda) ||
                    Contiene(e.Correo, busqueda) ||
                    Contiene(e.Distrito, busqueda) ||
                    Contiene(e.Direccion, busqueda));
            }

            Empresas.Clear();

            foreach (Empresa empresa in filtradas)
            {
                Empresas.Add(empresa);
            }

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return valor?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) == true;
        }

        private void AbrirEditor(Empresa? empresa)
        {
            EmpresaEditorViewModel viewModel = new(empresa, _todasLasEmpresas.Count == 0);
            EmpresaEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                CargarEmpresas();
            }
        }

        private void Eliminar(object? parametro)
        {
            if (parametro == null || !int.TryParse(parametro.ToString(), out int idEmpresa))
            {
                NotificationService.Warning("Debe seleccionar una empresa");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Esta seguro de eliminar esta empresa?",
                "Confirmar eliminacion");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _empresaNegocio.Eliminar(idEmpresa);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarEmpresas();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }
    }
}
