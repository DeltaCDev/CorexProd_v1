using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Ventas.Views;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Ventas.ViewModels
{
    public class OrdenesCompraInternaViewModel : BaseViewModel
    {
        private readonly OrdenCompraInternaNegocio _negocio = new();
        private readonly GuiaInternaNegocio _guiaInternaNegocio = new();
        private readonly List<OrdenCompraInterna> _todas = [];
        private string _textoBusqueda = string.Empty;
        private string _estadoFiltro = "Todos";
        private DateTime? _fechaDesde;
        private DateTime? _fechaHasta;

        public ObservableCollection<OrdenCompraInterna> Ordenes { get; } = [];
        public ObservableCollection<string> Estados { get; } = ["Todos", "Registrada", "Parcial", "Despachada", "Anulada"];

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value; OnPropertyChanged(); AplicarFiltros(); }
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set { _estadoFiltro = value; OnPropertyChanged(); AplicarFiltros(); }
        }

        public DateTime? FechaDesde
        {
            get => _fechaDesde;
            set { _fechaDesde = value; OnPropertyChanged(); AplicarFiltros(); }
        }

        public DateTime? FechaHasta
        {
            get => _fechaHasta;
            set { _fechaHasta = value; OnPropertyChanged(); AplicarFiltros(); }
        }

        public string Resumen => $"Mostrando {Ordenes.Count} de {_todas.Count} órdenes";
        public decimal TotalVisible => Ordenes.Sum(orden => orden.Total);

        public ICommand VerCommand { get; }
        public ICommand GenerarOtCommand { get; }
        public ICommand GenerarGuiaSalidaCommand { get; }
        public ICommand AnularCommand { get; }
        public ICommand ExportarCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand LimpiarCommand { get; }

        public OrdenesCompraInternaViewModel()
        {
            VerCommand = new RelayCommand(Ver);
            GenerarOtCommand = new RelayCommand(GenerarOt, PuedeGenerarOt);
            GenerarGuiaSalidaCommand = new RelayCommand(GenerarGuiaSalida, PuedeGenerarGuiaSalida);
            AnularCommand = new RelayCommand(Anular, PuedeAnular);
            ExportarCommand = new RelayCommand(_ => Exportar());
            ActualizarCommand = new RelayCommand(_ => Cargar());
            LimpiarCommand = new RelayCommand(_ => Limpiar());

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Cargar();
            }
        }

        private void Cargar()
        {
            try
            {
                _todas.Clear();
                _todas.AddRange(_negocio.Listar());
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo cargar las OCI: {ex.Message}");
            }
        }

        private void AplicarFiltros()
        {
            string texto = TextoBusqueda.Trim();
            IEnumerable<OrdenCompraInterna> resultado = _todas;

            if (EstadoFiltro != "Todos")
                resultado = resultado.Where(o => o.Estado.Equals(EstadoFiltro, StringComparison.OrdinalIgnoreCase));
            if (FechaDesde.HasValue)
                resultado = resultado.Where(o => o.FechaEmision.Date >= FechaDesde.Value.Date);
            if (FechaHasta.HasValue)
                resultado = resultado.Where(o => o.FechaEmision.Date <= FechaHasta.Value.Date);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                resultado = resultado.Where(o =>
                    Contiene(o.NumeroOci, texto) || Contiene(o.NumeroProforma, texto) ||
                    Contiene(o.OrdenCompraCliente, texto) || Contiene(o.NombreCliente, texto));
            }

            Ordenes.Clear();
            foreach (OrdenCompraInterna orden in resultado) Ordenes.Add(orden);
            OnPropertyChanged(nameof(Resumen));
            OnPropertyChanged(nameof(TotalVisible));
        }

        private void Limpiar()
        {
            _textoBusqueda = string.Empty;
            _estadoFiltro = "Todos";
            _fechaDesde = null;
            _fechaHasta = null;
            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(EstadoFiltro));
            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));
            AplicarFiltros();
        }

        private void Ver(object? parametro)
        {
            if (parametro is not OrdenCompraInterna fila) return;
            OrdenCompraInterna? orden = _negocio.Obtener(fila.IdOrdenCompraInterna);
            if (orden == null)
            {
                NotificationService.Warning("No se encontró la orden seleccionada.");
                return;
            }

            new OrdenCompraInternaDetalleWindow(orden) { Owner = Application.Current.MainWindow }.ShowDialog();
        }

        private static bool PuedeGenerarOt(object? parametro) =>
            parametro is OrdenCompraInterna orden && orden.PuedeGenerarOt;

        private void GenerarOt(object? parametro)
        {
            if (parametro is not OrdenCompraInterna orden
                || !_negocio.RequiereOrdenTrabajo(orden.IdOrdenCompraInterna))
            {
                NotificationService.Warning("La OCI ya no requiere una orden de trabajo. Actualice la lista.");
                Cargar();
                return;
            }

            NotificationService.Info("No existe stock suficiente para completar el despacho de esta OCI.");
        }

        private static bool PuedeGenerarGuiaSalida(object? parametro) =>
            parametro is OrdenCompraInterna orden && orden.PuedeGenerarGuiaSalida;

        private void GenerarGuiaSalida(object? parametro)
        {
            if (parametro is not OrdenCompraInterna orden
                || !_negocio.PuedeGenerarGuiaSalida(orden.IdOrdenCompraInterna))
            {
                NotificationService.Warning("La OCI ya no tiene productos pendientes con stock disponible.");
                Cargar();
                return;
            }

            GuiaInterna? guia = _guiaInternaNegocio.Preparar(orden.IdOrdenCompraInterna);
            if (guia == null || guia.Detalles.Count == 0)
            {
                NotificationService.Warning("No se encontraron productos pendientes para preparar la Guía Interna.");
                Cargar();
                return;
            }

            GuiaInternaPreviaWindow previa = new(guia)
            {
                Owner = Application.Current.MainWindow
            };
            if (previa.ShowDialog() == true)
                Cargar();
        }

        private static bool PuedeAnular(object? parametro) =>
            parametro is OrdenCompraInterna orden && orden.PuedeAnular;

        private void Anular(object? parametro)
        {
            if (parametro is not OrdenCompraInterna orden) return;

            AnularOciWindow ventana = new(orden.NumeroOci)
            {
                Owner = Application.Current.MainWindow
            };
            if (ventana.ShowDialog() != true) return;

            string usuario = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
            string mensaje = _negocio.Anular(
                orden.IdOrdenCompraInterna,
                ventana.MotivoAnulacion,
                usuario);
            if (mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
                NotificationService.Success(mensaje);
            else
                NotificationService.Warning(mensaje);

            Cargar();
        }

        private void Exportar()
        {
            if (Ordenes.Count == 0)
            {
                NotificationService.Warning("No hay órdenes para exportar.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Exportar órdenes de compra interna",
                FileName = $"OCI_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Filter = "Archivo CSV para Excel (*.csv)|*.csv",
                DefaultExt = ".csv"
            };
            if (dialog.ShowDialog() != true) return;

            StringBuilder csv = new();
            csv.AppendLine("OCI;Orden compra cliente;Proforma;Estado;Cliente;Fecha emisión;Subtotal;Descuento;IGV;Total");
            foreach (OrdenCompraInterna orden in Ordenes)
            {
                csv.AppendLine(string.Join(";", Escapar(orden.NumeroOci), Escapar(orden.OrdenCompraCliente),
                    Escapar(orden.NumeroProforma), Escapar(orden.Estado), Escapar(orden.NombreCliente),
                    orden.FechaEmision.ToString("dd/MM/yyyy"), orden.Subtotal.ToString("0.00"),
                    orden.Descuento.ToString("0.00"), orden.Igv.ToString("0.00"), orden.Total.ToString("0.00")));
            }
            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
            NotificationService.Success($"Se exportaron {Ordenes.Count} órdenes correctamente.");
        }

        private static bool Contiene(string valor, string texto) =>
            valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

        private static string Escapar(string valor) => $"\"{valor.Replace("\"", "\"\"")}\"";
    }
}
