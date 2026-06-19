using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Shared.Views
{
    public partial class CargaMasivaProductosWindow : Window
    {
        public CargaMasivaProductosWindow(
            string titulo,
            Func<string, CargaMasivaProductoInfo?> buscarProducto,
            bool ampliarVentana = false)
        {
            InitializeComponent();

            if (ampliarVentana)
            {
                Width = 1100;
                Height = 760;
                MinWidth = 900;
                MinHeight = 650;
            }

            DataContext = new CargaMasivaProductosViewModel(titulo, buscarProducto, this);
        }

        public IReadOnlyList<CargaMasivaProductoFila> ProductosSeleccionados { get; private set; } = [];
        public int ErroresEncontrados { get; private set; }

        private class CargaMasivaProductosViewModel : BaseViewModel
        {
            private readonly Func<string, CargaMasivaProductoInfo?> _buscarProducto;
            private readonly CargaMasivaProductosWindow _ventana;
            private string _textoCarga = string.Empty;

            public CargaMasivaProductosViewModel(
                string titulo,
                Func<string, CargaMasivaProductoInfo?> buscarProducto,
                CargaMasivaProductosWindow ventana)
            {
                Titulo = titulo;
                _buscarProducto = buscarProducto;
                _ventana = ventana;
                ProcesarCommand = new RelayCommand(_ => Procesar());
                LimpiarCommand = new RelayCommand(_ => Limpiar());
                AgregarCommand = new RelayCommand(_ => Agregar(), _ => Filas.Any(f => f.EsValido && f.Seleccionado));
                CancelarCommand = new RelayCommand(_ => _ventana.DialogResult = false);
            }

            public string Titulo { get; }
            public ObservableCollection<CargaMasivaProductoFila> Filas { get; } = [];

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
                $"Procesados: {Filas.Count(f => f.EsValido && f.Seleccionado)} | Unidades: {Filas.Where(f => f.EsValido && f.Seleccionado).Sum(f => f.Cantidad):N2} | Errores: {Filas.Count(f => !f.EsValido)}";

            public ICommand ProcesarCommand { get; }
            public ICommand LimpiarCommand { get; }
            public ICommand AgregarCommand { get; }
            public ICommand CancelarCommand { get; }

            private void Procesar()
            {
                CargaMasivaProductoResultado resultado = CargaMasivaProductoService.Procesar(TextoCarga, _buscarProducto);
                Filas.Clear();

                foreach (CargaMasivaProductoFila fila in resultado.Filas)
                {
                    fila.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(CargaMasivaProductoFila.Seleccionado))
                        {
                            OnPropertyChanged(nameof(Resumen));
                            CommandManager.InvalidateRequerySuggested();
                        }
                    };

                    Filas.Add(fila);
                }

                OnPropertyChanged(nameof(Resumen));
                CommandManager.InvalidateRequerySuggested();
            }

            private void Limpiar()
            {
                TextoCarga = string.Empty;
                Filas.Clear();
                OnPropertyChanged(nameof(Resumen));
                CommandManager.InvalidateRequerySuggested();
            }

            private void Agregar()
            {
                _ventana.ProductosSeleccionados = Filas
                    .Where(fila => fila.EsValido && fila.Seleccionado)
                    .ToList();
                _ventana.ErroresEncontrados = Filas.Count(fila => !fila.EsValido);

                _ventana.DialogResult = true;
            }
        }
    }
}
