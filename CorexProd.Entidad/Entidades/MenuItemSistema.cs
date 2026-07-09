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

        public string Icono => Titulo switch
        {
            "Ventas" => "\uE8A5",
            "Producción" => "\uE9F5",
            "Reportes" => "\uE9D2",
            "Almacén" => "\uE8B7",
            "Productos" => "\uE719",
            "Destajo y Pagos" => "\uE8C7",
            "Seguridad" => "\uE72E",
            "Configuración" => "\uE713",
            _ => "\uE8B7"
        };

        public string ColorIcono => Titulo switch
        {
            "Ventas" => "#3B82F6",
            "Producción" => "#F97316",
            "Reportes" => "#0EA5E9",
            "Almacén" => "#22C55E",
            "Productos" => "#F97316",
            "Destajo y Pagos" => "#8B5CF6",
            "Seguridad" => "#2563EB",
            "Configuración" => "#2563EB",
            _ => "#3B82F6"
        };
    }
}