using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace CorexProd.WPF.Views
{
    public partial class HomeView : UserControl, INotifyPropertyChanged
    {
        private readonly CultureInfo _cultura = new("es-PE");

        public HomeView()
        {
            InitializeComponent();
            PeriodoActual = DateTime.Now.ToString("MMMM yyyy", _cultura).ToUpper(_cultura);
            EmpresaTitulo = "Dashboard operativo";
            ResumenInicio = "Cargando información real de producción, compras y despacho.";
            MensajeDatos = "Conectando con los datos del sistema.";
            DataContext = this;
            CargarDatosReales();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string PeriodoActual { get; private set; } = string.Empty;

        public string EmpresaTitulo { get; private set; }

        public string ResumenInicio { get; private set; }

        public string MensajeDatos { get; private set; }

        public ObservableCollection<IndicadorDashboard> IndicadoresGenerales { get; } = new();
        public ObservableCollection<IndicadorDashboard> OrdenesCompraResumen { get; } = new();
        public ObservableCollection<IndicadorDashboard> OrdenesTrabajoResumen { get; } = new();
        public ObservableCollection<IndicadorDashboard> GuiasResumen { get; } = new();

        public ObservableCollection<RankingDashboard> TopClientes { get; } = new();
        public ObservableCollection<RankingDashboard> TopProductos { get; } = new();

        public ObservableCollection<BarraDashboard> EstadisticaOc6Meses { get; } = new();
        public ObservableCollection<BarraDashboard> EstadisticaOt6Meses { get; } = new();
        public ObservableCollection<BarraDashboard> EstadisticaGuias6Meses { get; } = new();

        public UsuarioDashboard UsuarioMasOc { get; private set; } = new("-", 0, "OC");
        public UsuarioDashboard UsuarioMasOt { get; private set; } = new("-", 0, "OT");

        private void CargarDatosReales()
        {
            try
            {
                DateTime hoy = DateTime.Today;
                DateTime desdeMes = new(hoy.Year, hoy.Month, 1);
                DateTime desde6Meses = desdeMes.AddMonths(-5);
                DateTime hasta = desdeMes.AddMonths(1).AddDays(-1);

                Empresa? empresa = new EmpresaNegocio().ObtenerPredeterminada();
                OrdenTrabajoNegocio ordenTrabajoNegocio = new();
                List<OrdenCompraInterna> ordenesCompra = new OrdenCompraInternaNegocio().Listar();
                List<OrdenTrabajo> ordenesTrabajo = ordenTrabajoNegocio.Listar();
                List<GuiaInterna> guias = new GuiaInternaNegocio().Listar(desde6Meses, hasta, null, "Todos", "Todos", string.Empty);
                List<StockProducto> productos = new StockProductoNegocio().Listar();
                List<StockInsumo> insumos = new StockInsumoNegocio().Listar();

                List<OrdenCompraInterna> ocMes = ordenesCompra
                    .Where(x => EstaEnMes(x.FechaEmision, desdeMes))
                    .ToList();
                List<OrdenTrabajo> otMes = ordenesTrabajo
                    .Where(x => EstaEnMes(x.FechaEmision, desdeMes))
                    .ToList();
                List<GuiaInterna> guiasMes = guias
                    .Where(x => EstaEnMes(x.FechaEmision, desdeMes))
                    .ToList();

                EmpresaTitulo = string.IsNullOrWhiteSpace(empresa?.NombreComercial)
                    ? empresa?.Nombre ?? "Dashboard operativo"
                    : empresa.NombreComercial;
                ResumenInicio = $"Vista inicial con datos reales al {hoy:dd/MM/yyyy}: compras, producción, despacho y alertas de stock.";
                MensajeDatos = $"Datos reales actualizados desde el sistema. Empresa: {TextoSeguro(empresa?.Nombre, "No configurada")}.";
                OnPropertyChanged(nameof(EmpresaTitulo));
                OnPropertyChanged(nameof(ResumenInicio));
                OnPropertyChanged(nameof(MensajeDatos));

                CargarIndicadoresGenerales(ocMes, otMes, guiasMes, productos, insumos);
                CargarResumenOrdenesCompra(ocMes);
                CargarResumenOrdenesTrabajo(otMes);
                CargarResumenGuias(guiasMes);
                CargarRankings(ocMes, otMes, ordenTrabajoNegocio);
                CargarUsuarios(ocMes, otMes);
                CargarBarras(EstadisticaOc6Meses, ConteoMensual(ordenesCompra, desde6Meses, x => x.FechaEmision), "#2563EB");
                CargarBarras(EstadisticaOt6Meses, ConteoMensual(ordenesTrabajo, desde6Meses, x => x.FechaEmision), "#16A34A");
                CargarBarras(EstadisticaGuias6Meses, ConteoMensual(guias, desde6Meses, x => x.FechaEmision), "#D97706");
            }
            catch (Exception ex)
            {
                MensajeDatos = $"No se pudieron cargar los datos reales del inicio: {ex.Message}";
                ResumenInicio = "Revise la conexión a la base de datos e ingrese nuevamente al inicio.";
                OnPropertyChanged(nameof(MensajeDatos));
                OnPropertyChanged(nameof(ResumenInicio));
                LimpiarColecciones();
            }
        }

        private void CargarIndicadoresGenerales(
            List<OrdenCompraInterna> ocMes,
            List<OrdenTrabajo> otMes,
            List<GuiaInterna> guiasMes,
            List<StockProducto> productos,
            List<StockInsumo> insumos)
        {
            int stockCritico = productos.Count(x => x.Cantidad <= 0) + insumos.Count(x => x.Cantidad <= x.StockMinimo);
            int produccionActiva = otMes.Count(x => x.EstadoOperativo is "Pendiente" or "En Proceso" or "Terminado Parcial");

            IndicadoresGenerales.Clear();
            IndicadoresGenerales.Add(new("OC del mes", ocMes.Count.ToString("N0", _cultura), "#2563EB"));
            IndicadoresGenerales.Add(new("OT del mes", otMes.Count.ToString("N0", _cultura), "#16A34A"));
            IndicadoresGenerales.Add(new("Guías internas", guiasMes.Count.ToString("N0", _cultura), "#D97706"));
            IndicadoresGenerales.Add(new("Alertas de stock", stockCritico.ToString("N0", _cultura), stockCritico > 0 ? "#DC2626" : "#16A34A"));

            if (produccionActiva > 0)
            {
                MensajeDatos += $" Producción activa: {produccionActiva:N0} OT.";
                OnPropertyChanged(nameof(MensajeDatos));
            }
        }

        private void CargarResumenOrdenesCompra(List<OrdenCompraInterna> ocMes)
        {
            OrdenesCompraResumen.Clear();
            OrdenesCompraResumen.Add(new("Generadas", ocMes.Count.ToString("N0", _cultura), "#2563EB"));
            OrdenesCompraResumen.Add(new("Pendiente / Producción", ContarEstados(ocMes, "PENDIENTE", "PROCESO", "PRODUCCION").ToString("N0", _cultura), "#D97706"));
            OrdenesCompraResumen.Add(new("Con OT activa", ocMes.Count(x => x.TieneOrdenTrabajo || !x.PuedeGenerarOt).ToString("N0", _cultura), "#7C3AED"));
            OrdenesCompraResumen.Add(new("Con guía", ocMes.Count(x => x.TieneGuiaSalida).ToString("N0", _cultura), "#0EA5E9"));
            OrdenesCompraResumen.Add(new("Entregadas", ContarEstados(ocMes, "ENTREGADO", "ENTREGADA").ToString("N0", _cultura), "#16A34A"));
            OrdenesCompraResumen.Add(new("Anuladas", ContarEstados(ocMes, "ANULADO", "ANULADA").ToString("N0", _cultura), "#DC2626"));
        }

        private void CargarResumenOrdenesTrabajo(List<OrdenTrabajo> otMes)
        {
            OrdenesTrabajoResumen.Clear();
            OrdenesTrabajoResumen.Add(new("Generadas", otMes.Count.ToString("N0", _cultura), "#2563EB"));
            OrdenesTrabajoResumen.Add(new("En proceso", otMes.Count(x => x.EstadoOperativo == "En Proceso").ToString("N0", _cultura), "#D97706"));
            OrdenesTrabajoResumen.Add(new("Terminadas", otMes.Count(x => x.EstadoOperativo == "Terminado").ToString("N0", _cultura), "#16A34A"));
            OrdenesTrabajoResumen.Add(new("Manuales", otMes.Count(x => x.TipoOTDescripcion == "Manual").ToString("N0", _cultura), "#7C3AED"));
            OrdenesTrabajoResumen.Add(new("Por OCI", otMes.Count(x => x.TipoOTDescripcion == "OCI").ToString("N0", _cultura), "#0EA5E9"));
            OrdenesTrabajoResumen.Add(new("Anuladas", otMes.Count(x => x.EstadoOperativo == "Anulado").ToString("N0", _cultura), "#DC2626"));
        }

        private void CargarResumenGuias(List<GuiaInterna> guiasMes)
        {
            GuiasResumen.Clear();
            GuiasResumen.Add(new("Generadas", guiasMes.Count.ToString("N0", _cultura), "#2563EB"));
            GuiasResumen.Add(new("Manuales", guiasMes.Count(x => x.EsManual).ToString("N0", _cultura), "#7C3AED"));
            GuiasResumen.Add(new("Desde OC", guiasMes.Count(x => !x.EsManual).ToString("N0", _cultura), "#16A34A"));
            GuiasResumen.Add(new("Anuladas", guiasMes.Count(x => x.EsAnulada).ToString("N0", _cultura), "#DC2626"));
        }

        private void CargarRankings(List<OrdenCompraInterna> ocMes, List<OrdenTrabajo> otMes, OrdenTrabajoNegocio ordenTrabajoNegocio)
        {
            CargarRanking(TopClientes, ocMes
                .Where(x => !string.IsNullOrWhiteSpace(x.NombreCliente))
                .GroupBy(x => x.NombreCliente.Trim())
                .Select(x => (Nombre: x.Key, Cantidad: x.Count()))
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Nombre)
                .Take(5));

            List<OrdenTrabajoDetalle> detallesMes = otMes
                .Select(x => ordenTrabajoNegocio.Obtener(x.IdOrdenTrabajo))
                .Where(x => x != null)
                .SelectMany(x => x!.Detalles)
                .ToList();

            IEnumerable<(string Nombre, int Cantidad)> productos = detallesMes
                .Where(x => !string.IsNullOrWhiteSpace(x.NombreProducto))
                .GroupBy(x => ProductoNombre(x))
                .Select(x => (Nombre: x.Key, Cantidad: Convert.ToInt32(Math.Round(x.Sum(d => d.CantidadPlanificada)))))
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Nombre)
                .Take(5);

            CargarRanking(TopProductos, productos);
        }

        private void CargarUsuarios(List<OrdenCompraInterna> ocMes, List<OrdenTrabajo> otMes)
        {
            UsuarioMasOc = CrearUsuarioDashboard(ocMes.Select(x => x.UsuarioGenerador), "OC");
            UsuarioMasOt = CrearUsuarioDashboard(otMes.Select(x => x.UsuarioCreacion), "OT");
            OnPropertyChanged(nameof(UsuarioMasOc));
            OnPropertyChanged(nameof(UsuarioMasOt));
        }

        private void LimpiarColecciones()
        {
            IndicadoresGenerales.Clear();
            OrdenesCompraResumen.Clear();
            OrdenesTrabajoResumen.Clear();
            GuiasResumen.Clear();
            TopClientes.Clear();
            TopProductos.Clear();
            EstadisticaOc6Meses.Clear();
            EstadisticaOt6Meses.Clear();
            EstadisticaGuias6Meses.Clear();
        }

        private static bool EstaEnMes(DateTime fecha, DateTime desdeMes) =>
            fecha >= desdeMes && fecha < desdeMes.AddMonths(1);

        private static int ContarEstados(IEnumerable<OrdenCompraInterna> ordenes, params string[] estados) =>
            ordenes.Count(x => estados.Contains(NormalizarEstado(x.Estado)));

        private static string NormalizarEstado(string estado) =>
            (estado ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "_");

        private static string ProductoNombre(OrdenTrabajoDetalle detalle) =>
            string.IsNullOrWhiteSpace(detalle.CodigoProducto)
                ? detalle.NombreProducto.Trim()
                : $"{detalle.CodigoProducto.Trim()} - {detalle.NombreProducto.Trim()}";

        private static string TextoSeguro(string? valor, string alternativo) =>
            string.IsNullOrWhiteSpace(valor) ? alternativo : valor.Trim();

        private static UsuarioDashboard CrearUsuarioDashboard(IEnumerable<string> usuarios, string tipo)
        {
            var usuario = usuarios
                .Select(TextoUsuario)
                .Where(x => x != "-")
                .GroupBy(x => x)
                .Select(x => new { Nombre = x.Key, Cantidad = x.Count() })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Nombre)
                .FirstOrDefault();

            return usuario == null
                ? new UsuarioDashboard("-", 0, tipo)
                : new UsuarioDashboard(usuario.Nombre, usuario.Cantidad, tipo);
        }

        private static string TextoUsuario(string? usuario) =>
            string.IsNullOrWhiteSpace(usuario) ? "-" : usuario.Trim();

        private static List<int> ConteoMensual<T>(IEnumerable<T> items, DateTime desde, Func<T, DateTime> fechaSelector)
        {
            List<int> valores = new();
            for (int i = 0; i < 6; i++)
            {
                DateTime mes = desde.AddMonths(i);
                valores.Add(items.Count(x => EstaEnMes(fechaSelector(x), mes)));
            }

            return valores;
        }

        private void CargarRanking(ObservableCollection<RankingDashboard> destino, IEnumerable<(string Nombre, int Cantidad)> origen)
        {
            destino.Clear();

            int posicion = 1;
            foreach ((string nombre, int cantidad) in origen)
            {
                destino.Add(new RankingDashboard(posicion++, nombre, cantidad));
            }

            if (destino.Count == 0)
            {
                destino.Add(new RankingDashboard(1, "Sin datos para el periodo", 0));
            }
        }

        private void CargarBarras(ObservableCollection<BarraDashboard> destino, IReadOnlyList<int> valores, string color)
        {
            destino.Clear();

            int maximo = valores.Count == 0 ? 1 : Math.Max(1, valores.Max());
            DateTime mes = new(DateTime.Now.Year, DateTime.Now.Month, 1);
            for (int i = 0; i < valores.Count; i++)
            {
                DateTime itemMes = mes.AddMonths(i - valores.Count + 1);
                double ancho = 220d * valores[i] / maximo;
                destino.Add(new BarraDashboard(itemMes.ToString("MMM yy", _cultura), valores[i], ancho, color));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed record IndicadorDashboard(string Titulo, string Valor, string Color);

    public sealed record RankingDashboard(int Posicion, string Nombre, int Cantidad);

    public sealed record BarraDashboard(string Mes, int Total, double Ancho, string Color);

    public sealed record UsuarioDashboard(string Nombre, int Cantidad, string TipoDocumento)
    {
        public string CantidadTexto => $"{Cantidad} {TipoDocumento} generadas";
    }
}
