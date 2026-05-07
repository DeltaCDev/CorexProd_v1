using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Almacen.Views;
using CorexProd.WPF.Modules.Productos.Views;
using CorexProd.WPF.Modules.Produccion.Views;
using CorexProd.WPF.Modules.Reportes.Views;
using CorexProd.WPF.Modules.Seguridad.Views;
using CorexProd.WPF.Modules.Ventas.Views;
using CorexProd.WPF.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace CorexProd.WPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private UserControl _vistaActual = null!;
        private string _titulo = string.Empty;
        private string _nombreUsuario = string.Empty;
        private string _nombreRol = string.Empty;

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

        public MainViewModel()
        {
            NombreUsuario = SessionManager.UsuarioActual?.NombreCompleto ?? "Usuario";
            NombreRol = SessionManager.UsuarioActual?.NombreRol ?? "Sin rol";
            CerrarSesionCommand = new RelayCommand(_ => CerrarSesion());
            CambiarClaveCommand = new RelayCommand(_ => CambiarClave());

            IrInicioCommand = new RelayCommand(_ => IrInicio());
            IrVistaCommand = new RelayCommand(parametro => IrVista(parametro?.ToString() ?? string.Empty));

            CargarMenus();

            IrInicio();
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
            loginView.Show();

            Application.Current.MainWindow?.Close();
            Application.Current.MainWindow = loginView;
        }
        private void CambiarClave()
        {
            Titulo = "Cambiar Clave";
            VistaActual = new CambiarClaveView();
        }

        private void CargarMenus()
        {
            SidebarMenus.Clear();

            var menusPermitidos =
                SessionManager.MenusPermitidos ?? [];

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

            if (menusPermitidos.Contains("Guía de Salida"))
            {
                ventas.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Guía de Salida",
                    Vista = "GuiaSalida"
                });
            }

            if (menusPermitidos.Contains("Ventas") && ventas.Hijos.Count > 0)
            {
                SidebarMenus.Add(ventas);
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

            if (menusPermitidos.Contains("Insumos"))
            {
                almacen.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Insumos",
                    Vista = "Insumos"
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
                SidebarMenus.Add(almacen);
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
                SidebarMenus.Add(productos);
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

            if (menusPermitidos.Contains("Orden de Trabajo"))
            {
                produccion.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Orden de Trabajo",
                    Vista = "OT"
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
                SidebarMenus.Add(produccion);
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

            if (menusPermitidos.Contains("Reportes") && reportes.Hijos.Count > 0)
            {
                SidebarMenus.Add(reportes);
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

            if (menusPermitidos.Contains("Cargos"))
            {
                seguridad.Hijos.Add(new MenuItemSistema
                {
                    Titulo = "Cargos",
                    Vista = "Cargos"
                });
            }

            if (menusPermitidos.Contains("Seguridad") && seguridad.Hijos.Count > 0)
            {
                SidebarMenus.Add(seguridad);
            }
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

                case "Cargos":
                    Titulo = "Cargos";
                    VistaActual = new CargosView();
                    break;

                // VENTAS
                case "Ventas":
                case "Proformas":
                case "OCI":
                case "GuiaSalida":
                    Titulo = vista;
                    VistaActual = new VentasView();
                    break;

                // ALMACÉN
                case "Almacén":
                case "UnidadesMedida":
                case "Insumos":
                case "FichaTecnica":
                    Titulo = vista;
                    VistaActual = new AlmacenView();
                    break;

                // PRODUCTOS
                case "Productos":
                case "CategoriasProducto":
                    Titulo = vista;
                    VistaActual = new ProductosView();
                    break;

                // PRODUCCIÓN
                case "Producción":
                case "AreasProduccion":
                case "OT":
                case "SeguimientoOT":
                    Titulo = vista;
                    VistaActual = new ProduccionView();
                    break;

                // REPORTES
                case "Reportes":
                case "StockProductos":
                case "StockInsumos":
                case "KardexProductos":
                case "KardexInsumos":
                    Titulo = vista;
                    VistaActual = new ReportesView();
                    break;

                default:
                    IrInicio();
                    break;
            }
        }
    }
}