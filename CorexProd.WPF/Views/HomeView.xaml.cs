using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.Views
{
    public partial class HomeView : UserControl, INotifyPropertyChanged
    {
        private readonly List<HomePage> _pages;
        private int _currentPageIndex;

        public HomeView()
        {
            InitializeComponent();
            _pages = CreatePages();
            DataContext = this;
            UpdatePage();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public HomePage CurrentPage => _pages[_currentPageIndex];
        public bool CanGoPrevious => _currentPageIndex > 0;
        public bool CanGoNext => _currentPageIndex < _pages.Count - 1;
        public string PageIndicator => $"Pagina {_currentPageIndex + 1} de {_pages.Count}";
        public double ProgressWidth => 150d * (_currentPageIndex + 1) / _pages.Count;

        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        private void HomeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left && CanGoPrevious)
            {
                GoToPage(_currentPageIndex - 1);
                e.Handled = true;
            }
            else if (e.Key == Key.Right && CanGoNext)
            {
                GoToPage(_currentPageIndex + 1);
                e.Handled = true;
            }
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (CanGoPrevious)
            {
                GoToPage(_currentPageIndex - 1);
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (CanGoNext)
            {
                GoToPage(_currentPageIndex + 1);
            }
        }

        private void GoToPage(int pageIndex)
        {
            _currentPageIndex = pageIndex;
            UpdatePage();
        }

        private void UpdatePage()
        {
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(PageIndicator));
            OnPropertyChanged(nameof(ProgressWidth));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static List<HomePage> CreatePages()
        {
            return new List<HomePage>
            {
                new(
                    "Bienvenido a CorexProd",
                    "Una vista rapida de las areas que reune el sistema.",
                    "CONTROL OPERATIVO",
                    "CorexProd integra ventas, almacen, produccion, destajo, reportes y seguridad en un solo panel de trabajo.",
                    "Inicia desde el menu lateral para entrar al modulo que necesitas. Cada area mantiene sus datos conectados para reducir doble registro y dar seguimiento a la operacion diaria.",
                    "#EFF6FF",
                    "#BFDBFE",
                    new List<HomeMetric>
                    {
                        new("6", "areas clave"),
                        new("ERP", "flujo central"),
                        new("PDF", "documentos")
                    },
                    new List<SystemModule>
                    {
                        new("Ventas", "Proformas, clientes, detalle de productos, importes, anulaciones y documentos PDF.", "\uE8A5", "#2563EB", "#DBEAFE"),
                        new("Almacen", "Entradas de productos e insumos, abastecimiento, stock por almacen y kardex.", "\uE7B8", "#16A34A", "#DCFCE7"),
                        new("Productos", "Catalogos, categorias, unidades de medida, stock minimo y creacion masiva.", "\uE719", "#D97706", "#FEF3C7"),
                        new("Produccion", "Fichas tecnicas por producto, versiones y control del consumo de insumos.", "\uE9D9", "#CA8A04", "#FEF9C3"),
                        new("Destajo y pagos", "Periodos, movimientos operativos, cuotas, lotes y seguimiento del trabajo.", "\uE9F9", "#7C3AED", "#EDE9FE"),
                        new("Reportes y seguridad", "Consultas de stock, usuarios, roles, permisos, empleados, empresa y proveedores.", "\uE9D2", "#E11D48", "#FFE4E6")
                    }),
                new(
                    "Gestion comercial y almacen",
                    "Herramientas para vender, registrar ingresos y conocer las existencias.",
                    "VENTAS Y STOCK",
                    "Desde una proforma hasta el movimiento de stock, la informacion queda conectada y disponible para consulta.",
                    "Revisa proformas, OCI, guias internas, entradas manuales, stock y kardex con una lectura consistente entre ventas y almacen.",
                    "#F0FDF4",
                    "#BBF7D0",
                    new List<HomeMetric>
                    {
                        new("OCI", "compras internas"),
                        new("KDX", "movimientos"),
                        new("RUC", "consultas")
                    },
                    new List<SystemModule>
                    {
                        new("Proformas agiles", "Busqueda dinamica de clientes, filtros por fecha, carga masiva desde Excel y generacion de PDF con el logo de la empresa.", "\uE8A5", "#2563EB", "#DBEAFE"),
                        new("Entradas de stock", "Registro, detalle, abastecimiento y anulacion de ingresos manuales para productos e insumos.", "\uE7B8", "#16A34A", "#DCFCE7"),
                        new("Stock y kardex", "Existencias por almacen, historial de movimientos, reversion automatica y controles para evitar inconsistencias.", "\uE9D9", "#D97706", "#FEF3C7"),
                        new("Maestros comerciales", "Gestion de clientes, proveedores, productos, insumos, categorias y unidades de medida con consultas de DNI y RUC.", "\uE719", "#7C3AED", "#EDE9FE")
                    }),
                new(
                    "Produccion y administracion",
                    "Control operativo, configuracion del negocio y acceso seguro.",
                    "PRODUCCION Y CONTROL",
                    "El sistema completa el flujo con produccion, destajo, reportes y una administracion organizada por permisos.",
                    "Usa esta vista como mapa para gestionar fichas tecnicas, seguimiento de OT, pagos operativos, configuracion empresarial y seguridad.",
                    "#FFF7ED",
                    "#FED7AA",
                    new List<HomeMetric>
                    {
                        new("OT", "seguimiento"),
                        new("FT", "fichas"),
                        new("ROL", "permisos")
                    },
                    new List<SystemModule>
                    {
                        new("Fichas tecnicas", "Definicion de productos terminados, insumos requeridos, versiones y acceso directo a la ficha en PDF.", "\uE9D9", "#CA8A04", "#FEF9C3"),
                        new("Destajo y pagos", "Control de periodos, movimientos, lotes, cuotas y reportes para el seguimiento operativo.", "\uE9F9", "#7C3AED", "#EDE9FE"),
                        new("Reportes", "Consulta y filtrado del stock disponible de productos terminados e insumos, con cantidades consolidadas.", "\uE9D2", "#2563EB", "#DBEAFE"),
                        new("Seguridad y empresa", "Administracion de usuarios, roles, permisos, empleados, cargos, datos empresariales y cambio de clave.", "\uE72E", "#E11D48", "#FFE4E6")
                    })
            };
        }
    }

    public sealed record HomePage(
        string Title,
        string Subtitle,
        string Eyebrow,
        string Introduction,
        string FocusText,
        string HighlightBackground,
        string HighlightBorder,
        List<HomeMetric> Metrics,
        List<SystemModule> Modules);

    public sealed record HomeMetric(
        string Value,
        string Label);

    public sealed record SystemModule(
        string Title,
        string Description,
        string Icon,
        string Accent,
        string IconBackground);
}
