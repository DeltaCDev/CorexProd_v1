using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class PermisoMenuRolNegocio
    {
        private readonly PermisoMenuRolDatos _permisoDatos;

        public PermisoMenuRolNegocio()
        {
            _permisoDatos = new PermisoMenuRolDatos();
        }

        public List<MenuSistema> ListarMenusPorRol(int idRol)
        {
            return _permisoDatos.ListarMenusPorRol(idRol);
        }

        public void GuardarPermisos(int idRol, List<MenuSistema> menus)
        {
            foreach (var menu in menus)
            {
                _permisoDatos.GuardarPermiso(
                    idRol,
                    menu.IdMenu,
                    menu.TienePermiso
                );
            }
        }
    }
}