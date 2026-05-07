using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CorexProd.Entidad.Entidades
{
    public class MenuSistema : INotifyPropertyChanged
    {
        private bool _tienePermiso;

        public int IdMenu { get; set; }

        public string NombreMenu { get; set; } = string.Empty;

        public int? IdMenuPadre { get; set; }

        public int Orden { get; set; }

        public bool TienePermiso
        {
            get => _tienePermiso;
            set
            {
                _tienePermiso = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MenuSistema> Hijos { get; set; } = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }
}