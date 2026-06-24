using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Seguridad.ViewModels
{
    public class MenusViewModel : BaseViewModel
    {
        private readonly MenuSistemaNegocio _menuNegocio = new();
        private MenuSistema? _menuSeleccionado;

        public ObservableCollection<MenuSistema> Menus { get; } = [];

        public MenuSistema? MenuSeleccionado
        {
            get => _menuSeleccionado;
            set
            {
                _menuSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand SubirCommand { get; }
        public ICommand BajarCommand { get; }

        public MenusViewModel()
        {
            GuardarCommand = new RelayCommand(_ => Guardar());
            ActualizarCommand = new RelayCommand(_ => Cargar());
            SubirCommand = new RelayCommand(_ => Mover(-1));
            BajarCommand = new RelayCommand(_ => Mover(1));

            Cargar();
        }

        private void Cargar()
        {
            Menus.Clear();

            foreach (MenuSistema menu in _menuNegocio.Listar())
            {
                Menus.Add(menu);
            }
        }

        private void Mover(int direccion)
        {
            if (MenuSeleccionado == null)
            {
                NotificationService.Warning("Seleccione un menú para mover.");
                return;
            }

            var grupo = Menus
                .Where(x => x.IdMenuPadre == MenuSeleccionado.IdMenuPadre)
                .OrderBy(x => x.Orden)
                .ThenBy(x => x.NombreMenu)
                .ToList();

            int indiceActual = grupo.FindIndex(x => x.IdMenu == MenuSeleccionado.IdMenu);
            int indiceNuevo = indiceActual + direccion;

            if (indiceActual < 0 || indiceNuevo < 0 || indiceNuevo >= grupo.Count)
            {
                return;
            }

            (grupo[indiceActual].Orden, grupo[indiceNuevo].Orden) =
                (grupo[indiceNuevo].Orden, grupo[indiceActual].Orden);

            MenuSistema seleccionado = MenuSeleccionado;
            ReordenarVisualmente();
            MenuSeleccionado = Menus.FirstOrDefault(x => x.IdMenu == seleccionado.IdMenu);
        }

        private void ReordenarVisualmente()
        {
            var ordenados = Menus
                .OrderBy(x => x.IdMenuPadre == null ? x.Orden : Menus.FirstOrDefault(p => p.IdMenu == x.IdMenuPadre)?.Orden ?? 999)
                .ThenBy(x => x.IdMenuPadre == null ? 0 : 1)
                .ThenBy(x => x.Orden)
                .ThenBy(x => x.NombreMenu)
                .ToList();

            Menus.Clear();

            foreach (MenuSistema menu in ordenados)
            {
                Menus.Add(menu);
            }
        }

        private void Guardar()
        {
            _menuNegocio.GuardarOrdenes(Menus);
            NotificationService.Success("Orden de menús guardado correctamente. Vuelva a iniciar sesión para refrescar permisos si cambió estados.");
            Cargar();
        }
    }
}
