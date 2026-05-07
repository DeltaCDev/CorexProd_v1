using System.Collections.ObjectModel;

namespace CorexProd.Entidad.Entidades
{
    public class MenuSistema
    {
        public int IdMenu { get; set; }

        public string NombreMenu { get; set; } = string.Empty;

        public int? IdMenuPadre { get; set; }

        public int Orden { get; set; }

        public bool TienePermiso { get; set; }

        public ObservableCollection<MenuSistema> Hijos { get; set; } = [];
    }
}