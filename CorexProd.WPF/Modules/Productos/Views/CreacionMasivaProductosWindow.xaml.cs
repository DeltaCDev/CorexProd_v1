using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Productos.Views
{
    public partial class CreacionMasivaProductosWindow : Window
    {
        public CreacionMasivaProductosWindow(
            IEnumerable<Producto> productosExistentes,
            IEnumerable<CategoriaProducto> categorias,
            IEnumerable<UnidadMedida> unidadesMedida,
            Func<Producto, string> crearProducto)
        {
            InitializeComponent();
            DataContext = new CreacionMasivaProductosViewModel(
                productosExistentes,
                categorias,
                unidadesMedida,
                crearProducto,
                this);
        }

        public bool SeCrearonProductos { get; private set; }

        private class CreacionMasivaProductosViewModel : BaseViewModel
        {
            private readonly List<Producto> _productosExistentes;
            private readonly List<CategoriaProducto> _categorias;
            private readonly List<UnidadMedida> _unidadesMedida;
            private readonly Func<Producto, string> _crearProducto;
            private readonly CreacionMasivaProductosWindow _ventana;
            private string _textoCarga = string.Empty;
            private int _creados;
            private bool _procesado;

            public CreacionMasivaProductosViewModel(
                IEnumerable<Producto> productosExistentes,
                IEnumerable<CategoriaProducto> categorias,
                IEnumerable<UnidadMedida> unidadesMedida,
                Func<Producto, string> crearProducto,
                CreacionMasivaProductosWindow ventana)
            {
                _productosExistentes = productosExistentes.ToList();
                _categorias = categorias.ToList();
                _unidadesMedida = unidadesMedida.ToList();
                _crearProducto = crearProducto;
                _ventana = ventana;

                ProcesarCommand = new RelayCommand(_ => Procesar());
                CrearProductosCommand = new RelayCommand(_ => CrearProductos(), _ => Filas.Any(f => f.EsValido && !f.Creado));
                LimpiarCommand = new RelayCommand(_ => Limpiar());
                CancelarCommand = new RelayCommand(_ => _ventana.Close());
            }

            public ObservableCollection<CreacionMasivaProductoFila> Filas { get; } = [];

            public string TextoCarga
            {
                get => _textoCarga;
                set
                {
                    _textoCarga = value;
                    OnPropertyChanged();
                }
            }

            public string Resumen =>
                $"Lineas procesadas: {Filas.Count} | Productos validos: {Filas.Count(f => f.EsValido)} | Productos rechazados: {Filas.Count(f => !f.EsValido)} | Creados: {_creados}";

            public string DetalleErrores
            {
                get
                {
                    List<CreacionMasivaProductoFila> errores = Filas.Where(fila => !fila.EsValido).ToList();
                    return errores.Count == 0
                        ? string.Empty
                        : string.Join("  |  ", errores.Take(4).Select(fila => $"Linea {fila.NumeroLinea}: {fila.Validacion}"))
                            + (errores.Count > 4 ? $"  |  +{errores.Count - 4} errores mas" : string.Empty);
                }
            }

            public ICommand ProcesarCommand { get; }
            public ICommand CrearProductosCommand { get; }
            public ICommand LimpiarCommand { get; }
            public ICommand CancelarCommand { get; }

            private void Procesar()
            {
                CreacionMasivaProductosResultado resultado = CreacionMasivaProductosService.Procesar(
                    TextoCarga,
                    _productosExistentes,
                    _categorias,
                    _unidadesMedida);

                Filas.Clear();
                _creados = 0;
                _procesado = true;

                foreach (CreacionMasivaProductoFila fila in resultado.Filas)
                {
                    Filas.Add(fila);
                }

                NotificarResumen();
            }

            private void CrearProductos()
            {
                if (!_procesado)
                {
                    Procesar();
                }

                int creados = 0;

                foreach (CreacionMasivaProductoFila fila in Filas.Where(f => f.EsValido && !f.Creado).ToList())
                {
                    string mensaje = _crearProducto(fila.CrearProducto());

                    if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                    {
                        fila.Creado = true;
                        fila.Estado = "Creado";
                        fila.Validacion = "Producto creado correctamente";
                        _productosExistentes.Add(fila.CrearProducto());
                        creados++;
                    }
                    else
                    {
                        fila.EsValido = false;
                        fila.Estado = "Error";
                        fila.Validacion = mensaje;
                    }
                }

                _creados += creados;
                _ventana.SeCrearonProductos = _creados > 0;
                NotificarResumen();
                MostrarResumenFinal();
                CommandManager.InvalidateRequerySuggested();
            }

            private void Limpiar()
            {
                TextoCarga = string.Empty;
                Filas.Clear();
                _creados = 0;
                _procesado = false;
                NotificarResumen();
            }

            private void NotificarResumen()
            {
                OnPropertyChanged(nameof(Resumen));
                OnPropertyChanged(nameof(DetalleErrores));
                CommandManager.InvalidateRequerySuggested();
            }

            private void MostrarResumenFinal()
            {
                int rechazados = Filas.Count(fila => !fila.Creado);
                StringBuilder detalle = new();

                detalle.AppendLine($"Total de lineas procesadas: {Filas.Count}");
                detalle.AppendLine($"Productos creados correctamente: {_creados}");
                detalle.AppendLine($"Productos rechazados: {rechazados}");

                List<CreacionMasivaProductoFila> errores = Filas.Where(fila => !fila.Creado).ToList();

                if (errores.Count > 0)
                {
                    detalle.AppendLine();
                    detalle.AppendLine("Filas con errores:");

                    foreach (CreacionMasivaProductoFila fila in errores.Take(20))
                    {
                        detalle.AppendLine($"Linea {fila.NumeroLinea} ({fila.Codigo}): {fila.Validacion}");
                    }

                    if (errores.Count > 20)
                    {
                        detalle.AppendLine($"... y {errores.Count - 20} filas mas.");
                    }
                }

                MessageBox.Show(detalle.ToString(), "Resumen de creacion masiva", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
