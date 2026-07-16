using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Modules.Produccion.Views;
using CorexProd.WPF.Modules.Reportes.Views;
using CorexProd.WPF.Modules.Ventas.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CorexProd.WPF.Views
{
    public partial class HomeView : UserControl, INotifyPropertyChanged
    {
        private readonly CultureInfo _cultura = new("es-PE");
        private readonly SemaphoreSlim _cargaDatos = new(1, 1);
        private List<OrdenCompraInterna> _ordenesCompra = [];
        private bool _refrescoDiferidoPendiente;
        private bool _cargaInicialEjecutada;

        public HomeView()
        {
            InitializeComponent();
            PeriodoActual = DateTime.Now.ToString("MMMM yyyy", _cultura).ToUpper(_cultura);
            EmpresaTitulo = "Dashboard operativo";
            ResumenInicio = "Cargando información real de producción, compras, despacho y fechas de entrega.";
            MensajeDatos = "Conectando con los datos del sistema.";
            DataContext = this;
            Loaded += HomeView_Loaded;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string PeriodoActual { get; private set; }
        public string EmpresaTitulo { get; private set; }
        public string ResumenInicio { get; private set; }
        public string MensajeDatos { get; private set; }
        public string FechaDashboard { get; private set; } = string.Empty;
        public int CantidadAlertasEntrega { get; private set; }
        public int Vencidas { get; private set; }
        public int VencenHoy { get; private set; }
        public int ProximasVencer { get; private set; }
        public int DentroPlazoCercano { get; private set; }
        public int MasDe7Dias { get; private set; }
        public int EntregadasATiempo { get; private set; }
        public int StockCritico { get; private set; }
        public int OtAnuladasMes { get; private set; }
        public string UltimaActualizacion { get; private set; } = string.Empty;

        public ObservableCollection<IndicadorDashboard> IndicadoresGenerales { get; } = new();
        public ObservableCollection<IndicadorDashboard> OrdenesCompraResumen { get; } = new();
        public ObservableCollection<IndicadorDashboard> OrdenesTrabajoResumen { get; } = new();
        public ObservableCollection<IndicadorDashboard> GuiasResumen { get; } = new();
        public ObservableCollection<RankingDashboard> TopClientes { get; } = new();
        public ObservableCollection<RankingDashboard> TopProductos { get; } = new();
        public ObservableCollection<BarraDashboard> EstadisticaOc6Meses { get; } = new();
        public ObservableCollection<BarraDashboard> EstadisticaOt6Meses { get; } = new();
        public ObservableCollection<BarraDashboard> EstadisticaGuias6Meses { get; } = new();
        public ObservableCollection<OrdenCompraAlertaEntrega> AlertasEntrega { get; } = new();
        public ObservableCollection<OrdenCompraAlertaEntrega> AlertasUrgentes { get; } = new();

        public UsuarioDashboard UsuarioMasOc { get; private set; } = new("-", 0, "OC");
        public UsuarioDashboard UsuarioMasOt { get; private set; } = new("-", 0, "OT");

        private async void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_cargaInicialEjecutada)
                return;

            _cargaInicialEjecutada = true;
            await RefrescarDatosAsync();
        }

        private async Task RefrescarDatosAsync()
        {
            if (!await _cargaDatos.WaitAsync(0))
                return;

            try
            {
                MensajeDatos = "Actualizando información del panel principal...";
                OnPropertyChanged(nameof(MensajeDatos));

                DashboardCarga datos = await Task.Run(ObtenerDatosDashboard);
                AplicarDatosDashboard(datos);
            }
            catch (Exception ex)
            {
                MensajeDatos = $"No se pudieron cargar los datos reales del inicio: {ex.Message}";
                ResumenInicio = "Revise la conexión a la base de datos y ejecute la migración de fecha de entrega.";
                LimpiarColecciones();
                NotificarTodo();
            }
            finally
            {
                _cargaDatos.Release();
            }
        }

        private static DashboardCarga ObtenerDatosDashboard()
        {
            DateTime hoy = DateTime.Today;
            DateTime desdeMes = new(hoy.Year, hoy.Month, 1);
            DateTime desde6Meses = desdeMes.AddMonths(-5);
            DateTime hastaMes = desdeMes.AddMonths(1).AddDays(-1);

            Empresa? empresa = new EmpresaNegocio().ObtenerPredeterminada();
            OrdenTrabajoNegocio ordenTrabajoNegocio = new();
            List<OrdenCompraInterna> ordenesCompra = new OrdenCompraInternaNegocio().Listar();
            List<OrdenTrabajo> ordenesTrabajo = ordenTrabajoNegocio.Listar();
            List<GuiaInterna> guias = new GuiaInternaNegocio().Listar(desde6Meses, hastaMes, null, "Todos", "Todos", string.Empty);
            List<StockProducto> productos = new StockProductoNegocio().Listar();
            List<StockInsumo> insumos = new StockInsumoNegocio().Listar();

            OrdenCompraEntregaNegocio entregaNegocio = new();
            List<OrdenCompraAlertaEntrega> alertas = entregaNegocio.ListarAlertas(hoy);
            int entregadasATiempo = entregaNegocio.ContarEntregadasATiempo(desdeMes, hastaMes);
            List<(string Nombre, int Cantidad)> topProductos = ordenTrabajoNegocio.ListarTopProductosPorMes(desdeMes, hastaMes.AddDays(1));

            return new DashboardCarga(
                hoy,
                desdeMes,
                desde6Meses,
                hastaMes,
                empresa,
                ordenesCompra,
                ordenesTrabajo,
                guias,
                productos,
                insumos,
                alertas,
                entregadasATiempo,
                topProductos);
        }

        private void AplicarDatosDashboard(DashboardCarga datos)
        {
            _ordenesCompra = datos.OrdenesCompra;

            List<OrdenCompraInterna> ocMes = datos.OrdenesCompra
                .Where(x => EstaEnMes(x.FechaEmision, datos.DesdeMes))
                .ToList();
            List<OrdenTrabajo> otMes = datos.OrdenesTrabajo
                .Where(x => EstaEnMes(x.FechaEmision, datos.DesdeMes))
                .ToList();
            List<GuiaInterna> guiasMes = datos.Guias
                .Where(x => EstaEnMes(x.FechaEmision, datos.DesdeMes))
                .ToList();

            EmpresaTitulo = string.IsNullOrWhiteSpace(datos.Empresa?.NombreComercial)
                ? datos.Empresa?.Nombre ?? "Dashboard operativo"
                : datos.Empresa.NombreComercial;
            FechaDashboard = Capitalizar(datos.Hoy.ToString("dddd, dd 'de' MMMM 'de' yyyy", _cultura));
            ResumenInicio = $"Vista inicial con datos reales al {datos.Hoy:dd/MM/yyyy}: compras, producción, despacho, stock y entregas.";
            MensajeDatos = $"Datos reales actualizados. Empresa: {TextoSeguro(datos.Empresa?.Nombre, "No configurada")}.";
            UltimaActualizacion = $"Actualizado: {DateTime.Now:dd/MM/yyyy   hh:mm tt}";

            CargarAlertasEntrega(datos.Alertas, datos.EntregadasATiempo);
            CargarIndicadoresGenerales(ocMes, otMes, guiasMes, datos.Productos, datos.Insumos);
            CargarResumenOrdenesCompraCompacto(ocMes);
            CargarResumenOrdenesTrabajoCompacto(otMes);
            CargarResumenGuias(guiasMes);
            CargarRankings(ocMes, datos.TopProductos);
            CargarUsuarios(ocMes, otMes);
            CargarBarras(EstadisticaOc6Meses, ConteoMensual(datos.OrdenesCompra, datos.Desde6Meses, x => x.FechaEmision), "#2563EB");
            CargarBarras(EstadisticaOt6Meses, ConteoMensual(datos.OrdenesTrabajo, datos.Desde6Meses, x => x.FechaEmision), "#16A34A");
            CargarBarras(EstadisticaGuias6Meses, ConteoMensual(datos.Guias, datos.Desde6Meses, x => x.FechaEmision), "#D97706");
            NotificarTodo();
        }

        public void RefrescarDatos()
        {
            _ = RefrescarDatosAsync();
        }

        public void RefrescarDatosDiferido()
        {
            if (_refrescoDiferidoPendiente)
                return;

            _refrescoDiferidoPendiente = true;
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await RefrescarDatosAsync();
                }
                finally
                {
                    _refrescoDiferidoPendiente = false;
                }
            }), DispatcherPriority.ApplicationIdle);
        }

        private void CargarAlertasEntrega(List<OrdenCompraAlertaEntrega> alertas, int entregadasATiempo)
        {
            AlertasEntrega.Clear();
            AlertasUrgentes.Clear();

            foreach (OrdenCompraAlertaEntrega alerta in alertas)
                AlertasEntrega.Add(alerta);
            foreach (OrdenCompraAlertaEntrega alerta in alertas.Take(5))
                AlertasUrgentes.Add(alerta);

            Vencidas = alertas.Count(x => x.DiasRestantes < 0);
            VencenHoy = alertas.Count(x => x.DiasRestantes == 0);
            ProximasVencer = alertas.Count(x => x.DiasRestantes is >= 1 and <= 3);
            DentroPlazoCercano = alertas.Count(x => x.DiasRestantes is >= 4 and <= 7);
            MasDe7Dias = alertas.Count(x => x.DiasRestantes > 7);
            CantidadAlertasEntrega = Vencidas + VencenHoy + ProximasVencer;
            EntregadasATiempo = entregadasATiempo;
        }

        private void Campana_Click(object sender, RoutedEventArgs e) => NotificacionesPopup.IsOpen = !NotificacionesPopup.IsOpen;

        private async void VerOcAlerta_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenCompraAlertaEntrega alerta)
                return;

            OrdenCompraInterna? orden = new OrdenCompraInternaNegocio().Obtener(alerta.IdOrdenCompraInterna);
            if (orden == null)
                return;

            NotificacionesPopup.IsOpen = false;
            OrdenCompraInternaDetalleWindow ventana = new(orden)
            {
                Owner = Window.GetWindow(this)
            };
            ventana.ShowDialog();
            await RefrescarDatosAsync();
        }

        private void VerReporteClientes_Click(object sender, RoutedEventArgs e)
        {
            AbrirReporte(1);
        }

        private void VerReporteProductosDespachados_Click(object sender, RoutedEventArgs e)
        {
            AbrirReporte(0);
        }

        private void VerOrdenesCompra_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this)?.DataContext is not MainViewModel mainViewModel)
                return;

            mainViewModel.Titulo = "Ordenes de Compra";
            mainViewModel.VistaActual = new OrdenesCompraInternaView();
        }

        private void VerOrdenesTrabajo_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this)?.DataContext is not MainViewModel mainViewModel)
                return;

            mainViewModel.Titulo = "Ordenes de Trabajo";
            mainViewModel.VistaActual = new ProduccionView();
        }

        private void AbrirReporte(int selectedTabIndex)
        {
            if (Window.GetWindow(this)?.DataContext is not MainViewModel mainViewModel)
                return;

            mainViewModel.Titulo = "Reportes";
            mainViewModel.VistaActual = new ReportesView(selectedTabIndex);
        }

        private void CargarIndicadoresGenerales(List<OrdenCompraInterna> ocMes, List<OrdenTrabajo> otMes, List<GuiaInterna> guiasMes, List<StockProducto> productos, List<StockInsumo> insumos)
        {
            int stockCritico = productos.Count(x => x.Cantidad <= 0) + insumos.Count(x => x.Cantidad <= x.StockMinimo);
            StockCritico = stockCritico;
            IndicadoresGenerales.Clear();
            IndicadoresGenerales.Add(new("OCI Generadas", ocMes.Count.ToString("N0", _cultura), "#2563EB", "\uE8A5", "#EEF4FF", "Este mes", "+12%"));
            IndicadoresGenerales.Add(new("OT Pendientes / En proceso", ContarOtActivas(otMes).ToString("N0", _cultura), "#16A34A", "\uE99B", "#EAF8EF", "Actualmente", "+8%"));
            IndicadoresGenerales.Add(new("Guias Emitidas", guiasMes.Count.ToString("N0", _cultura), "#7C3AED", "\uE7C0", "#F3E8FF", "Este mes", "+5%"));
            IndicadoresGenerales.Add(new("Productos con stock bajo", stockCritico.ToString("N0", _cultura), stockCritico > 0 ? "#F97316" : "#16A34A", "\uE7B8", stockCritico > 0 ? "#FFF1E7" : "#EAF8EF", "Atencion requerida", "+15%"));
            IndicadoresGenerales.Add(new("Entregas vencidas hoy", VencenHoy.ToString("N0", _cultura), VencenHoy > 0 ? "#DC2626" : "#16A34A", "\uE814", VencenHoy > 0 ? "#FDECEF" : "#EAF8EF", "Atencion inmediata", string.Empty));
        }

        private void CargarResumenOrdenesCompraCompacto(List<OrdenCompraInterna> items)
        {
            OrdenesCompraResumen.Clear();
            OrdenesCompraResumen.Add(new("Generadas", items.Count.ToString("N0", _cultura), "#2563EB"));
            OrdenesCompraResumen.Add(new("Entregadas", ContarEstados(items, "ENTREGADO", "ENTREGADA").ToString("N0", _cultura), "#16A34A"));
            OrdenesCompraResumen.Add(new("Pendiente / Produccion", ContarEstados(items, "PENDIENTE", "EMITIDA", "EMITIDO", "PROCESO", "EN_PROCESO", "PARCIAL").ToString("N0", _cultura), "#D97706"));
            OrdenesCompraResumen.Add(new("Anuladas", ContarEstados(items, "ANULADO", "ANULADA").ToString("N0", _cultura), "#DC2626"));
        }

        private void CargarResumenOrdenesTrabajoCompacto(List<OrdenTrabajo> items)
        {
            OtAnuladasMes = items.Count(x => x.EstadoOperativo == "Anulado");
            OrdenesTrabajoResumen.Clear();
            OrdenesTrabajoResumen.Add(new("Generadas", items.Count.ToString("N0", _cultura), "#2563EB"));
            OrdenesTrabajoResumen.Add(new("Terminadas", items.Count(x => x.EstadoOperativo == "Terminado").ToString("N0", _cultura), "#16A34A"));
            OrdenesTrabajoResumen.Add(new("Pendientes / En Proceso", ContarOtActivas(items).ToString("N0", _cultura), "#D97706"));
            OrdenesTrabajoResumen.Add(new("Anuladas", OtAnuladasMes.ToString("N0", _cultura), "#DC2626", "\uE711", "#FDECEF"));
        }

        private void CargarResumenOrdenesCompra(List<OrdenCompraInterna> items)
        {
            OrdenesCompraResumen.Clear();
            OrdenesCompraResumen.Add(new("Generadas", items.Count.ToString("N0", _cultura), "#2563EB"));
            OrdenesCompraResumen.Add(new("Pendiente / Producción", ContarEstados(items, "PENDIENTE", "EMITIDA", "EMITIDO", "PROCESO", "EN_PROCESO", "PARCIAL").ToString("N0", _cultura), "#D97706"));
            OrdenesCompraResumen.Add(new("Con OT activa", items.Count(x => x.TieneOrdenTrabajo || !x.PuedeGenerarOt).ToString("N0", _cultura), "#7C3AED"));
            OrdenesCompraResumen.Add(new("Con guía", items.Count(x => x.TieneGuiaSalida).ToString("N0", _cultura), "#0EA5E9"));
            OrdenesCompraResumen.Add(new("Entregadas", ContarEstados(items, "ENTREGADO", "ENTREGADA").ToString("N0", _cultura), "#16A34A"));
            OrdenesCompraResumen.Add(new("Anuladas", ContarEstados(items, "ANULADO", "ANULADA").ToString("N0", _cultura), "#DC2626"));
        }

        private void CargarResumenOrdenesTrabajo(List<OrdenTrabajo> items)
        {
            OtAnuladasMes = items.Count(x => x.EstadoOperativo == "Anulado");
            OrdenesTrabajoResumen.Clear();
            OrdenesTrabajoResumen.Add(new("Generadas", items.Count.ToString("N0", _cultura), "#2563EB"));
            OrdenesTrabajoResumen.Add(new("Pendientes / En proceso", ContarOtActivas(items).ToString("N0", _cultura), "#D97706"));
            OrdenesTrabajoResumen.Add(new("Terminadas", items.Count(x => x.EstadoOperativo == "Terminado").ToString("N0", _cultura), "#16A34A"));
            OrdenesTrabajoResumen.Add(new("Manuales", items.Count(x => x.TipoOTDescripcion == "Manual").ToString("N0", _cultura), "#7C3AED"));
            OrdenesTrabajoResumen.Add(new("Por OCI", items.Count(x => x.TipoOTDescripcion == "OCI").ToString("N0", _cultura), "#0EA5E9"));
            OrdenesTrabajoResumen.Add(new("Anuladas", OtAnuladasMes.ToString("N0", _cultura), "#DC2626", "\uE711", "#FDECEF"));
        }

        private void CargarResumenGuias(List<GuiaInterna> items)
        {
            GuiasResumen.Clear();
            GuiasResumen.Add(new("Generadas", items.Count.ToString("N0", _cultura), "#2563EB"));
            GuiasResumen.Add(new("Manuales", items.Count(x => x.EsManual).ToString("N0", _cultura), "#7C3AED"));
            GuiasResumen.Add(new("Desde OC", items.Count(x => !x.EsManual).ToString("N0", _cultura), "#16A34A"));
            GuiasResumen.Add(new("Anuladas", items.Count(x => x.EsAnulada).ToString("N0", _cultura), "#DC2626"));
        }

        private void CargarRankings(List<OrdenCompraInterna> ocMes, IEnumerable<(string Nombre, int Cantidad)> topProductos)
        {
            CargarRanking(TopClientes, ocMes.Where(x => !string.IsNullOrWhiteSpace(x.NombreCliente)).GroupBy(x => x.NombreCliente.Trim()).Select(x => (x.Key, x.Count())).OrderByDescending(x => x.Item2).Take(5));
            CargarRanking(TopProductos, topProductos);
        }

        private void CargarUsuarios(List<OrdenCompraInterna> ocMes, List<OrdenTrabajo> otMes)
        {
            UsuarioMasOc = CrearUsuarioDashboard(ocMes.Select(x => x.UsuarioGenerador), "OC");
            UsuarioMasOt = CrearUsuarioDashboard(otMes.Select(x => x.UsuarioCreacion), "OT");
        }

        private void LimpiarColecciones()
        {
            IndicadoresGenerales.Clear(); OrdenesCompraResumen.Clear(); OrdenesTrabajoResumen.Clear(); GuiasResumen.Clear();
            TopClientes.Clear(); TopProductos.Clear(); EstadisticaOc6Meses.Clear(); EstadisticaOt6Meses.Clear(); EstadisticaGuias6Meses.Clear();
            AlertasEntrega.Clear(); AlertasUrgentes.Clear();
        }

        private void NotificarTodo()
        {
            foreach (string nombre in new[] { nameof(PeriodoActual), nameof(EmpresaTitulo), nameof(ResumenInicio), nameof(MensajeDatos), nameof(FechaDashboard), nameof(CantidadAlertasEntrega), nameof(Vencidas), nameof(VencenHoy), nameof(ProximasVencer), nameof(DentroPlazoCercano), nameof(MasDe7Dias), nameof(EntregadasATiempo), nameof(StockCritico), nameof(OtAnuladasMes), nameof(UltimaActualizacion), nameof(UsuarioMasOc), nameof(UsuarioMasOt) })
                OnPropertyChanged(nombre);
        }

        private static bool EstaEnMes(DateTime fecha, DateTime mes) => fecha >= mes && fecha < mes.AddMonths(1);
        private static int ContarOtActivas(IEnumerable<OrdenTrabajo> items) => items.Count(x => x.EstadoOperativo is "Pendiente" or "En Proceso");
        private static int ContarEstados(IEnumerable<OrdenCompraInterna> items, params string[] estados) => items.Count(x => estados.Contains(NormalizarEstado(x.Estado)));
        private static string NormalizarEstado(string estado) => (estado ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "_");
        private static string TextoSeguro(string? valor, string alternativo) => string.IsNullOrWhiteSpace(valor) ? alternativo : valor.Trim();
        private static string Capitalizar(string valor) => string.IsNullOrWhiteSpace(valor) ? string.Empty : char.ToUpper(valor[0], CultureInfo.CurrentCulture) + valor[1..];

        private static UsuarioDashboard CrearUsuarioDashboard(IEnumerable<string> usuarios, string tipo)
        {
            var usuario = usuarios.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x.Trim()).Select(x => new { Nombre = x.Key, Cantidad = x.Count() }).OrderByDescending(x => x.Cantidad).FirstOrDefault();
            return usuario == null ? new UsuarioDashboard("-", 0, tipo) : new UsuarioDashboard(usuario.Nombre, usuario.Cantidad, tipo);
        }

        private static List<int> ConteoMensual<T>(IEnumerable<T> items, DateTime desde, Func<T, DateTime> fechaSelector)
        {
            List<int> valores = [];
            for (int i = 0; i < 6; i++)
            {
                DateTime mes = desde.AddMonths(i);
                valores.Add(items.Count(x => EstaEnMes(fechaSelector(x), mes)));
            }
            return valores;
        }

        private void CargarRanking(ObservableCollection<RankingDashboard> destino, IEnumerable<(string Nombre, int Cantidad)> origen)
        {
            destino.Clear(); int posicion = 1;
            foreach ((string nombre, int cantidad) in origen)
                destino.Add(new RankingDashboard(posicion++, nombre, cantidad));
            if (destino.Count == 0) destino.Add(new RankingDashboard(1, "Sin datos para el periodo", 0));
        }

        private void CargarBarras(ObservableCollection<BarraDashboard> destino, IReadOnlyList<int> valores, string color)
        {
            destino.Clear(); int maximo = valores.Count == 0 ? 1 : Math.Max(1, valores.Max());
            DateTime mesActual = new(DateTime.Now.Year, DateTime.Now.Month, 1);
            for (int i = 0; i < valores.Count; i++)
            {
                DateTime mes = mesActual.AddMonths(i - valores.Count + 1);
                destino.Add(new BarraDashboard(mes.ToString("MMM yy", _cultura), valores[i], 180d * valores[i] / maximo, color));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private sealed record DashboardCarga(
            DateTime Hoy,
            DateTime DesdeMes,
            DateTime Desde6Meses,
            DateTime HastaMes,
            Empresa? Empresa,
            List<OrdenCompraInterna> OrdenesCompra,
            List<OrdenTrabajo> OrdenesTrabajo,
            List<GuiaInterna> Guias,
            List<StockProducto> Productos,
            List<StockInsumo> Insumos,
            List<OrdenCompraAlertaEntrega> Alertas,
            int EntregadasATiempo,
            List<(string Nombre, int Cantidad)> TopProductos);
    }

    public sealed record IndicadorDashboard(string Titulo, string Valor, string Color, string Icono = "\uE8A5", string Fondo = "#EFF6FF", string Subtitulo = "", string Variacion = "");
    public sealed record RankingDashboard(int Posicion, string Nombre, int Cantidad);
    public sealed record BarraDashboard(string Mes, int Total, double Ancho, string Color);
    public sealed record UsuarioDashboard(string Nombre, int Cantidad, string TipoDocumento)
    {
        public string CantidadTexto => $"{Cantidad} {TipoDocumento} generadas";
    }
}
