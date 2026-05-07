using System.Collections.ObjectModel;

namespace CorexProd.Entidad.Entidades
{
    public class MenuItemSistema
    {
        public string Titulo { get; set; } = string.Empty;

        public string Vista { get; set; } = string.Empty;

        public bool EsPadre { get; set; }

        public ObservableCollection<MenuItemSistema> Hijos
        {
            get;
            set;
        } = [];
    }
}