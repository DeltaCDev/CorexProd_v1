using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ProduccionView : UserControl
    {
        private readonly OrdenTrabajoNegocio _negocio = new();
        private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(15) };
        private List<OrdenTrabajo> _ordenes = [];
        private readonly UsuarioNegocio _usuariosNegocio = new();
        private bool _inicializandoFiltros;
        private bool _filtroPredeterminado = true;

        public ProduccionView()
        {
            InitializeComponent();
            InicializarFiltros();
            _refreshTimer.Tick += (_, _) => Cargar(silencioso: true);
            Loaded += (_, _) => _refreshTimer.Start();
            Unloaded += (_, _) => _refreshTimer.Stop();
            Cargar();
        }

        private void ConfigurarOrdenColumnas()
        {
            if (OrdenesGrid.Columns.Count < 10)
                return;

            int[] ordenColumnas = [0, 1, 3, 4, 6, 5, 2, 7, 8, 9];
            for (int posicion = 0; posicion < ordenColumnas.Length; posicion++)
                OrdenesGrid.Columns[ordenColumnas[posicion]].DisplayIndex = posicion;
        }

        private void AgregarBotonImprimir()
        {
            DataTemplate plantilla = new();
            FrameworkElementFactory boton = new(typeof(Button));
            boton.SetValue(Button.ContentProperty, "Imprimir");
            boton.SetValue(Button.ToolTipProperty, "Imprimir Orden de Trabajo");
            boton.SetValue(Button.HeightProperty, 34.0);
            boton.SetValue(Button.MinWidthProperty, 82.0);
            boton.SetValue(Button.PaddingProperty, new Thickness(10, 0, 10, 0));
            boton.SetValue(Button.ForegroundProperty, Brushes.White);
            boton.SetValue(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(37, 99, 235)));
            boton.SetValue(Button.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(29, 78, 216)));
            boton.AddHandler(Button.ClickEvent, new RoutedEventHandler(Imprimir_Click));
            plantilla.VisualTree = boton;
            OrdenesGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Imprimir",
                Width = 100,
                CellTemplate = plantilla
            });
        }

        private void AgregarBotonAprobacion()
        {
            DataTemplate plantilla = new();
            FrameworkElementFactory boton = new(typeof(Button));
            boton.SetValue(Button.ContentProperty, "Aprobar"); boton.SetValue(Button.ToolTipProperty, "Visualizar y aprobar OT");
            boton.SetValue(Button.HeightProperty, 34.0); boton.SetValue(Button.MinWidthProperty, 82.0); boton.SetValue(Button.PaddingProperty, new Thickness(10, 0, 10, 0));
            boton.SetValue(Button.ForegroundProperty, Brushes.White); boton.SetValue(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(22, 163, 74))); boton.SetValue(Button.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(21, 128, 61)));
            boton.AddHandler(Button.ClickEvent, new RoutedEventHandler(Aprobar_Click)); plantilla.VisualTree = boton;
            OrdenesGrid.Columns.Add(new DataGridTemplateColumn { Header = "Aprobación", Width = 100, CellTemplate = plantilla });
        }

        private void AgregarColumnaEstadoAprobacion()
        {
            DataTemplate plantilla = new();
            FrameworkElementFactory panel = new(typeof(StackPanel));
            panel.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);
            panel.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            FrameworkElementFactory estado = new(typeof(TextBlock));
            estado.SetBinding(TextBlock.TextProperty, new Binding(nameof(OrdenTrabajo.EstadoAprobacionTexto)));
            estado.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            estado.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(180, 83, 9)));
            estado.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            panel.AppendChild(estado);

            plantilla.VisualTree = panel;
            OrdenesGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Estado",
                Width = 125,
                CellTemplate = plantilla
            });
        }

        private void ColocarColumnasDeControlDespuesDeAcciones()
        {
            DataGridColumn? imprimir = OrdenesGrid.Columns.FirstOrDefault(x => string.Equals(x.Header?.ToString(), "Imprimir", StringComparison.OrdinalIgnoreCase));
            DataGridColumn? aprobacion = OrdenesGrid.Columns.FirstOrDefault(x => string.Equals(x.Header?.ToString(), "Aprobación", StringComparison.OrdinalIgnoreCase));
            DataGridColumn? estado = OrdenesGrid.Columns.FirstOrDefault(x => string.Equals(x.Header?.ToString(), "Estado", StringComparison.OrdinalIgnoreCase));
            if (imprimir == null || aprobacion == null || estado == null)
                return;

            imprimir.DisplayIndex = 1;
            aprobacion.DisplayIndex = 2;
            estado.DisplayIndex = 3;
        }

        private void Cargar(bool silencioso = false)
        {
            try
            {
                int? seleccion = (OrdenesGrid.SelectedItem as OrdenTrabajo)?.IdOrdenTrabajo;
                _ordenes = _negocio.Listar();
                string aprobadores = ObtenerAprobadoresTexto();
                foreach (OrdenTrabajo orden in _ordenes)
                    orden.AprobadoresGerenciaTexto = aprobadores;
                AplicarFiltros(seleccion);

            }
            catch (Exception ex)
            {
                if (!silencioso)
                    NotificationService.Error($"No se pudieron cargar las OT: {ex.Message}");
            }
        }

        private void InicializarFiltros()
        {
            _inicializandoFiltros = true;
            EstadoComboBox.ItemsSource = new[] { "Todos", "Pendiente", "En Proceso", "Terminado", "Terminado Parcial", "Anulado" };
            EstadoComboBox.SelectedIndex = 0;
            EstadoAprobacionComboBox.ItemsSource = new[] { "Todos", "Por aprobar", "Aprobada" };
            EstadoAprobacionComboBox.SelectedIndex = 0;
            TipoOtComboBox.ItemsSource = new[] { "Todos", "OCI", "Manual" };
            TipoOtComboBox.SelectedIndex = 0;
            FechaDesdePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FechaHastaPicker.SelectedDate = DateTime.Today;
            _inicializandoFiltros = false;
        }

        private void AplicarFiltros(int? seleccion = null)
        {
            IEnumerable<OrdenTrabajo> consulta = _ordenes;
            string texto = BuscarTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(texto))
            {
                consulta = consulta.Where(x =>
                    x.NumeroOT.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.NumeroOci.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.TipoOTDescripcion.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.NumeroOTRelacionada.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.OrdenCompraCliente.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || x.NombreCliente.Contains(texto, StringComparison.OrdinalIgnoreCase));
            }

            string cliente = BuscarClienteTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(cliente))
                consulta = consulta.Where(x => x.NombreCliente.Contains(cliente, StringComparison.OrdinalIgnoreCase));
            string oci = BuscarOciTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(oci))
                consulta = consulta.Where(x => x.NumeroOci.Contains(oci, StringComparison.OrdinalIgnoreCase));
            string ordenCliente = BuscarOrdenClienteTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(ordenCliente))
                consulta = consulta.Where(x => x.OrdenCompraCliente.Contains(ordenCliente, StringComparison.OrdinalIgnoreCase));

            string estado = EstadoComboBox.SelectedItem?.ToString() ?? "Todos";
            if (!estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                consulta = consulta.Where(x => CoincideEstado(x.EstadoOperativo, estado));
            string aprobacion = EstadoAprobacionComboBox.SelectedItem?.ToString() ?? "Todos";
            if (!aprobacion.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                consulta = consulta.Where(x => aprobacion.Equals("Aprobada", StringComparison.OrdinalIgnoreCase) ? x.EstaAprobada : x.PuedeAprobarGerencia);
            string tipo = TipoOtComboBox.SelectedItem?.ToString() ?? "Todos";
            if (!tipo.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                consulta = consulta.Where(x => x.TipoOTDescripcion.Equals(tipo, StringComparison.OrdinalIgnoreCase));
            DateTime? desde = FechaDesdePicker.SelectedDate?.Date;
            DateTime? hasta = FechaHastaPicker.SelectedDate?.Date;
            if (_filtroPredeterminado)
                consulta = consulta.Where(x => EsOtActiva(x.EstadoOperativo) || CoincideRango(x.FechaEmision, desde, hasta));
            else
            {
                if (desde.HasValue) consulta = consulta.Where(x => x.FechaEmision.Date >= desde.Value);
                if (hasta.HasValue) consulta = consulta.Where(x => x.FechaEmision.Date <= hasta.Value);
            }

            List<OrdenTrabajo> visibles = consulta.ToList();
            OrdenesGrid.ItemsSource = visibles;
            if (seleccion.HasValue)
                OrdenesGrid.SelectedItem = visibles.FirstOrDefault(x => x.IdOrdenTrabajo == seleccion.Value);
            TotalOtText.Text = visibles.Count.ToString("N0");
            PorAprobarText.Text = visibles.Count(x => x.PuedeAprobarGerencia).ToString("N0");
            PendientesProduccionText.Text = visibles.Count(x => x.EstadoOperativo.Equals("Pendiente", StringComparison.OrdinalIgnoreCase)).ToString("N0");
            EnProcesoText.Text = visibles.Count(x => x.EstadoOperativo.Equals("En Proceso", StringComparison.OrdinalIgnoreCase)).ToString("N0");
            TerminadasText.Text = visibles.Count(x => x.EstadoOperativo is "Terminado" or "Terminado Parcial").ToString("N0");
            AnuladasText.Text = visibles.Count(x => x.EstadoOperativo.Equals("Anulado", StringComparison.OrdinalIgnoreCase)).ToString("N0");
        }

        private static bool EsOtActiva(string estado) => estado.Trim().ToUpperInvariant() is "PENDIENTE" or "EN PROCESO" or "EN_PROCESO";
        private static bool CoincideRango(DateTime fecha, DateTime? desde, DateTime? hasta) =>
            (!desde.HasValue || fecha.Date >= desde.Value) && (!hasta.HasValue || fecha.Date <= hasta.Value);

        private static bool CoincideEstado(string valor, string filtro)
        {
            string estado = valor.Trim().ToUpperInvariant();
            return filtro switch
            {
                "Pendiente" => estado is "PENDIENTE" or "EMITIDA",
                "En Proceso" => estado is "EN PROCESO" or "EN_PROCESO" or "PROCESO",
                "Terminado" => estado is "TERMINADO" or "TERMINADA" or "FINALIZADA",
                "Terminado Parcial" => estado is "TERMINADO PARCIAL",
                "Anulado" => estado is "ANULADO" or "ANULADA",
                _ => true
            };
        }

        private void Filtro_Changed(object sender, RoutedEventArgs e)
        {
            if (!_inicializandoFiltros)
            {
                _filtroPredeterminado = false;
                AplicarFiltros((OrdenesGrid.SelectedItem as OrdenTrabajo)?.IdOrdenTrabajo);
            }
        }

        private void LimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _inicializandoFiltros = true;
            BuscarTextBox.Clear();
            BuscarClienteTextBox.Clear();
            BuscarOciTextBox.Clear();
            BuscarOrdenClienteTextBox.Clear();
            EstadoComboBox.SelectedIndex = 0;
            EstadoAprobacionComboBox.SelectedIndex = 0;
            TipoOtComboBox.SelectedIndex = 0;
            FechaDesdePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FechaHastaPicker.SelectedDate = DateTime.Today;
            _filtroPredeterminado = true;
            _inicializandoFiltros = false;
            AplicarFiltros();
        }

        private void Actualizar_Click(object sender, RoutedEventArgs e) => Cargar();
        private void Acciones_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement elemento || elemento.DataContext is not OrdenTrabajo ot)
                return;

            ContextMenu menu = new() { PlacementTarget = elemento, StaysOpen = false, Padding = new Thickness(4) };
            menu.Items.Add(MenuAccion("Detalle de OT", ot, (_, _) => AbrirDetalle(ot)));
            menu.Items.Add(MenuAccion("Producción", ot, (_, _) => AbrirProduccion(ot)));
            menu.Items.Add(MenuAccion("Kardex", ot, (_, _) => Kardex_Click(new Button { DataContext = ot }, new RoutedEventArgs())));
            menu.Items.Add(MenuAccion("Historial", ot, (_, _) => Historial_Click(new Button { DataContext = ot }, new RoutedEventArgs())));
            menu.Items.Add(MenuAccion("Imprimir OT", ot, (_, _) => Imprimir_Click(new Button { DataContext = ot }, new RoutedEventArgs())));
            menu.Items.Add(new Separator());
            menu.Items.Add(MenuAccion("Anular OT", ot, (_, _) => Anular_Click(new Button { DataContext = ot }, new RoutedEventArgs()), true));
            menu.IsOpen = true;
        }

        private static MenuItem MenuAccion(string texto, OrdenTrabajo ot, RoutedEventHandler accion, bool peligro = false)
        {
            MenuItem item = new() { Header = texto, Tag = ot, Padding = new Thickness(12, 7, 24, 7), Foreground = peligro ? new SolidColorBrush(Color.FromRgb(185, 28, 28)) : new SolidColorBrush(Color.FromRgb(15, 23, 42)) };
            item.Click += accion;
            return item;
        }

        private void AbrirDetalle(OrdenTrabajo ot)
        {
            AbrirVentana(() => new OrdenTrabajoDetalleWindow(ot.IdOrdenTrabajo) { Owner = Application.Current.MainWindow, Title = $"Detalle {ot.NumeroOT}" });
        }

        private void AbrirProduccion(OrdenTrabajo ot)
        {
            if (!ot.EstaAprobada)
            {
                NotificationService.Warning("ORDEN PENDIENTE DE APROBACIÓN. Esta OT todavía no ha sido aprobada por Gerencia.");
                return;
            }
            AbrirVentana(() => new OrdenTrabajoDetalleWindow(ot.IdOrdenTrabajo) { Owner = Application.Current.MainWindow, Title = $"Producción {ot.NumeroOT}" });
        }

        private void NuevaOt_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionService.PuedeGenerarOrdenTrabajo)
            {
                PermissionService.MostrarSinPermiso();
                return;
            }

            AbrirVentana(() => new NuevaOrdenTrabajoWindow(_ordenes) { Owner = Application.Current.MainWindow });
            Cargar();
        }

        private void Abrir_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot) Abrir(ot); }
        private void Kardex_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot) AbrirVentana(() => new OrdenTrabajoKardexWindow(ot) { Owner = Application.Current.MainWindow, Title = $"Kardex de {ot.NumeroOT}" }); }
        private void Historial_Click(object sender, RoutedEventArgs e) { if ((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot) AbrirVentana(() => new OrdenTrabajoHistorialWindow(ot) { Owner = Application.Current.MainWindow, Title = $"Historial de {ot.NumeroOT}" }); }
        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajo resumen)
                return;

            try
            {
                OrdenTrabajo ot = _negocio.Obtener(resumen.IdOrdenTrabajo)
                    ?? throw new InvalidOperationException("No se encontró la OT seleccionada.");
                bool otAprobada = resumen.EstaAprobada || ot.EstaAprobada;
                int? idAprobadorGerencia = ot.IdUsuarioAprobacionGerencia ?? resumen.IdUsuarioAprobacionGerencia;
                Usuario? gerente = idAprobadorGerencia.HasValue
                    ? new UsuarioNegocio().ObtenerPorId(idAprobadorGerencia.Value)
                    : null;
                OrdenCompraInterna? oci = ot.IdOrdenCompraInterna > 0
                    ? new OrdenCompraInternaNegocio().Obtener(ot.IdOrdenCompraInterna)
                    : null;
                Dictionary<int, string> observaciones = oci?.Detalles
                    .ToDictionary(x => x.IdOrdenCompraInternaDetalle, x => x.Observacion ?? string.Empty)
                    ?? [];

                new OrdenTrabajoImpresionWindow(
                    ot.NumeroOT,
                    oci?.NumeroOci ?? "OT MANUAL",
                    oci?.OrdenCompraCliente ?? string.Empty,
                    ot.NombreCliente,
                    ot.UsuarioCreacion,
                    ot.UsuarioAutoriza,
                    ot.FechaRegistro == default ? ot.FechaEmision : ot.FechaRegistro,
                    ot.Observacion,
                    ot.Detalles.Select(detalle => new OrdenTrabajoImpresionItem
                    {
                        Codigo = detalle.CodigoProducto,
                        Producto = detalle.NombreProducto,
                        Observacion = observaciones.TryGetValue(detalle.IdOrdenCompraInternaDetalle, out string? obs)
                            ? obs
                            : detalle.ObservacionDiferencia,
                        CantidadOc = Math.Max(0, detalle.CantidadPlanificada - detalle.CantidadAdicionalStock),
                        CantidadStock = detalle.CantidadAdicionalStock
                    }),
                    null,
                    oci?.FechaEntrega == default ? null : oci?.FechaEntrega,
                    otAprobada ? "APROBADA" : ot.EstadoAprobacion,
                    gerente?.NombreUsuario
                        ?? resumen.UsuarioAprobadorGerencia
                        ?? string.Empty)
                {
                    Owner = Application.Current.MainWindow,
                    Title = $"Imprimir {ot.NumeroOT}"
                }.ShowDialog();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo preparar la impresión: {ex.Message}");
            }
        }

        private void Aprobar_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajo resumen) return;
            try
            {
                OrdenTrabajo ot = _negocio.Obtener(resumen.IdOrdenTrabajo) ?? throw new InvalidOperationException("No se encontró la OT seleccionada.");
                if (!ot.PuedeAprobarGerencia)
                {
                    NotificationService.Warning("Esta OT no es elegible para aprobación: puede estar aprobada, terminada o anulada.");
                    return;
                }
                new OrdenTrabajoAprobacionWindow(ot) { Owner = Application.Current.MainWindow }.ShowDialog();
                Cargar();
            }
            catch (Exception ex) { NotificationService.Error($"No se pudo abrir la aprobación: {ex.Message}"); }
        }
        private void Anular_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionService.PuedeOperarOrdenTrabajo)
            {
                PermissionService.MostrarSinPermiso();
                return;
            }

            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajo ot)
                return;

            if (!ot.PuedeAnular)
            {
                NotificationService.Warning("Solo se puede anular una OT en estado Pendiente o En Proceso sin productos terminados.");
                return;
            }

            try
            {
                OrdenTrabajo detalle = _negocio.Obtener(ot.IdOrdenTrabajo) ?? ot;
                bool convertirProcesoAMerma = detalle.EstadoOperativo.Equals("En Proceso", StringComparison.OrdinalIgnoreCase);

                if (convertirProcesoAMerma
                    && detalle.Detalles.Any(x => x.Estado.Equals("TERMINADO", StringComparison.OrdinalIgnoreCase) || x.CantidadProducida > 0))
                {
                    NotificationService.Warning("La OT tiene productos terminados y no puede anularse.");
                    return;
                }

                string advertencia = convertirProcesoAMerma
                    ? $"Esta seguro de anular este pedido? Tiene productos en proceso en {ResumenAreasProceso(detalle)}. Al confirmar, esos productos pasaran a estado Merma. Si cancela, el proceso continuara."
                    : "La OCI quedara disponible para generar una nueva OT si aun tiene pendiente.";

                AnularOrdenTrabajoWindow ventana = new(detalle.NumeroOT, advertencia)
                {
                    Owner = Application.Current.MainWindow
                };

                if (ventana.ShowDialog() != true)
                    return;

                int idUsuario = SessionManager.UsuarioActual?.IdUsuario ?? 0;
                string usuario = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
                _negocio.Anular(detalle.IdOrdenTrabajo, convertirProcesoAMerma, idUsuario, ventana.MotivoAnulacion, usuario);
                NotificationService.Success($"OT {ot.NumeroOT} anulada correctamente.");
                Cargar();
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
            }
        }

        private static string ResumenAreasProceso(OrdenTrabajo ot)
        {
            List<string> areas = ot.Areas
                .Where(x => x.CantidadPendiente > 0 && x.Estado is not ("FINALIZADA" or "BLOQUEADA" or "ANULADA"))
                .GroupBy(x => x.NombreArea)
                .Select(g => $"{g.Key} ({g.Sum(x => x.CantidadPendiente):N2})")
                .ToList();

            return areas.Count == 0 ? "las areas de produccion" : string.Join(", ", areas);
        }

        private void Abrir(OrdenTrabajo ot)
        {
            if (!ot.EstaAprobada)
            {
                string estado = ot.EstadoAprobacionTexto;
                NotificationService.Warning($"La OT {ot.NumeroOT} está {estado}. Debe ser aprobada por Gerencia antes de ingresar a producción o detalle.");
                return;
            }

            AbrirVentana(() => new OrdenTrabajoDetalleWindow(ot.IdOrdenTrabajo) { Owner = Application.Current.MainWindow });
            Cargar();
        }
        private string ObtenerAprobadoresTexto()
        {
            List<Usuario> aprobadores = _usuariosNegocio.Listar()
                .Where(x => x.Estado && x.AprobacionOs)
                .OrderBy(x => string.IsNullOrWhiteSpace(x.NombreEmpleado) ? x.NombreUsuario : x.NombreEmpleado)
                .ToList();

            return aprobadores.Count == 0
                ? "Sin usuarios autorizados"
                : string.Join(", ", aprobadores.Select(x => string.IsNullOrWhiteSpace(x.NombreEmpleado) ? x.NombreUsuario : x.NombreEmpleado));
        }
        private static void AbrirVentana(Func<Window> crear) { try { crear().ShowDialog(); } catch (Exception ex) { NotificationService.Error($"No se pudo abrir la ventana: {ex.Message}"); } }
    }
}
