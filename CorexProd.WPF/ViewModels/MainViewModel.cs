using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Almacen.Views;
using CorexProd.WPF.Modules.DestajoPagos.Views;
using CorexProd.WPF.Modules.Productos.Views;
using CorexProd.WPF.Modules.Produccion.Views;
using CorexProd.WPF.Modules.Reportes.Views;
using CorexProd.WPF.Modules.Seguridad.Views;
using CorexProd.WPF.Modules.Ventas.Views;
using CorexProd.WPF.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using CorexProd.Negocio.Negocio;

namespace CorexProd.WPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private UserControl _vistaActual = null!;
        private string _titulo = string.Empty;
        private string _nombreUsuario = string.Empty;
        private string _nombreRol = string.Empty;
        private bool _isSidebarCollapsed;
        private byte[]? _logoMenu;
        private byte[]? _iconoMenu;

        public string NombreRol
        {
            get => _nombreRol;
            set
            {
                _nombreRol = value;
                OnPropertyChanged();
            }
        }



        public ObservableCollection<MenuItemSistema> SidebarMenus { get; set; } = [];

        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set
            {
                _isSidebarCollapsed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SidebarWidth));
                OnPropertyChanged(nameof(SidebarExpandedVisibility));
                OnPropertyChanged(nameof(SidebarCollapsedVisibility));
                OnPropertyChanged(nameof(SidebarToggleText));
                OnPropertyChanged(nameof(SidebarToggleToolTip));
            }
        }

        public GridLength SidebarWidth => IsSidebarCollapsed ? new GridLength(64) : new GridLength(250);

        public Visibility SidebarExpandedVisibility => IsSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;

        public Visibility SidebarCollapsedVisibility => IsSidebarCollapsed ? Visibility.Visible : Visibility.Collapsed;

        public string SidebarToggleText => IsSidebarCollapsed ? ">>" : "<<";

        public string SidebarToggleToolTip => IsSidebarCollapsed ? "Expandir menu" : "Minimizar menu";

        public byte[]? LogoMenu
        {
            get => _logoMenu;
            set
            {
                _logoMenu = value;
                OnPropertyChanged();
            }
        }

        public byte[]? IconoMenu
        {
            get => _iconoMenu;
            set
            {
                _iconoMenu = value;
                OnPropertyChanged();
            }
        }

        public bool OmitirConfirmacionCierre { get; private set; }

        public UserControl VistaActual
        {
            get => _vistaActual;
            set
            {
                _vistaActual = value;
                OnPropertyChanged();
            }
        }

        public string Titulo
        {
            get => _titulo;
            set
            {
                _titulo = value;
                OnPropertyChanged();
            }
        }

        public string NombreUsuario
        {
            get => _nombreUsuario;
            set
            {
                _nombreUsuario = value;
                OnPropertyChanged();
            }
        }

        public ICommand IrInicioCommand { get; }
        public ICommand IrVistaCommand { get; }
        public ICommand CerrarSesionCommand { get; }
        public ICommand CambiarClaveCommand { get; }
        public ICommand ToggleSidebarCommand { get; }

        public MainViewModel()
        {
            NombreUsuario = SessionManager.UsuarioActual?.NombreCompleto ?? "Usuario";
            NombreRol = SessionManager.UsuarioActual?.NombreRol ?? "Sin rol";
            CerrarSesionCommand = new RelayCommand(_ => CerrarSesion());
            CambiarClaveCommand = new RelayCommand(_ => CambiarClave());
            ToggleSidebarCommand = new RelayCommand(_ => ToggleSidebar());

            IrInicioCommand = new RelayCommand(_ => IrInicio());
            IrVistaCommand = new RelayCommand(parametro => IrVista(parametro?.ToString() ?? string.Empty));

            CargarMenus();
            CargarMarcaEmpresa();

            IrInicio();
        }

        private void CargarMarcaEmpresa()
        {
            try
            {
                Empresa? empresa = new EmpresaNegocio().ObtenerPredeterminada();
                LogoMenu = empresa?.Logo ?? CargarRecursoImagen("Images/LOGO.png");
                IconoMenu = empresa?.Icono ?? CargarRecursoImagen("Assets/CorexProd.ico") ?? LogoMenu;
            }
            catch
            {
                LogoMenu = CargarRecursoImagen("Images/LOGO.png");
                IconoMenu = CargarRecursoImagen("Assets/CorexProd.ico") ?? LogoMenu;
            }
        }

        private static byte[]? CargarRecursoImagen(string rutaRelativa)
        {
            try
            {
                string ruta = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rutaRelativa);
                if (System.IO.File.Exists(ruta))
                    return System.IO.File.ReadAllBytes(ruta);

                System.Windows.Resources.StreamResourceInfo? recurso =
                    Application.GetResourceStream(new System.Uri($"pack://application:,,,/{rutaRelativa}", System.UriKind.Absolute));

                if (recurso?.Stream == null)
                    return null;

                using System.IO.MemoryStream memoria = new();
                recurso.Stream.CopyTo(memoria);
                return memoria.ToArray();
            }
            catch
            {
                return null;
            }
        }
        private void CerrarSesion()
        {
            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Desea cerrar sesión?",
                "Cerrar sesión");

            if (!confirmar)
            {
                return;
            }

            SessionManager.CerrarSesion();

            LoginView loginView = new();
            Window? ventanaPrincipal = Application.Current.MainWindow;

            OmitirConfirmacionCierre = true;
            loginView.Show();

            ventanaPrincipal?.Close();
            Application.Current.MainWindow = loginView;
        }
        private void CambiarClave()
        {
            Titulo = "Cambiar Clave";
            VistaActual = new CambiarClaveView();
        }

        private void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        private void CargarMenus()
        {
            SidebarMenus.Clear();

            var menusPermitidos =
                SessionManager.MenusPermitidos ?? [];

            var ordenMenus = ObtenerOrdenMenus();
            List<MenuItemSistema> menusConstruidos = [];

            // VENTAS
            MenuItemSistema ventas = new()
            {
                Titulo = "Ventas",
                EsPadre = true
            };

            if (menusPermitidos.Contains("Proformas"))
            {
                ventas.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Proformas",
                    Vista = "Proformas"
                });
            }

            if (menusPermitidos.Contains("OCI"))
            {
                ventas.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "OCI",
                    Vista = "OCI"
                });
            }

            if (menusPermitidos.Contains("Orden de Trabajo"))
            {
                ventas.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Orden de Trabajo",
                    Vista = "OT"
                });
            }

            if (menusPermitidos.Contains("Guía de Salida"))
            {
                ventas.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Guía Interna",
                    Vista = "GuiaSalida"
                });
            }

            if (menusPermitidos.Contains("Ventas") && ventas.Hijos.Count > 0)
            {
                menusConstruidos.Add(ventas);
            }

            // ALMACÉN
            MenuItemSistema almacen = new()
            {
                Titulo = "Almacén",
                EsPadre = true
            };

            if (menusPermitidos.Contains("Unidades de Medida"))
            {
                almacen.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Unidades de Medida",
                    Vista = "UnidadesMedida"
                });
            }

            if (menusPermitidos.Contains("Categorías de Insumos"))
            {
                almacen.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Categorías de Insumos",
                    Vista = "CategoriasInsumo"
                });
            }

            if (menusPermitidos.Contains("Insumos"))
            {
                almacen.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Insumos",
                    Vista = "Insumos"
                });
            }

            if (menusPermitidos.Contains("Entrada Manual de Productos") || menusPermitidos.Contains("Ingresos Manuales de Stock"))
            {
                almacen.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Entrada Manual de Productos",
                    Vista = "IngresosManualesStock"
                });
            }

            if (menusPermitidos.Contains("Entrada Manual de Insumos") || menusPermitidos.Contains("Ingresos de Stock de Insumos"))
            {
                almacen.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Entrada Manual de Insumos",
                    Vista = "IngresosManualesStockInsumos"
                });
            }

            if (menusPermitidos.Contains("Ficha Técnica"))
            {
                almacen.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Ficha Técnica",
                    Vista = "FichaTecnica"
                });
            }

            if (menusPermitidos.Contains("Almacén") && almacen.Hijos.Count > 0)
            {
                menusConstruidos.Add(almacen);
            }

            // PRODUCTOS
            MenuItemSistema productos = new()
            {
                Titulo = "Productos",
                EsPadre = true
            };

            if (menusPermitidos.Contains("Categorías de Productos"))
            {
                productos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Categorías de Productos",
                    Vista = "CategoriasProducto"
                });
            }

            if (menusPermitidos.Contains("Productos"))
            {
                productos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Productos",
                    Vista = "Productos"
                });
            }

            if (menusPermitidos.Contains("Productos") && productos.Hijos.Count > 0)
            {
                menusConstruidos.Add(productos);
            }

            // PRODUCCIÓN
            MenuItemSistema produccion = new()
            {
                Titulo = "Producción",
                EsPadre = true
            };

            if (menusPermitidos.Contains("Áreas de Producción"))
            {
                produccion.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Áreas de Producción",
                    Vista = "AreasProduccion"
                });
            }

            if (menusPermitidos.Contains("Seguimiento OT"))
            {
                produccion.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Seguimiento OT",
                    Vista = "SeguimientoOT"
                });
            }

            if (menusPermitidos.Contains("Producción") && produccion.Hijos.Count > 0)
            {
                menusConstruidos.Add(produccion);
            }

            // DESTAJO Y PAGOS
            MenuItemSistema destajoPagos = new()
            {
                Titulo = "Destajo y Pagos",
                EsPadre = true
            };

            if (menusPermitidos.Contains("Panel de Destajo"))
            {
                destajoPagos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Panel de Destajo",
                    Vista = "PanelDestajo"
                });
            }

            if (menusPermitidos.Contains("Periodos de Pago"))
            {
                destajoPagos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Periodos de Pago",
                    Vista = "PeriodosPago"
                });
            }

            if (menusPermitidos.Contains("Movimientos Operativos"))
            {
                destajoPagos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Movimientos Operativos",
                    Vista = "MovimientosOperativos"
                });
            }

            if (menusPermitidos.Contains("Prestamos y Cuotas") || menusPermitidos.Contains("Préstamos y Cuotas"))
            {
                destajoPagos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Prestamos y Cuotas",
                    Vista = "PrestamosCuotas"
                });
            }

            if (menusPermitidos.Contains("Lotes de Pago"))
            {
                destajoPagos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Lotes de Pago",
                    Vista = "LotesPago"
                });
            }

            if (menusPermitidos.Contains("Reportes de Pagos"))
            {
                destajoPagos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Reportes de Pagos",
                    Vista = "ReportesPagos"
                });
            }

            // Buscamos exactamente la palabra "Configuración" como está en tu Base de Datos (IdMenu 31)
            if (menusPermitidos.Contains("Configuración"))
            {
                destajoPagos.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Configuración",
                    Vista = "ConfiguracionPagos"
                });
            }

            if (menusPermitidos.Contains("Destajo y Pagos") && destajoPagos.Hijos.Count > 0)
            {
                menusConstruidos.Add(destajoPagos);
            }

            // REPORTES
            MenuItemSistema reportes = new()
            {
                Titulo = "Reportes",
                EsPadre = true
            };

            if (menusPermitidos.Contains("Stock Productos"))
            {
                reportes.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Stock Productos",
                    Vista = "StockProductos"
                });
            }

            if (menusPermitidos.Contains("Stock Insumos"))
            {
                reportes.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Stock Insumos",
                    Vista = "StockInsumos"
                });
            }

            if (menusPermitidos.Contains("Kardex Productos"))
            {
                reportes.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Kardex Productos",
                    Vista = "KardexProductos"
                });
            }

            if (menusPermitidos.Contains("Kardex Insumos"))
            {
                reportes.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Kardex Insumos",
                    Vista = "KardexInsumos"
                });
            }

            if (menusPermitidos.Contains("Reportes"))
            {
                reportes.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Estadísticas",
                    Vista = "Reportes"
                });
            }

            if (menusPermitidos.Contains("Reportes") && reportes.Hijos.Count > 0)
            {
                menusConstruidos.Add(reportes);
            }

            // SEGURIDAD
            MenuItemSistema seguridad = new()
            {
                Titulo = "Seguridad",
                EsPadre = true
            };

            if (menusPermitidos.Contains("Roles"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Roles",
                    Vista = "Roles"
                });
            }

            if (menusPermitidos.Contains("Usuarios"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Usuarios",
                    Vista = "Usuarios"
                });
            }

            if (menusPermitidos.Contains("Empleados"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Empleados",
                    Vista = "Empleados"
                });
            }

            if (menusPermitidos.Contains("Clientes"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Clientes",
                    Vista = "Clientes"
                });
            }

            if (menusPermitidos.Contains("Proveedores"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Proveedores",
                    Vista = "Proveedores"
                });
            }

            if (menusPermitidos.Contains("Empresa"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Empresa",
                    Vista = "Empresa"
                });
            }

            if (menusPermitidos.Contains("Cargos"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Cargos",
                    Vista = "Cargos"
                });
            }

            if (menusPermitidos.Contains("Parámetros"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Parámetros",
                    Vista = "Parametros"
                });
            }

            if (menusPermitidos.Contains("Series y Correlativos"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Series y Correlativos",
                    Vista = "SeriesCorrelativos"
                });
            }

            if (menusPermitidos.Contains("Menú") || menusPermitidos.Contains("Menu"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Menú",
                    Vista = "Menus"
                });
            }

            if (menusPermitidos.Contains("Seguridad") && seguridad.Hijos.Count > 0)
            {
                menusConstruidos.Add(seguridad);
            }

            foreach (MenuItemSistema menu in menusConstruidos)
            {
                OrdenarHijos(menu, ordenMenus);
            }

            foreach (MenuItemSistema menu in menusConstruidos
                .OrderBy(x => ObtenerOrden(ordenMenus, null, x.Titulo)))
            {
                SidebarMenus.Add(menu);
            }
        }

        private static void OrdenarHijos(MenuItemSistema menu, Dictionary<string, int> ordenMenus)
        {
            var hijosOrdenados = menu.Hijos
                .OrderBy(x => ObtenerOrden(ordenMenus, menu.Titulo, x.Titulo))
                .ToList();

            menu.Hijos.Clear();

            foreach (MenuItemSistema hijo in hijosOrdenados)
            {
                menu.Hijos.Add(hijo);
            }
        }

        private static Dictionary<string, int> ObtenerOrdenMenus()
        {
            try
            {
                return new MenuSistemaNegocio()
                    .Listar()
                    .GroupBy(x => LlaveMenu(string.IsNullOrWhiteSpace(x.NombrePadre) ? null : x.NombrePadre, x.NombreMenu))
                    .ToDictionary(x => x.Key, x => x.First().Orden);
            }
            catch
            {
                return new Dictionary<string, int>
                {
                    [LlaveMenu(null, "Ventas")] = 2,
                    [LlaveMenu(null, "Producción")] = 3,
                    [LlaveMenu(null, "Reportes")] = 4,
                    [LlaveMenu(null, "Almacén")] = 5,
                    [LlaveMenu(null, "Productos")] = 6,
                    [LlaveMenu(null, "Destajo y Pagos")] = 7,
                    [LlaveMenu(null, "Seguridad")] = 8
                };
            }
        }

        private static int ObtenerOrden(Dictionary<string, int> ordenMenus, string? padre, string titulo)
        {
            return ordenMenus.TryGetValue(LlaveMenu(padre, titulo), out int orden) ? orden : 999;
        }

        private static string LlaveMenu(string? padre, string titulo)
        {
            return $"{NormalizarMenu(padre ?? string.Empty)}|{NormalizarMenu(titulo)}";
        }

        private static string NormalizarMenu(string texto)
        {
            return texto
                .Replace("Menú", "Menu")
                .Trim();
        }

        private void IrInicio()
        {
            Titulo = "Panel principal";
            VistaActual = new HomeView();
        }

        private void IrVista(string vista)
        {
            switch (vista)
            {
                // SEGURIDAD
                case "Roles":
                    Titulo = "Roles";
                    VistaActual = new RolesView();
                    break;

                case "Usuarios":
                    Titulo = "Usuarios";
                    VistaActual = new UsuariosView();
                    break;

                case "Empleados":
                    Titulo = "Empleados";
                    VistaActual = new EmpleadosView();
                    break;

                case "Clientes":
                    Titulo = "Clientes";
                    VistaActual = new ClientesView();
                    break;

                case "Proveedores":
                    Titulo = "Proveedores";
                    VistaActual = new ProveedoresView();
                    break;

                case "Empresa":
                    Titulo = "Empresa";
                    VistaActual = new EmpresasView();
                    break;

                case "Cargos":
                    Titulo = "Cargos";
                    VistaActual = new CargosView();
                    break;

                case "Parametros":
                    Titulo = "Parámetros";
                    VistaActual = new ParametrosView();
                    break;

                case "SeriesCorrelativos":
                    Titulo = "Series y Correlativos";
                    VistaActual = new SeriesCorrelativosView();
                    break;

                case "Menus":
                    Titulo = "Menú";
                    VistaActual = new MenusView();
                    break;

                // VENTAS
                case "Ventas":
                    Titulo = vista;
                    VistaActual = new VentasView();
                    break;

                case "GuiaSalida":
                    Titulo = "Guía Interna";
                    VistaActual = new GuiasInternasView();
                    break;

                case "OCI":
                    Titulo = "Órdenes de Compra Interna";
                    VistaActual = new OrdenesCompraInternaView();
                    break;

                case "Proformas":
                    Titulo = "Proformas";
                    VistaActual = new ProformasView();
                    break;

                // ALMACÉN
                case "UnidadesMedida":
                    Titulo = "Unidades de Medida";
                    VistaActual = new UnidadesMedidaView();
                    break;

                case "CategoriasInsumo":
                    Titulo = "Categorías de Insumos";
                    VistaActual = new CategoriasInsumoView();
                    break;

                case "Insumos":
                    Titulo = "Insumos";
                    VistaActual = new InsumosView();
                    break;

                case "IngresosManualesStock":
                    Titulo = "Entrada Manual de Productos";
                    VistaActual = new IngresosManualesStockView();
                    break;

                case "IngresosManualesStockInsumos":
                    Titulo = "Entrada Manual de Insumos";
                    VistaActual = new IngresosManualesStockInsumosView();
                    break;

                case "Almacén":

                case "FichaTecnica":
                    Titulo = "Ficha Técnica";
                    VistaActual = new FichaTecnicaView();
                    break;


                // PRODUCTOS
                case "CategoriasProducto":
                    Titulo = "Categorías de Productos";
                    VistaActual = new CategoriasProductosView();
                    break;

                case "Productos":
                    Titulo = "Productos";
                    VistaActual = new ProductosView();
                    break;

                // PRODUCCIÓN
                case "Producción":
                    Titulo = "Producción";
                    VistaActual = new ProduccionView();
                    break;
                case "AreasProduccion":
                    Titulo = "Áreas de Producción";
                    VistaActual = new AreasProduccionView();
                    break;
                case "OT":
                    Titulo = "Orden de Trabajo";
                    VistaActual = new ProduccionView();
                    break;
                case "SeguimientoOT":
                    Titulo = "Seguimiento OT";
                    VistaActual = new ProduccionView();
                    break;

                // DESTAJO Y PAGOS
                case "PanelDestajo":
                    Titulo = "Panel de Destajo";
                    VistaActual = new DestajoPagosView(1);
                    break;

                case "PeriodosPago":
                    Titulo = "Periodos de Pago";
                    VistaActual = new DestajoPagosView(0);
                    break;

                case "MovimientosOperativos":
                    Titulo = "Movimientos Operativos";
                    VistaActual = new DestajoPagosView(1);
                    break;

                case "PrestamosCuotas":
                    Titulo = "Prestamos y Cuotas";
                    VistaActual = new DestajoPagosView(2);
                    break;

                case "LotesPago":
                    Titulo = "Lotes de Pago";
                    VistaActual = new DestajoPagosView(3);
                    break;

                case "ReportesPagos":
                    Titulo = "Reportes de Pagos";
                    VistaActual = new DestajoPagosView(4);
                    break;

                case "ConfiguracionPagos":
                    Titulo = "Configuración de Pagos";
                    VistaActual = new DestajoPagosView(5); // El índice 5 es la 6ta pestaña
                    break;

                case "DestajoPagos":
                    Titulo = "Control de Destajo y Pagos Operativos";
                    VistaActual = new DestajoPagosView();
                    break;

                // REPORTES
                case "StockProductos":
                    Titulo = "Stock Productos Terminados";
                    VistaActual = new StockProductosView();
                    break;

                case "StockInsumos":
                    Titulo = "Stock Insumos";
                    VistaActual = new StockInsumosView();
                    break;

                case "Reportes":
                    Titulo = "Reportes";
                    VistaActual = new ReportesView();
                    break;

                case "KardexProductos":
                    Titulo = "Kardex Productos";
                    VistaActual = new KardexProductosView();
                    break;

                case "KardexInsumos":
                    Titulo = "Kardex Insumos";
                    VistaActual = new KardexInsumosView();
                    break;

                default:
                    IrInicio();
                    break;
            }
        }
    }
}
