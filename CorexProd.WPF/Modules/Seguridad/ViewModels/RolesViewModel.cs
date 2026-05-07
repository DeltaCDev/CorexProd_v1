using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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
            Rol rol = new()
            {
                IdRol = IdRol,
                NombreRol = NombreRol,
                Estado = Estado
            };

            _rolNegocio.Guardar(rol);

            MessageBox.Show(
                IdRol == 0 ? "Rol registrado correctamente." : "Rol actualizado correctamente.",
                "CorexProd",
                MessageBoxButton.OK,
                MessageBoxImage.Information
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
        }

        private void GuardarPermisos()
        {
            if (IdRol == 0)
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
            MessageBox.Show(
                      "Permisos guardados correctamente.",
                      "CorexProd",
                      MessageBoxButton.OK,
                      MessageBoxImage.Information
                  );
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
        }

        private void CopiarPermisos()
        {
            if (RolOrigenSeleccionado == null)
            {
                MessageBox.Show(
                    "Seleccione un rol origen para copiar permisos.",
                    "CorexProd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
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

            MessageBox.Show(
                        $"Permisos copiados desde el rol {RolOrigenSeleccionado.NombreRol}. Recuerde presionar Guardar Permisos.",
                        "CorexProd",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

            OnPropertyChanged(nameof(Menus));
        }

    }
}