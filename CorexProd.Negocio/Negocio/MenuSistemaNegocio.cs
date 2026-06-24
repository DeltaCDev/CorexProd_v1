using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class MenuSistemaNegocio
    {
        private readonly MenuSistemaDatos _datos = new();

        public List<MenuSistema> Listar() => _datos.Listar();

        public void GuardarOrdenes(IEnumerable<MenuSistema> menus)
        {
            List<MenuSistema> lista = menus.ToList();

            foreach (MenuSistema menu in lista)
            {
                if (menu.Orden <= 0)
                {
                    menu.Orden = 1;
                }
            }

            _datos.GuardarOrdenes(lista);
        }
    }
}
