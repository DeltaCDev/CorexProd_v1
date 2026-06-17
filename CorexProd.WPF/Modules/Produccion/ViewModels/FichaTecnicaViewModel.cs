using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using CorexProd.WPF.Modules.Produccion.Views;
using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Produccion.ViewModels
{
    public class FichaTecnicaViewModel : BaseViewModel
    {
        private readonly FichaTecnicaNegocio _fichaNegocio = new FichaTecnicaNegocio();
        private readonly ProductoNegocio _productoNegocio = new ProductoNegocio();
        private readonly InsumoNegocio _insumoNegocio = new InsumoNegocio();
        private readonly UnidadMedidaNegocio _unidadNegocio = new UnidadMedidaNegocio();

        public ObservableCollection<FichaTecnica> FichasTecnicas { get; set; } = new();
        public ObservableCollection<FichaTecnicaDetalle> Detalles { get; set; } = new();

        public ObservableCollection<Producto> Productos { get; set; } = new();
        public ObservableCollection<Insumo> Insumos { get; set; } = new();
        public ObservableCollection<UnidadMedida> UnidadesMedida { get; set; } = new();

        private FichaTecnica? _fichaSeleccionada;
        public FichaTecnica? FichaSeleccionada
        {
            get => _fichaSeleccionada;
            set
            {
                _fichaSeleccionada = value;
                OnPropertyChanged();

                if (_fichaSeleccionada != null)
                {
                    IdFichaTecnica = _fichaSeleccionada.IdFichaTecnica;
                    ProductoSeleccionado = Productos.FirstOrDefault(x => x.IdProducto == _fichaSeleccionada.IdProducto);
                    Version = _fichaSeleccionada.Version;
                    Observacion = _fichaSeleccionada.Observacion ?? "";
                    Estado = _fichaSeleccionada.Estado;

                    CargarDetalle();
                }
            }
        }

        private Producto? _productoSeleccionado;
        public Producto? ProductoSeleccionado
        {
            get => _productoSeleccionado;
            set
            {
                _productoSeleccionado = value;
                OnPropertyChanged();
            }
        }

        private Insumo? _insumoSeleccionado;
        public Insumo? InsumoSeleccionado
        {
            get => _insumoSeleccionado;
            set
            {
                _insumoSeleccionado = value;
                OnPropertyChanged();

                if (_insumoSeleccionado != null)
                {
                    UnidadSeleccionada = UnidadesMedida.FirstOrDefault(x => x.IdUnidadMedida == _insumoSeleccionado.IdUnidadMedida);
                }
            }
        }

        private UnidadMedida? _unidadSeleccionada;
        public UnidadMedida? UnidadSeleccionada
        {
            get => _unidadSeleccionada;
            set
            {
                _unidadSeleccionada = value;
                OnPropertyChanged();
            }
        }

        private FichaTecnicaDetalle? _detalleSeleccionado;
        public FichaTecnicaDetalle? DetalleSeleccionado
        {
            get => _detalleSeleccionado;
            set
            {
                _detalleSeleccionado = value;
                OnPropertyChanged();

                if (_detalleSeleccionado != null)
                {
                    InsumoSeleccionado = Insumos.FirstOrDefault(x => x.IdInsumo == _detalleSeleccionado.IdInsumo);
                    UnidadSeleccionada = UnidadesMedida.FirstOrDefault(x => x.IdUnidadMedida == _detalleSeleccionado.IdUnidadMedida);
                    Cantidad = _detalleSeleccionado.Cantidad;
                }
            }
        }

        private int _idFichaTecnica;
        public int IdFichaTecnica
        {
            get => _idFichaTecnica;
            set
            {
                _idFichaTecnica = value;
                OnPropertyChanged();
            }
        }

        private int _version = 1;
        public int Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        }

        private string _observacion = "";
        public string Observacion
        {
            get => _observacion;
            set
            {
                _observacion = value;
                OnPropertyChanged();
            }
        }

        private decimal _cantidad;
        public decimal Cantidad
        {
            get => _cantidad;
            set
            {
                _cantidad = value;
                OnPropertyChanged();
            }
        }

        private bool _estado = true;
        public bool Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged();
            }
        }

        public ICommand NuevoCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand AgregarDetalleCommand { get; }
        public ICommand EditarDetalleCommand { get; }
        public ICommand QuitarDetalleCommand { get; }
        public ICommand RefrescarCommand { get; }
        public ICommand EditarFichaCommand { get; }
        public string ResumenRegistros => $"Mostrando {FichasTecnicas.Count} fichas tecnicas";

        public FichaTecnicaViewModel()
        {
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            GuardarCommand = new RelayCommand(_ => Guardar());
            AgregarDetalleCommand = new RelayCommand(_ => AgregarDetalle());
            EditarDetalleCommand = new RelayCommand(_ => EditarDetalle());
            QuitarDetalleCommand = new RelayCommand(_ => QuitarDetalle());
            RefrescarCommand = new RelayCommand(_ => CargarDatos());
            EditarFichaCommand = new RelayCommand(parametro => EditarFicha(parametro));

            CargarDatos();
        }

        private void CargarDatos()
        {
            FichasTecnicas.Clear();
            Productos.Clear();
            Insumos.Clear();
            UnidadesMedida.Clear();

            foreach (var item in _fichaNegocio.Listar())
            {
                item.CantidadInsumos = _fichaNegocio.ListarDetalle(item.IdFichaTecnica).Count;
                FichasTecnicas.Add(item);
            }

            foreach (var item in _productoNegocio.Listar())
                Productos.Add(item);

            foreach (var item in _insumoNegocio.Listar())
                Insumos.Add(item);

            foreach (var item in _unidadNegocio.Listar())
                UnidadesMedida.Add(item);

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void CargarDetalle()
        {
            Detalles.Clear();

            if (IdFichaTecnica <= 0)
                return;

            foreach (var item in _fichaNegocio.ListarDetalle(IdFichaTecnica))
                Detalles.Add(item);
        }

        private void Nuevo()
        {
            IdFichaTecnica = 0;
            ProductoSeleccionado = null;
            Version = 1;
            Observacion = "";
            Estado = true;
            FichaSeleccionada = null;
            Detalles.Clear();

            LimpiarDetalle();
        }

        private void Guardar()
        {
            var ficha = new FichaTecnica
            {
                IdFichaTecnica = IdFichaTecnica,
                IdProducto = ProductoSeleccionado?.IdProducto ?? 0,
                Version = Version,
                Observacion = Observacion,
                Estado = Estado
            };

            string mensaje;
            bool resultado;

            if (IdFichaTecnica == 0)
            {
                resultado = _fichaNegocio.Registrar(ficha, out mensaje);
            }
            else
            {
                resultado = _fichaNegocio.Editar(ficha, out mensaje);
            }

            if (resultado)
            {
                NotificationService.Success(mensaje);
                CargarDatos();
                Nuevo();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void AgregarDetalle()
        {
            if (IdFichaTecnica <= 0)
            {
                NotificationService.Warning("Primero debe guardar la cabecera de la ficha técnica.");
                return;
            }

            var detalle = new FichaTecnicaDetalle
            {
                IdFichaTecnica = IdFichaTecnica,
                IdInsumo = InsumoSeleccionado?.IdInsumo ?? 0,
                Cantidad = Cantidad,
                IdUnidadMedida = UnidadSeleccionada?.IdUnidadMedida ?? 0
            };

            bool resultado = _fichaNegocio.RegistrarDetalle(detalle, out string mensaje);

            if (resultado)
            {
                NotificationService.Success(mensaje);
                CargarDetalle();
                LimpiarDetalle();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void EditarDetalle()
        {
            if (DetalleSeleccionado == null)
            {
                NotificationService.Warning("Seleccione un insumo del detalle.");
                return;
            }

            var detalle = new FichaTecnicaDetalle
            {
                IdFichaTecnicaDetalle = DetalleSeleccionado.IdFichaTecnicaDetalle,
                Cantidad = Cantidad,
                IdUnidadMedida = UnidadSeleccionada?.IdUnidadMedida ?? 0
            };

            bool resultado = _fichaNegocio.EditarDetalle(detalle, out string mensaje);

            if (resultado)
            {
                NotificationService.Success(mensaje);
                CargarDetalle();
                LimpiarDetalle();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void QuitarDetalle()
        {
            if (DetalleSeleccionado == null)
            {
                NotificationService.Warning("Seleccione un insumo del detalle.");
                return;
            }

            bool confirmado = ConfirmDialogService.Confirmar(
                "Quitar insumo",
                "¿Desea quitar este insumo de la ficha técnica?"
            );

            if (!confirmado)
                return;

            bool resultado = _fichaNegocio.EliminarDetalle(
                DetalleSeleccionado.IdFichaTecnicaDetalle,
                out string mensaje
            );

            if (resultado)
            {
                NotificationService.Success(mensaje);
                CargarDetalle();
                LimpiarDetalle();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }
        private void EditarFicha(object? parametro)
        {
            FichaTecnica? ficha = parametro as FichaTecnica ?? FichaSeleccionada;

            if (ficha == null)
            {
                NotificationService.Warning("Seleccione una ficha técnica para editar.");
                return;
            }

            AbrirEditor(ficha);
        }

        private void AbrirEditor(FichaTecnica? ficha)
        {
            try
            {
                int idFichaActual = ficha?.IdFichaTecnica ?? 0;

                var ventana = new FichaTecnicaEditorWindow(ficha)
                {
                    Owner = Application.Current.MainWindow
                };

                ventana.ViewModel.GuardadoExitoso = () =>
                {
                    CargarDatos();

                    if (idFichaActual > 0)
                    {
                        FichaSeleccionada = FichasTecnicas.FirstOrDefault(x => x.IdFichaTecnica == idFichaActual);
                    }
                };

                ventana.ShowDialog();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo abrir el editor de ficha técnica.\n{ex.Message}");
            }
        }

        private void LimpiarDetalle()
        {
            InsumoSeleccionado = null;
            UnidadSeleccionada = null;
            Cantidad = 0;
            DetalleSeleccionado = null;
        }
    }
}
