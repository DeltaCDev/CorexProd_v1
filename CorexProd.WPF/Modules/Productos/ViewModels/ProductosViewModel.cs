using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Productos.Views;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System;
using System.Windows;

namespace CorexProd.WPF.Modules.Productos.ViewModels
{
    public class ProductosViewModel : BaseViewModel
    {
        private readonly ProductoNegocio _productoNegocio = new();
        private readonly CategoriaProductoNegocio _categoriaProductoNegocio = new();
        private readonly UnidadMedidaNegocio _unidadMedidaNegocio = new();
        private readonly ParametroNegocio _parametroNegocio = new();
        private readonly List<Producto> _todosLosProductos = [];

        private int _idProducto;
        private string _codigo = string.Empty;
        private string _nombreProducto = string.Empty;
        private int _idCategoriaProducto;
        private int _idUnidadMedida;
        private decimal _stockMinimo;
        private bool _estado = true;
        private Producto? _productoSeleccionado;
        private string _filtroCodigo = string.Empty;
        private string _filtroNombre = string.Empty;
        private int _idCategoriaFiltro;

        public ObservableCollection<Producto> Productos { get; set; } = [];
        public ObservableCollection<CategoriaProducto> Categorias { get; set; } = [];
        public ObservableCollection<CategoriaProducto> CategoriasFiltro { get; set; } = [];
        public ObservableCollection<UnidadMedida> UnidadesMedida { get; set; } = [];

        public int IdProducto
        {
            get => _idProducto;
            set
            {
                _idProducto = value;
                OnPropertyChanged();
            }
        }

        public string Codigo
        {
            get => _codigo;
            set
            {
                _codigo = value;
                OnPropertyChanged();
            }
        }

        public string NombreProducto
        {
            get => _nombreProducto;
            set
            {
                _nombreProducto = value;
                OnPropertyChanged();
            }
        }

        public int IdCategoriaProducto
        {
            get => _idCategoriaProducto;
            set
            {
                _idCategoriaProducto = value;
                OnPropertyChanged();
            }
        }

        public int IdUnidadMedida
        {
            get => _idUnidadMedida;
            set
            {
                _idUnidadMedida = value;
                OnPropertyChanged();
            }
        }

        public decimal StockMinimo
        {
            get => _stockMinimo;
            set
            {
                _stockMinimo = value;
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

        public Producto? ProductoSeleccionado
        {
            get => _productoSeleccionado;
            set
            {
                _productoSeleccionado = value;
                OnPropertyChanged();

                if (_productoSeleccionado != null)
                {
                    IdProducto = _productoSeleccionado.IdProducto;
                    Codigo = _productoSeleccionado.Codigo;
                    NombreProducto = _productoSeleccionado.NombreProducto;
                    IdCategoriaProducto = _productoSeleccionado.IdCategoriaProducto;
                    IdUnidadMedida = _productoSeleccionado.IdUnidadMedida;
                    StockMinimo = _productoSeleccionado.StockMinimo;
                    Estado = _productoSeleccionado.Estado;
                }
            }
        }

        public string FiltroCodigo
        {
            get => _filtroCodigo;
            set
            {
                _filtroCodigo = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public string FiltroNombre
        {
            get => _filtroNombre;
            set
            {
                _filtroNombre = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public int IdCategoriaFiltro
        {
            get => _idCategoriaFiltro;
            set
            {
                _idCategoriaFiltro = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand LimpiarCommand { get; }
        public ICommand EliminarCommand { get; }
        public ICommand VerFichaTecnicaCommand { get; }
        public ICommand NuevoCommand { get; }
        public ICommand CreacionMasivaCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand RefrescarCommand { get; }
        public ICommand LimpiarFiltrosCommand { get; }
        public ICommand ExportarCommand { get; }
        public ICommand CerrarCommand { get; }

        public Action? CerrarVentana { get; set; }
        public bool Guardado { get; private set; }
        public string TituloEditor => IdProducto > 0 ? "Editar Producto" : "Nuevo Producto";
        public string ResumenRegistros => $"Mostrando {Productos.Count} productos";

        public ProductosViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());
            EliminarCommand = new RelayCommand(parametro => Eliminar(parametro));
            VerFichaTecnicaCommand = new RelayCommand(parametro => VerFichaTecnica(parametro));
            NuevoCommand = new RelayCommand(_ => AbrirEditor(null));
            CreacionMasivaCommand = new RelayCommand(_ => AbrirCreacionMasiva());
            EditarCommand = new RelayCommand(parametro => Editar(parametro));
            RefrescarCommand = new RelayCommand(_ => Refrescar());
            LimpiarFiltrosCommand = new RelayCommand(_ => LimpiarFiltros());
            ExportarCommand = new RelayCommand(_ => Exportar());
            CerrarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());

            CargarCategorias();
            CargarUnidadesMedida();
            CargarProductos();
        }

        private void CargarProductos()
        {
            _todosLosProductos.Clear();
            _todosLosProductos.AddRange(_productoNegocio.Listar());
            AplicarFiltros();
        }

        private void CargarCategorias()
        {
            Categorias.Clear();
            CategoriasFiltro.Clear();
            CategoriasFiltro.Add(new CategoriaProducto
            {
                IdCategoriaProducto = 0,
                NombreCategoria = "Todas las categorías"
            });

            foreach (CategoriaProducto categoria in _categoriaProductoNegocio.Listar())
            {
                if (categoria.Estado)
                {
                    Categorias.Add(categoria);
                    CategoriasFiltro.Add(categoria);
                }
            }
        }

        private void AplicarFiltros()
        {
            string codigo = FiltroCodigo.Trim();
            string nombre = FiltroNombre.Trim();

            List<Producto> filtrados = _todosLosProductos
                .Where(producto =>
                    (string.IsNullOrWhiteSpace(codigo)
                        || producto.Codigo.Contains(codigo, StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrWhiteSpace(nombre)
                        || producto.NombreProducto.Contains(nombre, StringComparison.OrdinalIgnoreCase))
                    && (IdCategoriaFiltro == 0
                        || producto.IdCategoriaProducto == IdCategoriaFiltro))
                .OrderBy(producto => producto.NombreProducto)
                .ToList();

            Productos.Clear();

            foreach (Producto producto in filtrados)
            {
                Productos.Add(producto);
            }

            OnPropertyChanged(nameof(ResumenRegistros));
        }

        private void LimpiarFiltros()
        {
            _filtroCodigo = string.Empty;
            _filtroNombre = string.Empty;
            _idCategoriaFiltro = 0;
            OnPropertyChanged(nameof(FiltroCodigo));
            OnPropertyChanged(nameof(FiltroNombre));
            OnPropertyChanged(nameof(IdCategoriaFiltro));
            AplicarFiltros();
        }

        private void Exportar()
        {
            if (Productos.Count == 0)
            {
                NotificationService.Warning("No hay productos para exportar.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Exportar productos",
                FileName = $"Productos_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                DefaultExt = ".csv",
                Filter = "Archivo CSV para Excel (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                StringBuilder contenido = new();
                contenido.AppendLine("ID;Código;Producto;Categoría;Unidad;Stock mínimo;Estado");

                foreach (Producto producto in Productos)
                {
                    contenido.AppendLine(string.Join(";",
                        producto.IdProducto,
                        EscaparCsv(producto.Codigo),
                        EscaparCsv(producto.NombreProducto),
                        EscaparCsv(producto.NombreCategoria),
                        EscaparCsv(producto.NombreUnidad),
                        producto.StockMinimo.ToString("0.00"),
                        producto.Estado ? "Activo" : "Inactivo"));
                }

                File.WriteAllText(dialog.FileName, contenido.ToString(), new UTF8Encoding(true));
                NotificationService.Success($"Se exportaron {Productos.Count} productos correctamente.");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo exportar los productos: {ex.Message}");
            }
        }

        private static string EscaparCsv(string valor)
        {
            return $"\"{valor.Replace("\"", "\"\"")}\"";
        }

        private void CargarUnidadesMedida()
        {
            UnidadesMedida.Clear();

            foreach (UnidadMedida unidad in _unidadMedidaNegocio.Listar())
            {
                if (unidad.Estado)
                {
                    UnidadesMedida.Add(unidad);
                }
            }
        }

        private void Guardar()
        {
            if (IdProducto > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información del producto?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }

            Producto producto = new()
            {
                IdProducto = IdProducto,
                Codigo = Codigo,
                NombreProducto = NombreProducto,
                IdCategoriaProducto = IdCategoriaProducto,
                IdUnidadMedida = IdUnidadMedida,
                StockMinimo = StockMinimo,
                Estado = Estado
            };

            string mensaje = _productoNegocio.Guardar(producto);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarProductos();
                Guardado = true;
                CerrarVentana?.Invoke();
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
                NotificationService.Warning("Debe seleccionar un producto.");
                return;
            }

            if (!int.TryParse(parametro.ToString(), out int idProducto))
            {
                NotificationService.Warning("Id de producto inválido.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Está seguro de eliminar este producto?",
                "Confirmar eliminación");

            if (!confirmar)
            {
                return;
            }

            string mensaje = _productoNegocio.Eliminar(idProducto);

            if (mensaje.Contains("correctamente"))
            {
                NotificationService.Success(mensaje);
                CargarProductos();
                Limpiar();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void Limpiar()
        {
            IdProducto = 0;
            Codigo = string.Empty;
            NombreProducto = string.Empty;
            IdCategoriaProducto = 0;
            IdUnidadMedida = 0;
            StockMinimo = 0;
            Estado = true;
            ProductoSeleccionado = null;
            OnPropertyChanged(nameof(TituloEditor));
        }

        private void Refrescar()
        {
            CargarCategorias();
            CargarUnidadesMedida();
            CargarProductos();
        }

        private void AbrirEditor(Producto? producto)
        {
            ProductosViewModel viewModel = new();

            if (producto != null)
            {
                viewModel.IdProducto = producto.IdProducto;
                viewModel.Codigo = producto.Codigo;
                viewModel.NombreProducto = producto.NombreProducto;
                viewModel.IdCategoriaProducto = producto.IdCategoriaProducto;
                viewModel.IdUnidadMedida = producto.IdUnidadMedida;
                viewModel.StockMinimo = producto.StockMinimo;
                viewModel.Estado = producto.Estado;
                viewModel.OnPropertyChanged(nameof(TituloEditor));
            }

            ProductoEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
            {
                Refrescar();
                Limpiar();
            }
        }

        private void AbrirCreacionMasiva()
        {
            CreacionMasivaProductosWindow ventana = new(
                Productos.ToList(),
                _categoriaProductoNegocio.Listar(),
                _unidadMedidaNegocio.Listar(),
                producto => _productoNegocio.Guardar(producto))
            {
                Owner = Application.Current.MainWindow
            };

            ventana.ShowDialog();

            if (ventana.SeCrearonProductos)
            {
                Refrescar();
                Limpiar();
            }
        }

        private void Editar(object? parametro)
        {
            if (parametro is not Producto producto)
            {
                NotificationService.Warning("Debe seleccionar un producto.");
                return;
            }

            AbrirEditor(producto);
        }

        private void VerFichaTecnica(object? parametro)
        {
            try
            {
                if (parametro == null)
                {
                    NotificationService.Warning("Debe seleccionar un producto.");
                    return;
                }

                string codigo = parametro.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    NotificationService.Warning("El producto no tiene código.");
                    return;
                }

                var parametroRuta = _parametroNegocio.ObtenerPorCodigo("RUTA_FICHA_TECNICA");

                if (parametroRuta == null)
                {
                    NotificationService.Warning("No existe el parámetro RUTA_FICHA_TECNICA.");
                    return;
                }

                string rutaBase = parametroRuta.ValorParametro;

                if (string.IsNullOrWhiteSpace(rutaBase))
                {
                    NotificationService.Warning("La ruta de fichas técnicas está vacía.");
                    return;
                }

                string rutaPdf = Path.Combine(rutaBase, $"{codigo}.pdf");

                if (!File.Exists(rutaPdf))
                {
                    NotificationService.Warning($"No se encontró la ficha técnica:\n{rutaPdf}");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = rutaPdf,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error al abrir la ficha técnica:\n{ex.Message}");
            }
        }
    }
}
