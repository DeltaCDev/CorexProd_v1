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
        public string PageIndicator => $"Página {_currentPageIndex + 1} de {_pages.Count}";
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
                    "Una vista rápida de las áreas que reúne el sistema.",
                    "CorexProd integra la operación comercial, el control de almacén, la producción y la administración en un solo lugar.",
                    "#EFF6FF",
                    "#BFDBFE",
                    new List<SystemModule>
                    {
                        new("Ventas", "Proformas, clientes, detalle de productos, importes, anulaciones y documentos PDF.", "\uE8A5", "#2563EB", "#DBEAFE"),
                        new("Almacén", "Entradas de productos e insumos, abastecimiento, stock por almacén y kardex.", "\uE7B8", "#16A34A", "#DCFCE7"),
                        new("Productos", "Catálogos, categorías, unidades de medida, stock mínimo y creación masiva.", "\uE719", "#D97706", "#FEF3C7"),
                        new("Producción", "Fichas técnicas por producto, versiones y control del consumo de insumos.", "\uE9D9", "#CA8A04", "#FEF9C3"),
                        new("Destajo y pagos", "Periodos, movimientos operativos, cuotas, lotes y seguimiento del trabajo.", "\uE9F9", "#7C3AED", "#EDE9FE"),
                        new("Reportes y seguridad", "Consultas de stock, usuarios, roles, permisos, empleados, empresa y proveedores.", "\uE9D2", "#E11D48", "#FFE4E6")
                    }),
                new(
                    "Gestión comercial y almacén",
                    "Herramientas para vender, registrar ingresos y conocer las existencias.",
                    "Desde una proforma hasta el movimiento de stock, la información queda conectada y disponible para consulta.",
                    "#F0FDF4",
                    "#BBF7D0",
                    new List<SystemModule>
                    {
                        new("Proformas ágiles", "Búsqueda dinámica de clientes, filtros por fecha, carga masiva desde Excel y generación de PDF con el logo de la empresa.", "\uE8A5", "#2563EB", "#DBEAFE"),
                        new("Entradas de stock", "Registro, detalle, abastecimiento y anulación de ingresos manuales para productos e insumos.", "\uE7B8", "#16A34A", "#DCFCE7"),
                        new("Stock y kardex", "Existencias por almacén, historial de movimientos, reversión automática y controles para evitar inconsistencias.", "\uE9D9", "#D97706", "#FEF3C7"),
                        new("Maestros comerciales", "Gestión de clientes, proveedores, productos, insumos, categorías y unidades de medida con consultas de DNI y RUC.", "\uE719", "#7C3AED", "#EDE9FE")
                    }),
                new(
                    "Producción y administración",
                    "Control operativo, configuración del negocio y acceso seguro.",
                    "El sistema completa el flujo con producción, destajo, reportes y una administración organizada por permisos.",
                    "#FFF7ED",
                    "#FED7AA",
                    new List<SystemModule>
                    {
                        new("Fichas técnicas", "Definición de productos terminados, insumos requeridos, versiones y acceso directo a la ficha en PDF.", "\uE9D9", "#CA8A04", "#FEF9C3"),
                        new("Destajo y pagos", "Control de periodos, movimientos, lotes, cuotas y reportes para el seguimiento operativo.", "\uE9F9", "#7C3AED", "#EDE9FE"),
                        new("Reportes", "Consulta y filtrado del stock disponible de productos terminados e insumos, con cantidades consolidadas.", "\uE9D2", "#2563EB", "#DBEAFE"),
                        new("Seguridad y empresa", "Administración de usuarios, roles, permisos, empleados, cargos, datos empresariales y cambio de clave.", "\uE72E", "#E11D48", "#FFE4E6")
                    })
            };
        }
    }

    public sealed record HomePage(
        string Title,
        string Subtitle,
        string Introduction,
        string HighlightBackground,
        string HighlightBorder,
        List<SystemModule> Modules);

    public sealed record SystemModule(
        string Title,
        string Description,
        string Icon,
        string Accent,
        string IconBackground);
}
