using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class RolNegocio
    {
        private readonly RolDatos _rolDatos;

        public RolNegocio()
        {
            _rolDatos = new RolDatos();
        }

        public List<Rol> Listar()
        {
            return _rolDatos.Listar();
        }

        public void Guardar(Rol rol)
        {
            if (string.IsNullOrWhiteSpace(rol.NombreRol))
            {
                throw new Exception("Ingrese el nombre del rol.");
            }

            if (rol.IdRol == 0)
            {
                _rolDatos.Registrar(rol);
            }
            else
            {
                _rolDatos.Editar(rol);
            }
        }
    }
}