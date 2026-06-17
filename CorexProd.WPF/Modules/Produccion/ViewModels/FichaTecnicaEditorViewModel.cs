using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Produccion.ViewModels
{
    public class FichaTecnicaEditorViewModel : BaseViewModel
    {
        private readonly FichaTecnicaNegocio _fichaNegocio = new();
        private readonly ProductoNegocio _productoNegocio = new();
        private readonly InsumoNegocio _insumoNegocio = new();
        private readonly UnidadMedidaNegocio _unidadNegocio = new();

        public ObservableCollection<Producto> Productos { get; } = new();
        public ObservableCollection<Insumo> Insumos { get; } = new();
        public ObservableCollection<UnidadMedida> UnidadesMedida { get; } = new();
        public ObservableCollection<FichaTecnicaDetalle> Detalles { get; } = new();

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand AgregarDetalleCommand { get; }
        public ICommand EditarDetalleCommand { get; }
        public ICommand QuitarDetalleCommand { get; }

        public Action? CerrarVentana { get; set; }
        public Action? GuardadoExitoso { get; set; }

        public int IdFichaTecnica { get; private set; }
        public bool EsNuevo => IdFichaTecnica == 0;
        public string TituloEditor => EsNuevo ? "Nueva Ficha Tecnica" : "Editar Ficha Tecnica";

        private Producto? _productoSeleccionado;
        public Producto? ProductoSeleccionado { get => _productoSeleccionado; set { _productoSeleccionado = value; OnPropertyChanged(); } }

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
                    var detalleExistente = Detalles.FirstOrDefault(x => x.IdInsumo == _insumoSeleccionado.IdInsumo);
                    if (detalleExistente != null)
                    {
                        Cantidad = detalleExistente.Cantidad;
                        UnidadSeleccionada = UnidadesMedida.FirstOrDefault(x => x.IdUnidadMedida == detalleExistente.IdUnidadMedida);
                    }
                    else
                    {
                        Cantidad = 0;
                        UnidadSeleccionada = UnidadesMedida.FirstOrDefault(x => x.IdUnidadMedida == _insumoSeleccionado.IdUnidadMedida);
                    }
                }
            }
        }

        private UnidadMedida? _unidadSeleccionada;
        public UnidadMedida? UnidadSeleccionada { get => _unidadSeleccionada; set { _unidadSeleccionada = value; OnPropertyChanged(); } }

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

        private int _version;
        public int Version { get => _version; set { _version = value; OnPropertyChanged(); } }

        private string _observacion = string.Empty;
        public string Observacion { get => _observacion; set { _observacion = value; OnPropertyChanged(); } }

        private bool _estado;
        public bool Estado { get => _estado; set { _estado = value; OnPropertyChanged(); } }

        private decimal _cantidad;
        public decimal Cantidad { get => _cantidad; set { _cantidad = value; OnPropertyChanged(); } }

        public FichaTecnicaEditorViewModel(FichaTecnica? fichaSeleccionada)
        {
            IdFichaTecnica = fichaSeleccionada?.IdFichaTecnica ?? 0;
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            AgregarDetalleCommand = new RelayCommand(_ => AgregarDetalle());
            EditarDetalleCommand = new RelayCommand(_ => EditarDetalle());
            QuitarDetalleCommand = new RelayCommand(_ => QuitarDetalle());

            foreach (var item in _productoNegocio.Listar()) Productos.Add(item);
            MarcarProductosConFichaTecnica();
            foreach (var item in _insumoNegocio.Listar()) Insumos.Add(item);
            foreach (var item in _unidadNegocio.Listar()) UnidadesMedida.Add(item);

            ProductoSeleccionado = fichaSeleccionada == null
                ? null
                : Productos.FirstOrDefault(x => x.IdProducto == fichaSeleccionada.IdProducto);
            Version = fichaSeleccionada?.Version ?? 1;
            Observacion = fichaSeleccionada?.Observacion ?? string.Empty;
            Estado = fichaSeleccionada?.Estado ?? true;

            CargarDetalle();
        }

        private void MarcarProductosConFichaTecnica()
        {
            var productosConFicha = _fichaNegocio.Listar()
                .Select(x => x.IdProducto)
                .ToHashSet();

            foreach (var producto in Productos)
            {
                producto.TieneFichaTecnica = productosConFicha.Contains(producto.IdProducto);
            }
        }

        private void CargarDetalle()
        {
            Detalles.Clear();
            if (IdFichaTecnica <= 0)
            {
                return;
            }

            foreach (var item in _fichaNegocio.ListarDetalle(IdFichaTecnica))
                Detalles.Add(item);
        }

        private void Guardar()
        {
            var ficha = new FichaTecnica { IdFichaTecnica = IdFichaTecnica, IdProducto = ProductoSeleccionado?.IdProducto ?? 0, Version = Version, Observacion = Observacion, Estado = Estado };
            bool resultado;
            string mensaje;

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
                MarcarProductosConFichaTecnica();
                GuardadoExitoso?.Invoke();
                if (IdFichaTecnica == 0)
                {
                    CerrarVentana?.Invoke();
                }
            }
            else NotificationService.Warning(mensaje);
        }

        private void AgregarDetalle()
        {
            if (IdFichaTecnica <= 0)
            {
                NotificationService.Warning("Primero debe guardar la cabecera de la ficha técnica.");
                return;
            }

            var detalle = new FichaTecnicaDetalle { IdFichaTecnica = IdFichaTecnica, IdInsumo = InsumoSeleccionado?.IdInsumo ?? 0, Cantidad = Cantidad, IdUnidadMedida = UnidadSeleccionada?.IdUnidadMedida ?? 0 };
            bool resultado = _fichaNegocio.RegistrarDetalle(detalle, out string mensaje);
            if (resultado) { NotificationService.Success(mensaje); CargarDetalle(); LimpiarDetalle(); } else NotificationService.Warning(mensaje);
        }

        private void EditarDetalle()
        {
            if (DetalleSeleccionado == null) { NotificationService.Warning("Seleccione un insumo del detalle."); return; }
            var detalle = new FichaTecnicaDetalle { IdFichaTecnicaDetalle = DetalleSeleccionado.IdFichaTecnicaDetalle, Cantidad = Cantidad, IdUnidadMedida = UnidadSeleccionada?.IdUnidadMedida ?? 0 };
            bool resultado = _fichaNegocio.EditarDetalle(detalle, out string mensaje);
            if (resultado) { NotificationService.Success(mensaje); CargarDetalle(); LimpiarDetalle(); } else NotificationService.Warning(mensaje);
        }

        private void QuitarDetalle()
        {
            if (DetalleSeleccionado == null) { NotificationService.Warning("Seleccione un insumo del detalle."); return; }
            bool confirmado = ConfirmDialogService.Confirmar("Quitar insumo", "¿Desea quitar este insumo de la ficha técnica?");
            if (!confirmado) return;
            bool resultado = _fichaNegocio.EliminarDetalle(DetalleSeleccionado.IdFichaTecnicaDetalle, out string mensaje);
            if (resultado) { NotificationService.Success(mensaje); CargarDetalle(); LimpiarDetalle(); } else NotificationService.Warning(mensaje);
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
