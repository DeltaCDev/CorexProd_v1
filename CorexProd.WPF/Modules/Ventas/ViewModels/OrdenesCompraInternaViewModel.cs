using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Ventas.Views;
using CorexProd.WPF.Modules.Produccion.Views;
using CorexProd.WPF.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly EmpresaNegocio _empresaNegocio = new();
        private readonly List<OrdenCompraInterna> _todas = [];
        private string _textoBusqueda = string.Empty;
        private string _estadoFiltro = "Todos";
        private DateTime? _fechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime? _fechaHasta = DateTime.Today;
        private bool _filtroPredeterminado = true;

        public ObservableCollection<OrdenCompraInterna> Ordenes { get; } = [];
        public ObservableCollection<string> Estados { get; } = ["Todos", "Emitida", "PROCESO", "Parcial", "Entregado", "Anulado"];

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value; _filtroPredeterminado = false; OnPropertyChanged(); AplicarFiltros(); }
        }

        public string EstadoFiltro
        {
            get => _estadoFiltro;
            set { _estadoFiltro = value; _filtroPredeterminado = false; OnPropertyChanged(); AplicarFiltros(); }
        }

        public DateTime? FechaDesde
        {
            get => _fechaDesde;
            set { _fechaDesde = value; _filtroPredeterminado = false; OnPropertyChanged(); AplicarFiltros(); }
        }

        public DateTime? FechaHasta
        {
            get => _fechaHasta;
            set { _fechaHasta = value; _filtroPredeterminado = false; OnPropertyChanged(); AplicarFiltros(); }
        }

        public string Resumen => $"Mostrando {Ordenes.Count} de {_todas.Count} órdenes";
        public decimal TotalVisible => Ordenes.Sum(orden => orden.Total);

        public ICommand VerCommand { get; }
        public ICommand NuevoCommand { get; }
        public ICommand ImprimirCommand { get; }
        public ICommand CopiarCommand { get; }
        public ICommand EditarCommand { get; }
        public ICommand GenerarOtCommand { get; }
        public ICommand GenerarGuiaSalidaCommand { get; }
        public ICommand AnularCommand { get; }
        public ICommand ExportarCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand LimpiarCommand { get; }

        public OrdenesCompraInternaViewModel()
        {
            NuevoCommand = new RelayCommand(_ => Nuevo());
            VerCommand = new RelayCommand(Ver);
            ImprimirCommand = new RelayCommand(Imprimir);
            CopiarCommand = new RelayCommand(Copiar);
            EditarCommand = new RelayCommand(Editar, PuedeEditar);
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

        private void Nuevo()
        {
            ProformaEditorViewModel viewModel = new(null, false, crearOrdenCompraDirecta: true);
            ProformaEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
                Cargar();
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

            if (_filtroPredeterminado)
                resultado = resultado.Where(o => EsOciActiva(o.Estado) || CoincideRango(o.FechaEmision));
            else if (EstadoFiltro != "Todos")
                resultado = resultado.Where(o => o.Estado.Equals(EstadoFiltro, StringComparison.OrdinalIgnoreCase));
            if (!_filtroPredeterminado && FechaDesde.HasValue)
                resultado = resultado.Where(o => o.FechaEmision.Date >= FechaDesde.Value.Date);
            if (!_filtroPredeterminado && FechaHasta.HasValue)
                resultado = resultado.Where(o => o.FechaEmision.Date <= FechaHasta.Value.Date);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                resultado = resultado.Where(o =>
                    Contiene(o.NumeroOci, texto) ||
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
            _fechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _fechaHasta = DateTime.Today;
            _filtroPredeterminado = true;
            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(EstadoFiltro));
            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));
            AplicarFiltros();
        }

        private bool CoincideRango(DateTime fecha) => (!_fechaDesde.HasValue || fecha.Date >= _fechaDesde.Value.Date) && (!_fechaHasta.HasValue || fecha.Date <= _fechaHasta.Value.Date);
        private static bool EsOciActiva(string estado) => estado.Trim().ToUpperInvariant() is not ("ENTREGADO" or "ENTREGADA" or "ANULADO" or "ANULADA");

        private void Ver(object? parametro)
        {
            if (parametro is not OrdenCompraInterna fila) return;
            try
            {
                OrdenCompraInterna? orden = _negocio.Obtener(fila.IdOrdenCompraInterna);
                if (orden == null)
                {
                    NotificationService.Warning("No se encontró la orden seleccionada.");
                    return;
                }

                new OrdenCompraInternaDetalleWindow(orden) { Owner = Application.Current.MainWindow }.ShowDialog();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo abrir el detalle de la OCI: {ex.Message}");
            }
        }

        private void Imprimir(object? parametro)
        {
            OrdenCompraInterna? orden = ObtenerOrdenCompleta(parametro);
            if (orden == null) return;

            Empresa? empresa = _empresaNegocio.ObtenerPredeterminada();
            if (empresa == null)
            {
                NotificationService.Warning("Debe registrar una empresa predeterminada antes de imprimir.");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Guardar orden de compra",
                FileName = $"OrdenCompra_{orden.NumeroOci}.pdf",
                Filter = "PDF|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                ProformaPdfExporter.Exportar(dialog.FileName, empresa, CrearDocumentoPdf(orden));
                NotificationService.Success("Orden de compra generada correctamente.");
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo generar la orden de compra: {ex.Message}");
            }
        }

        private void Copiar(object? parametro)
        {
            OrdenCompraInterna? orden = ObtenerOrdenCompleta(parametro);
            if (orden == null) return;
            AbrirEditorOrden(orden, copiar: true);
        }

        private static bool PuedeEditar(object? parametro) =>
            parametro is OrdenCompraInterna orden
            && orden.Estado.Trim().ToUpperInvariant() is "PENDIENTE" or "EMITIDA" or "EMITIDO"
            && !orden.TieneOrdenTrabajo
            && !orden.TieneGuiaSalida
            && string.IsNullOrWhiteSpace(orden.MotivoAnulacion)
            && !orden.FechaAnulacion.HasValue;

        private void Editar(object? parametro)
        {
            OrdenCompraInterna? orden = ObtenerOrdenCompleta(parametro);
            if (orden == null) return;

            if (!orden.PuedeEditar)
            {
                NotificationService.Warning("Solo se puede editar una OC pendiente sin OT, guias, anulacion ni acciones realizadas.");
                return;
            }

            AbrirEditorOrden(orden, copiar: false);
        }

        private static bool PuedeGenerarOt(object? parametro) =>
            PermissionService.PuedeGenerarOrdenTrabajo
            && parametro is OrdenCompraInterna orden
            && orden.PuedeGenerarOt;

        private void GenerarOt(object? parametro)
        {
            if (!PermissionService.PuedeGenerarOrdenTrabajo)
            {
                PermissionService.MostrarSinPermiso();
                return;
            }

            if (parametro is not OrdenCompraInterna orden
                || !_negocio.RequiereOrdenTrabajo(orden.IdOrdenCompraInterna))
            {
                NotificationService.Warning("La OCI ya no requiere una orden de trabajo. Actualice la lista.");
                Cargar();
                return;
            }

            OrdenCompraInterna? completa = _negocio.Obtener(orden.IdOrdenCompraInterna);
            if (completa == null || completa.Detalles.Count == 0)
            {
                NotificationService.Warning("No se encontraron productos pendientes para planificar la OT.");
                return;
            }

            OrdenTrabajoNegocio otNegocio = new();
            List<OrdenTrabajoValidacionProducto> validacion = otNegocio.ValidarInsumos(completa.IdOrdenCompraInterna);
            if (validacion.Count == 0)
            {
                NotificationService.Warning("La OCI no tiene productos pendientes para generar una OT.");
                return;
            }

            ValidacionInsumosWindow ventana = new(completa, validacion)
            {
                Owner = Application.Current.MainWindow
            };
            if (ventana.ShowDialog() != true) return;

            OrdenTrabajoCrearWindow crear = new(completa, validacion)
            {
                Owner = Application.Current.MainWindow
            };
            if (crear.ShowDialog() == true) Cargar();
        }

        private static bool PuedeGenerarGuiaSalida(object? parametro) =>
            PermissionService.PuedeGenerarGuiaInterna
            && parametro is OrdenCompraInterna orden
            && orden.PuedeGenerarGuiaSalida;

        private void GenerarGuiaSalida(object? parametro)
        {
            if (!PermissionService.PuedeGenerarGuiaInterna)
            {
                PermissionService.MostrarSinPermiso();
                return;
            }

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
                FileName = $"OrdenesCompra_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Filter = "Archivo CSV para Excel (*.csv)|*.csv",
                DefaultExt = ".csv"
            };
            if (dialog.ShowDialog() != true) return;

            StringBuilder csv = new();
            csv.AppendLine("Orden;Orden compra cliente;Estado;Cliente;Fecha emisión;Subtotal;Descuento;IGV;Total");
            foreach (OrdenCompraInterna orden in Ordenes)
            {
                csv.AppendLine(string.Join(";", Escapar(orden.NumeroOci), Escapar(orden.OrdenCompraCliente),
                    Escapar(orden.Estado), Escapar(orden.NombreCliente),
                    orden.FechaEmision.ToString("dd/MM/yyyy"), orden.Subtotal.ToString("0.00"),
                    orden.Descuento.ToString("0.00"), orden.Igv.ToString("0.00"), orden.Total.ToString("0.00")));
            }
            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
            NotificationService.Success($"Se exportaron {Ordenes.Count} órdenes correctamente.");
        }

        private static bool Contiene(string valor, string texto) =>
            valor.Contains(texto, StringComparison.OrdinalIgnoreCase);

        private static string Escapar(string valor) => $"\"{valor.Replace("\"", "\"\"")}\"";

        private OrdenCompraInterna? ObtenerOrdenCompleta(object? parametro)
        {
            if (parametro is not OrdenCompraInterna fila)
            {
                NotificationService.Warning("Debe seleccionar una orden de compra.");
                return null;
            }

            OrdenCompraInterna? orden = _negocio.Obtener(fila.IdOrdenCompraInterna);
            if (orden == null)
                NotificationService.Warning("No se encontro la orden de compra.");
            return orden;
        }

        private void AbrirEditorOrden(OrdenCompraInterna orden, bool copiar)
        {
            ProformaEditorViewModel viewModel = new(orden, copiar);
            ProformaEditorWindow ventana = new()
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            viewModel.CerrarVentana = ventana.Close;
            ventana.ShowDialog();

            if (viewModel.Guardado)
                Cargar();
        }

        private static Proforma CrearDocumentoPdf(OrdenCompraInterna orden) => new()
        {
            SerieNumero = orden.NumeroOci,
            FechaEmision = orden.FechaEmision,
            FechaVencimiento = orden.FechaEmision,
            OrdenCompraCliente = orden.OrdenCompraCliente,
            NombreCliente = orden.NombreCliente,
            UsuarioGenerador = orden.UsuarioGenerador,
            Subtotal = orden.Subtotal,
            Descuento = orden.Descuento,
            Igv = orden.Igv,
            IgvPorcentaje = orden.IgvPorcentaje,
            CondicionTributaria = orden.CondicionTributaria,
            Total = orden.Total,
            Estado = orden.Estado,
            Observacion = string.Empty,
            Detalles = orden.Detalles.Select(d => new ProformaDetalle
            {
                IdProducto = d.IdProducto,
                CodigoProducto = d.CodigoProducto,
                NombreProducto = d.NombreProducto,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Descuento = d.Descuento,
                Importe = d.Importe,
                Observacion = d.Observacion
            }).ToList()
        };
    }
}
