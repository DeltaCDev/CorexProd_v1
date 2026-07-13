using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Produccion.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class OrdenCompraInternaDetalleWindow : Window
    {
        private readonly OrdenCompraInterna _orden;
        private readonly EmpresaNegocio _empresaNegocio = new();
        private readonly OrdenTrabajoNegocio _ordenTrabajoNegocio = new();
        private readonly GuiaInternaNegocio _guiaInternaNegocio = new();
        private readonly DispatcherTimer _tiempoTimer = new() { Interval = TimeSpan.FromMinutes(1) };

        public OrdenCompraInternaDetalleWindow(OrdenCompraInterna orden)
        {
            _orden = orden;
            InitializeComponent();

            Title = "Detalle Orden de Compra";
            DataContext = orden;
            Loaded += OrdenCompraInternaDetalleWindow_Loaded;
            Closed += (_, _) => _tiempoTimer.Stop();
            _tiempoTimer.Tick += (_, _) => ActualizarIndicadoresTiempo();
        }

        private void OrdenCompraInternaDetalleWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OrdenCompraInternaDetalleWindow_Loaded;
            ConfigurarCodigosProducto(this);
            CargarOrdenesTrabajoAsociadas();
            CargarGuiasInternasAsociadas();
            ActualizarIndicadoresTiempo();
            _tiempoTimer.Start();
        }

        private void CargarOrdenesTrabajoAsociadas()
        {
            try
            {
                List<OtAsociadaItem> items = _ordenTrabajoNegocio
                    .Listar()
                    .Where(x => x.IdOrdenCompraInterna == _orden.IdOrdenCompraInterna)
                    .OrderByDescending(x => x.FechaEmision)
                    .Select(CrearOtAsociadaItem)
                    .ToList();

                OrdenesTrabajoItems.ItemsSource = items;
                decimal unidadesActivas = items
                    .Where(x => x.Estado is "Pendiente" or "En Proceso" or "Terminado Parcial")
                    .Sum(x => x.Unidades);

                ResumenOtText.Text = items.Count == 0
                    ? "Sin OT relacionadas"
                    : $"{items.Count} OT relacionadas · {FormatearCantidad(unidadesActivas)} unidades actualmente en elaboración";
            }
            catch (Exception ex)
            {
                OrdenesTrabajoItems.ItemsSource = Array.Empty<OtAsociadaItem>();
                ResumenOtText.Text = "No se pudieron cargar las OT relacionadas";
                NotificationService.Warning($"No se pudieron cargar las OT asociadas: {ex.Message}");
            }
        }

        private static OtAsociadaItem CrearOtAsociadaItem(OrdenTrabajo ot)
        {
            OrdenTrabajoDetalleArea? areaActual = ot.Areas
                .Where(x => x.CantidadPendiente > 0 && x.Estado is not ("FINALIZADA" or "ANULADA"))
                .OrderBy(x => x.OrdenSecuencia)
                .FirstOrDefault();

            string estado = ot.EstadoOperativo;
            string areaTexto = estado switch
            {
                "Pendiente" => areaActual == null ? "Pendiente de iniciar" : $"Próxima área: {areaActual.NombreArea}",
                "En Proceso" => areaActual == null ? "Producción en proceso" : $"Área actual: {areaActual.NombreArea}",
                "Terminado Parcial" => areaActual == null ? "Producción parcial" : $"Área actual: {areaActual.NombreArea}",
                "Terminado" => "Producción terminada",
                "Anulado" => "Orden anulada",
                _ => areaActual?.NombreArea ?? "Sin área registrada"
            };

            (string fondo, string color) = estado switch
            {
                "Pendiente" => ("#FFF1D6", "#C45A08"),
                "En Proceso" => ("#DBEAFE", "#1D4ED8"),
                "Terminado Parcial" => ("#FFEDD5", "#C2410C"),
                "Terminado" => ("#DCFCE7", "#166534"),
                "Anulado" => ("#FEE2E2", "#B91C1C"),
                _ => ("#F1F5F9", "#475569")
            };

            decimal unidades = estado is "Pendiente" or "En Proceso"
                ? Math.Max(ot.TotalPendiente, ot.TotalPlanificado)
                : Math.Max(ot.TotalPendiente, ot.TotalProducido);

            return new OtAsociadaItem
            {
                IdOrdenTrabajo = ot.IdOrdenTrabajo,
                NumeroOT = ot.NumeroOT,
                Estado = estado,
                EstadoFondo = fondo,
                EstadoColor = color,
                Unidades = unidades,
                UnidadesTexto = $"{FormatearCantidad(unidades)} Und",
                AreaTexto = areaTexto
            };
        }

        private void AbrirOrdenTrabajo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton || !int.TryParse(boton.Tag?.ToString(), out int idOrdenTrabajo) || idOrdenTrabajo <= 0)
                return;

            OrdenTrabajoDetalleWindow ventana = new(idOrdenTrabajo) { Owner = this };
            ventana.ShowDialog();
            CargarOrdenesTrabajoAsociadas();
        }

        private void CargarGuiasInternasAsociadas()
        {
            try
            {
                List<GuiaInternaAsociadaItem> items = _guiaInternaNegocio
                    .Listar(null, null, null, "Todos", "Todos", string.Empty)
                    .Where(x => x.IdOrdenCompraInterna == _orden.IdOrdenCompraInterna)
                    .Where(x => !string.Equals(x.Estado, "Borrador", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.FechaEmision)
                    .Select(CrearGuiaInternaAsociadaItem)
                    .ToList();

                GuiasInternasItems.ItemsSource = items;
                ResumenGuiaText.Text = items.Count == 0
                    ? "Sin guías emitidas"
                    : $"{items.Count} guía{(items.Count == 1 ? string.Empty : "s")} emitida{(items.Count == 1 ? string.Empty : "s")} por esta OC";
            }
            catch (Exception ex)
            {
                GuiasInternasItems.ItemsSource = Array.Empty<GuiaInternaAsociadaItem>();
                ResumenGuiaText.Text = "No se pudieron cargar las guías emitidas";
                NotificationService.Warning($"No se pudieron cargar las guías internas asociadas: {ex.Message}");
            }
        }

        private static GuiaInternaAsociadaItem CrearGuiaInternaAsociadaItem(GuiaInterna guia)
        {
            string estado = string.IsNullOrWhiteSpace(guia.Estado) ? "Emitida" : guia.Estado.Trim();
            (string fondo, string color) = estado.ToUpperInvariant() switch
            {
                "ANULADA" or "ANULADO" => ("#FEE2E2", "#B91C1C"),
                "EMITIDA" or "ENTREGADA" or "ENTREGADO" => ("#D1FAE5", "#047857"),
                _ => ("#F1F5F9", "#475569")
            };

            string almacen = string.IsNullOrWhiteSpace(guia.NombreAlmacen) ? "Sin almacén" : guia.NombreAlmacen.Trim();
            string emisor = string.IsNullOrWhiteSpace(guia.UsuarioEmisor) ? "Sin emisor" : guia.UsuarioEmisor.Trim();

            return new GuiaInternaAsociadaItem
            {
                IdGuiaInterna = guia.IdGuiaInterna,
                NumeroGuia = guia.NumeroGuia,
                Estado = estado,
                EstadoFondo = fondo,
                EstadoColor = color,
                FechaTexto = guia.FechaEmision.ToString("dd/MM/yyyy"),
                AlmacenEmisorTexto = $"{almacen} · {emisor}"
            };
        }

        private void AbrirGuiaInterna_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton || !int.TryParse(boton.Tag?.ToString(), out int idGuiaInterna) || idGuiaInterna <= 0)
                return;

            GuiaInterna? guia = _guiaInternaNegocio.Obtener(idGuiaInterna);
            if (guia == null)
            {
                NotificationService.Warning("No se pudo cargar el detalle de la guía interna.");
                return;
            }

            new GuiaInternaDetalleWindow(guia) { Owner = this }.ShowDialog();
            CargarGuiasInternasAsociadas();
        }

        private void ActualizarIndicadoresTiempo()
        {
            DateTime inicio = _orden.FechaRegistro != default ? _orden.FechaRegistro : _orden.FechaEmision;
            DateTime ahora = DateTime.Now;
            string estado = (_orden.Estado ?? string.Empty).Trim().ToUpperInvariant();
            bool entregada = estado is "ENTREGADO" or "ENTREGADA" or "DESPACHADO" or "DESPACHADA";
            bool anulada = estado is "ANULADO" or "ANULADA";
            bool cerrada = entregada || anulada;
            DateTime fin = cerrada && _orden.FechaAnulacion.HasValue ? _orden.FechaAnulacion.Value : ahora;
            TimeSpan transcurrido = fin > inicio ? fin - inicio : TimeSpan.Zero;

            FechaEmisionHoraText.Text = $"F. Emisión: {inicio:dd/MM/yyyy HH:mm}";
            FechaCierreText.Text = cerrada ? $"F. Cierre: {fin:dd/MM/yyyy HH:mm}" : "F. Cierre: En proceso";
            TiempoTranscurridoText.Text = FormatearDuracion(transcurrido);
            ConfigurarEstadoBadge(estado);

            if (_orden.FechaEntrega == default)
            {
                FechaEntregaCabeceraText.Text = "No registrada";
                FechaEntregaEstadoText.Text = entregada ? "Entregado sin fecha planificada" : "Sin fecha de entrega";
                AplicarEstadoEntrega("#F8FAFC", "#CBD5E1", "#475569");
                return;
            }

            FechaEntregaCabeceraText.Text = _orden.FechaEntrega.ToString("dd/MM/yyyy");
            if (entregada)
            {
                int diferenciaCierre = (fin.Date - _orden.FechaEntrega.Date).Days;
                if (diferenciaCierre < 0)
                {
                    int diasAntes = Math.Abs(diferenciaCierre);
                    FechaEntregaEstadoText.Text = diasAntes == 1 ? "Entregado 1 dia antes" : $"Entregado {diasAntes} dias antes";
                    AplicarEstadoEntrega("#ECFDF5", "#34D399", "#047857");
                }
                else if (diferenciaCierre == 0)
                {
                    FechaEntregaEstadoText.Text = "Entregado a tiempo";
                    AplicarEstadoEntrega("#ECFDF5", "#34D399", "#047857");
                }
                else
                {
                    FechaEntregaEstadoText.Text = diferenciaCierre == 1 ? "Entregado 1 dia tarde" : $"Entregado {diferenciaCierre} dias tarde";
                    AplicarEstadoEntrega("#FEE2E2", "#F87171", "#B91C1C");
                }
                return;
            }

            if (anulada)
            {
                FechaEntregaEstadoText.Text = "Orden anulada";
                AplicarEstadoEntrega("#F8FAFC", "#CBD5E1", "#475569");
                return;
            }

            int dias = (_orden.FechaEntrega.Date - ahora.Date).Days;
            string fondo;
            string borde;
            string color;
            string texto;

            if (dias < 0)
            {
                texto = $"Vencido hace {Math.Abs(dias)} {(Math.Abs(dias) == 1 ? "día" : "días")}";
                fondo = "#FEE2E2";
                borde = "#F87171";
                color = "#B91C1C";
            }
            else if (dias == 0)
            {
                texto = "Vence hoy";
                fondo = "#FEF3C7";
                borde = "#F59E0B";
                color = "#B45309";
            }
            else
            {
                texto = $"Vence en {dias} {(dias == 1 ? "día" : "días")}";
                fondo = dias <= 2 ? "#FFF1F2" : "#ECFDF5";
                borde = dias <= 2 ? "#FCA5A5" : "#6EE7B7";
                color = dias <= 2 ? "#BE123C" : "#047857";
            }

            FechaEntregaEstadoText.Text = texto;
            AplicarEstadoEntrega(fondo, borde, color);
        }

        private void AplicarEstadoEntrega(string fondo, string borde, string color)
        {
            SolidColorBrush texto = new((Color)ColorConverter.ConvertFromString(color));
            FechaEntregaPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fondo));
            FechaEntregaPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borde));
            FechaEntregaCabeceraText.Foreground = texto;
            FechaEntregaEstadoText.Foreground = texto;
        }

        private void ConfigurarEstadoBadge(string estado)
        {
            (string fondo, string borde, string color) = estado switch
            {
                "ENTREGADO" or "ENTREGADA" or "DESPACHADO" or "DESPACHADA" => ("#DBEAFE", "#60A5FA", "#1D4ED8"),
                "ANULADO" or "ANULADA" => ("#FEE2E2", "#FCA5A5", "#B91C1C"),
                "PROCESO" or "EN PROCESO" or "PARCIAL" => ("#FEF3C7", "#F59E0B", "#B45309"),
                _ => ("#DBEAFE", "#60A5FA", "#1D4ED8")
            };

            EstadoPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fondo));
            EstadoPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borde));
            EstadoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private static string FormatearDuracion(TimeSpan tiempo)
        {
            int dias = Math.Max(0, (int)tiempo.TotalDays);
            int horas = Math.Max(0, tiempo.Hours);
            int minutos = Math.Max(0, tiempo.Minutes);

            if (dias > 0)
                return $"{dias} {(dias == 1 ? "día" : "días")} {horas} h {minutos} min";
            if (horas > 0)
                return $"{horas} h {minutos} min";
            return $"{minutos} min";
        }

        private static string FormatearCantidad(decimal valor) =>
            decimal.Truncate(valor) == valor ? valor.ToString("N0") : valor.ToString("N2");

        private void ConfigurarCodigosProducto(DependencyObject origen)
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);

                if (hijo is TextBlock texto
                    && !string.IsNullOrWhiteSpace(texto.Text)
                    && _orden.Detalles.Any(detalle =>
                        string.Equals(detalle.CodigoProducto, texto.Text, StringComparison.OrdinalIgnoreCase)))
                {
                    texto.FontFamily = new FontFamily("Consolas");
                    texto.FontSize = 11;
                    texto.FontWeight = FontWeights.SemiBold;
                    TextOptions.SetTextFormattingMode(texto, TextFormattingMode.Display);
                    TextOptions.SetTextRenderingMode(texto, TextRenderingMode.ClearType);

                    if (VisualTreeHelper.GetParent(texto) is Border contenedorCodigo)
                    {
                        contenedorCodigo.MinWidth = 80;
                        contenedorCodigo.Padding = new Thickness(10, 4, 10, 4);
                    }
                }

                ConfigurarCodigosProducto(hijo);
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();

        private void DescargarPdf_Click(object sender, RoutedEventArgs e)
        {
            Empresa? empresa = ObtenerEmpresa();
            if (empresa == null)
                return;

            SaveFileDialog dialogo = new()
            {
                Title = "Guardar orden de compra",
                FileName = CrearNombreArchivo(),
                Filter = "Documento PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true
            };

            if (dialogo.ShowDialog(this) != true)
                return;

            try
            {
                ExportarPdf(dialogo.FileName, empresa);
                NotificationService.Success("Orden de compra generada correctamente.");
                Process.Start(new ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo generar la orden de compra: {ex.Message}");
            }
        }

        private void Compartir_Click(object sender, RoutedEventArgs e)
        {
            Empresa? empresa = ObtenerEmpresa();
            if (empresa == null)
                return;

            try
            {
                string carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "CorexProd",
                    "Compartir");
                Directory.CreateDirectory(carpeta);

                string ruta = Path.Combine(carpeta, CrearNombreArchivo());
                ExportarPdf(ruta, empresa);
                Clipboard.SetText(ruta);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{ruta}\"",
                    UseShellExecute = true
                });

                NotificationService.Success("PDF preparado para compartir. La ubicación del archivo también fue copiada.");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo preparar el archivo para compartir: {ex.Message}");
            }
        }

        private Empresa? ObtenerEmpresa()
        {
            Empresa? empresa = _empresaNegocio.ObtenerPredeterminada();
            if (empresa == null)
                NotificationService.Warning("Debe registrar una empresa predeterminada antes de generar el PDF.");
            return empresa;
        }

        private void ExportarPdf(string ruta, Empresa empresa) =>
            ProformaPdfExporter.Exportar(ruta, empresa, CrearDocumentoPdf(_orden));

        private string CrearNombreArchivo()
        {
            string numero = string.IsNullOrWhiteSpace(_orden.OrdenCompraCliente)
                ? _orden.NumeroOci
                : _orden.OrdenCompraCliente;

            foreach (char caracter in Path.GetInvalidFileNameChars())
                numero = numero.Replace(caracter, '-');

            return $"OrdenCompra_{numero}.pdf";
        }

        private static Proforma CrearDocumentoPdf(OrdenCompraInterna orden) => new()
        {
            SerieNumero = orden.NumeroOci,
            FechaEmision = orden.FechaEmision,
            FechaVencimiento = orden.FechaEntrega == default ? orden.FechaEmision : orden.FechaEntrega,
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
            Detalles = orden.Detalles.Select(detalle => new ProformaDetalle
            {
                IdProducto = detalle.IdProducto,
                CodigoProducto = detalle.CodigoProducto,
                NombreProducto = detalle.NombreProducto,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Descuento = detalle.Descuento,
                Importe = detalle.Importe,
                Observacion = detalle.Observacion
            }).ToList()
        };

        private sealed class OtAsociadaItem
        {
            public int IdOrdenTrabajo { get; init; }
            public string NumeroOT { get; init; } = string.Empty;
            public string Estado { get; init; } = string.Empty;
            public string EstadoFondo { get; init; } = "#F1F5F9";
            public string EstadoColor { get; init; } = "#475569";
            public decimal Unidades { get; init; }
            public string UnidadesTexto { get; init; } = string.Empty;
            public string AreaTexto { get; init; } = string.Empty;
        }

        private sealed class GuiaInternaAsociadaItem
        {
            public int IdGuiaInterna { get; init; }
            public string NumeroGuia { get; init; } = string.Empty;
            public string Estado { get; init; } = string.Empty;
            public string EstadoFondo { get; init; } = "#F1F5F9";
            public string EstadoColor { get; init; } = "#475569";
            public string FechaTexto { get; init; } = string.Empty;
            public string AlmacenEmisorTexto { get; init; } = string.Empty;
        }
    }
}
