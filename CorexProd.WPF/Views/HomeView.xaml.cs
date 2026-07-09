using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace CorexProd.WPF.Views
{
    public partial class HomeView : UserControl, INotifyPropertyChanged
    {
        public HomeView()
        {
            InitializeComponent();
            PeriodoActual = DateTime.Now.ToString("MMMM yyyy", new CultureInfo("es-PE")).ToUpper();
            CargarDatosDemo();
            DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string PeriodoActual { get; private set; } = string.Empty;

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

        private void CargarDatosDemo()
        {
            IndicadoresGenerales.Clear();
            IndicadoresGenerales.Add(new("OC del mes", "128", "#2563EB"));
            IndicadoresGenerales.Add(new("OT del mes", "96", "#16A34A"));
            IndicadoresGenerales.Add(new("Guías internas", "74", "#D97706"));
            IndicadoresGenerales.Add(new("Producción en proceso", "31", "#7C3AED"));

            OrdenesCompraResumen.Clear();
            OrdenesCompraResumen.Add(new("Generadas", "128", "#2563EB"));
            OrdenesCompraResumen.Add(new("Pendiente / Producción", "34", "#D97706"));
            OrdenesCompraResumen.Add(new("Parcial despachadas", "12", "#7C3AED"));
            OrdenesCompraResumen.Add(new("Despachadas", "43", "#0EA5E9"));
            OrdenesCompraResumen.Add(new("Entregadas", "35", "#16A34A"));
            OrdenesCompraResumen.Add(new("Anuladas", "4", "#DC2626"));

            OrdenesTrabajoResumen.Clear();
            OrdenesTrabajoResumen.Add(new("Generadas", "96", "#2563EB"));
            OrdenesTrabajoResumen.Add(new("En proceso", "27", "#D97706"));
            OrdenesTrabajoResumen.Add(new("Completadas", "54", "#16A34A"));
            OrdenesTrabajoResumen.Add(new("Regularización stock", "8", "#7C3AED"));
            OrdenesTrabajoResumen.Add(new("Abastecimiento stock", "5", "#0EA5E9"));
            OrdenesTrabajoResumen.Add(new("Anuladas", "2", "#DC2626"));

            GuiasResumen.Clear();
            GuiasResumen.Add(new("Generadas", "74", "#2563EB"));
            GuiasResumen.Add(new("Pendientes", "14", "#D97706"));
            GuiasResumen.Add(new("Atendidas", "57", "#16A34A"));
            GuiasResumen.Add(new("Anuladas", "3", "#DC2626"));

            TopClientes.Clear();
            TopClientes.Add(new(1, "Confecciones San Miguel", 24));
            TopClientes.Add(new(2, "Textiles La Victoria", 19));
            TopClientes.Add(new(3, "Distribuciones Lima Norte", 16));
            TopClientes.Add(new(4, "Comercial El Sol", 13));
            TopClientes.Add(new(5, "Grupo Rivera", 11));

            TopProductos.Clear();
            TopProductos.Add(new(1, "Pantalón drill azul T/M", 420));
            TopProductos.Add(new(2, "Camisa oxford blanca T/L", 360));
            TopProductos.Add(new(3, "Casaca industrial negra", 285));
            TopProductos.Add(new(4, "Polo algodón cuello redondo", 250));
            TopProductos.Add(new(5, "Chaleco reflectivo", 210));

            UsuarioMasOc = new UsuarioDashboard("Luis Solis", 38, "OC");
            UsuarioMasOt = new UsuarioDashboard("Administrador", 31, "OT");
            OnPropertyChanged(nameof(UsuarioMasOc));
            OnPropertyChanged(nameof(UsuarioMasOt));

            CargarBarras(EstadisticaOc6Meses, new[] { 72, 85, 91, 104, 118, 128 }, "#2563EB");
            CargarBarras(EstadisticaOt6Meses, new[] { 58, 69, 73, 81, 88, 96 }, "#16A34A");
            CargarBarras(EstadisticaGuias6Meses, new[] { 40, 47, 55, 61, 69, 74 }, "#D97706");
        }

        private static void CargarBarras(ObservableCollection<BarraDashboard> destino, IReadOnlyList<int> valores, string color)
        {
            destino.Clear();

            int maximo = 1;
            foreach (int valor in valores)
            {
                if (valor > maximo)
                {
                    maximo = valor;
                }
            }

            DateTime mes = new(DateTime.Now.Year, DateTime.Now.Month, 1);
            for (int i = 0; i < valores.Count; i++)
            {
                DateTime itemMes = mes.AddMonths(i - valores.Count + 1);
                double ancho = 220d * valores[i] / maximo;
                destino.Add(new BarraDashboard(itemMes.ToString("MMM yy", new CultureInfo("es-PE")), valores[i], ancho, color));
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
