using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class RolesViewModel : BaseViewModel
    {
        private readonly RolNegocio _rolNegocio;
        private readonly PermisoMenuRolNegocio _permisoMenuRolNegocio;

        private int _idRol;
        private string _nombreRol = string.Empty;
        private bool _estado = true;
        private Rol? _rolSeleccionado;

        public ObservableCollection<Rol> Roles { get; set; } = [];
        public ObservableCollection<MenuSistema> Menus { get; set; } = [];
        public ICommand GuardarPermisosCommand { get; }
        public ICommand SeleccionarTodoCommand { get; }
        public ICommand QuitarSeleccionCommand { get; }

        private Rol? _rolOrigenSeleccionado;

        public ObservableCollection<Rol> RolesParaCopiar { get; set; } = [];

        public Rol? RolOrigenSeleccionado
        {
            get => _rolOrigenSeleccionado;
            set
            {
                _rolOrigenSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public ICommand CopiarPermisosCommand { get; }

        public bool PermisoOrdenTrabajo
        {
            get => ObtenerPermiso("Orden de Trabajo");
            set => EstablecerPermiso("Orden de Trabajo", value);
        }

        public bool PermisoGuiaInterna
        {
            get => ObtenerPermiso("Guía de Salida");
            set => EstablecerPermiso("Guía de Salida", value);
        }

        public bool PermisoTransferenciasOt
        {
            get => ObtenerPermiso("Orden de Trabajo");
            set => EstablecerPermiso("Orden de Trabajo", value);
        }

        public int IdRol
        {
            get => _idRol;
            set
            {
                _idRol = value;
                OnPropertyChanged();
            }
        }

        public string NombreRol
        {
            get => _nombreRol;
            set
            {
                _nombreRol = value;
                OnPropertyChanged();
            }
        }

        public bool Estado
        {
            get => _estado;
            set
            {
                _estado = value;
                OnPropertyChanged();
            }
        }

        public Rol? RolSeleccionado
        {
            get => _rolSeleccionado;
            set
            {
                _rolSeleccionado = value;
                OnPropertyChanged();

                if (value != null)
                {
                    IdRol = value.IdRol;
                    NombreRol = value.NombreRol;
                    Estado = value.Estado;

                    CargarMenusPorRol(value.IdRol);
                    NotificarPermisosOperativos();
                }
            }
        }

        public ICommand GuardarCommand { get; }

        public RolesViewModel()
        {
            _rolNegocio = new RolNegocio();

            _permisoMenuRolNegocio =
                new PermisoMenuRolNegocio();

            GuardarCommand =
                new RelayCommand(_ => Guardar());

            GuardarPermisosCommand =
                new RelayCommand(_ => GuardarPermisos());

            SeleccionarTodoCommand = 
                new RelayCommand(_ => SeleccionarTodo());

            QuitarSeleccionCommand = 
                new RelayCommand(_ => QuitarSeleccion());

            CopiarPermisosCommand = 
                new RelayCommand(_ => CopiarPermisos());

            CargarRoles();
        }

        private void CargarRoles()
        {
            Roles.Clear();

            var lista = _rolNegocio.Listar();

            foreach (var item in lista)
            {
                Roles.Add(item);
            }

            RolesParaCopiar.Clear();

            foreach (var item in lista)
            {
                RolesParaCopiar.Add(item);
            }
        }

        private void Guardar()
        {
            if (IdRol > 0)
            {
                bool confirmar = ConfirmDialogService.Confirmar(
                    "¿Desea actualizar la información del rol?",
                    "Confirmar actualización");

                if (!confirmar)
                {
                    return;
                }
            }

            Rol rol = new()
            {
                IdRol = IdRol,
                NombreRol = NombreRol,
                Estado = Estado
            };

            _rolNegocio.Guardar(rol);

            NotificationService.Success(
                IdRol == 0
                    ? "Rol registrado correctamente."
                    : "Rol actualizado correctamente."
            );

            Limpiar();
            CargarRoles();
        }

        private void Limpiar()
        {
            IdRol = 0;
            NombreRol = string.Empty;
            Estado = true;
            RolSeleccionado = null;
        }

        private void CargarMenusPorRol(int idRol)
        {
            Menus.Clear();

            var listaPlano =
                _permisoMenuRolNegocio
                .ListarMenusPorRol(idRol);

            var padres = listaPlano
                .Where(x => x.IdMenuPadre == null)
                .OrderBy(x => x.Orden)
                .ToList();

            foreach (var padre in padres)
            {
                var hijos = listaPlano
                    .Where(x => x.IdMenuPadre == padre.IdMenu)
                    .OrderBy(x => x.Orden)
                    .ToList();

                foreach (var hijo in hijos)
                {
                    padre.Hijos.Add(hijo);
                }

                Menus.Add(padre);
            }

            NotificarPermisosOperativos();
        }

        private void GuardarPermisos()
        {
            if (IdRol == 0)
            {
                NotificationService.Warning("Primero seleccione un rol.");
                return;
            }

            bool confirmar = ConfirmDialogService.Confirmar(
                "¿Desea guardar los permisos asignados a este rol?",
                "Confirmar permisos");

            if (!confirmar)
            {
                return;
            }

            List<MenuSistema> menusGuardar = [];

            foreach (var padre in Menus)
            {
                menusGuardar.Add(padre);

                foreach (var hijo in padre.Hijos)
                {
                    menusGuardar.Add(hijo);
                }
            }

            _permisoMenuRolNegocio.GuardarPermisos(
                IdRol,
                menusGuardar
            );

            NotificationService.Success("Permisos guardados correctamente.");
        }
        private void SeleccionarTodo()
        {
            foreach (var padre in Menus)
            {
                padre.TienePermiso = true;

                foreach (var hijo in padre.Hijos)
                {
                    hijo.TienePermiso = true;
                }
            }

            OnPropertyChanged(nameof(Menus));
            NotificarPermisosOperativos();
        }

        private void QuitarSeleccion()
        {
            foreach (var padre in Menus)
            {
                padre.TienePermiso = false;

                foreach (var hijo in padre.Hijos)
                {
                    hijo.TienePermiso = false;
                }
            }

            OnPropertyChanged(nameof(Menus));
            NotificarPermisosOperativos();
        }

        private void CopiarPermisos()
        {
            if (IdRol == 0)
            {
                NotificationService.Warning(
                    "Primero seleccione el rol al que desea copiar permisos."
                );

                return;
            }

            if (RolOrigenSeleccionado == null)
            {
                NotificationService.Warning(
                    "Seleccione un rol origen para copiar permisos."
                );

                return;
            }

            if (RolOrigenSeleccionado.IdRol == IdRol)
            {
                NotificationService.Warning(
                    "No puede copiar permisos desde el mismo rol seleccionado."
                );

                return;
            }

            var permisosOrigen =
                _permisoMenuRolNegocio
                .ListarMenusPorRol(RolOrigenSeleccionado.IdRol);

            foreach (var padre in Menus)
            {
                var permisoPadre =
                    permisosOrigen.FirstOrDefault(x => x.IdMenu == padre.IdMenu);

                if (permisoPadre != null)
                {
                    padre.TienePermiso = permisoPadre.TienePermiso;
                }

                foreach (var hijo in padre.Hijos)
                {
                    var permisoHijo =
                        permisosOrigen.FirstOrDefault(x => x.IdMenu == hijo.IdMenu);

                    if (permisoHijo != null)
                    {
                        hijo.TienePermiso = permisoHijo.TienePermiso;
                    }
                }
            }

            NotificationService.Success(
                $"Permisos copiados desde el rol {RolOrigenSeleccionado.NombreRol}. Recuerde guardar permisos."
            );

            OnPropertyChanged(nameof(Menus));
            NotificarPermisosOperativos();
        }

        private bool ObtenerPermiso(string nombreMenu)
        {
            return Menus.SelectMany(x => x.Hijos.Append(x))
                .Any(x => x.NombreMenu == nombreMenu && x.TienePermiso);
        }

        private void EstablecerPermiso(string nombreMenu, bool valor)
        {
            MenuSistema? menu = Menus.SelectMany(x => x.Hijos.Append(x))
                .FirstOrDefault(x => x.NombreMenu == nombreMenu);

            if (menu == null)
                return;

            menu.TienePermiso = valor;
            OnPropertyChanged(nameof(Menus));
            NotificarPermisosOperativos();
        }

        private void NotificarPermisosOperativos()
        {
            OnPropertyChanged(nameof(PermisoOrdenTrabajo));
            OnPropertyChanged(nameof(PermisoGuiaInterna));
            OnPropertyChanged(nameof(PermisoTransferenciasOt));
        }

    }
}
