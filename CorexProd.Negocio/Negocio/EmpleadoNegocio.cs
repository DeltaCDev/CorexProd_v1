using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class EmpleadoNegocio
    {
        private readonly EmpleadoDatos _empleadoDatos = new();

        public List<Empleado> Listar()
        {
            return _empleadoDatos.Listar();
        }

        public string Guardar(Empleado empleado)
        {
            empleado.TipoDocumento = empleado.TipoDocumento.Trim();
            empleado.NumeroDocumento = empleado.NumeroDocumento.Trim();
            empleado.Nombre = empleado.Nombre.Trim();
            empleado.Apellido = empleado.Apellido.Trim();
            empleado.Sexo = empleado.Sexo.Trim();
            empleado.Telefono = empleado.Telefono.Trim();
            empleado.Email = empleado.Email.Trim();
            empleado.Direccion = empleado.Direccion.Trim();

            if (string.IsNullOrWhiteSpace(empleado.TipoDocumento))
                return "El tipo de documento es obligatorio";

            if (string.IsNullOrWhiteSpace(empleado.NumeroDocumento))
                return "El número de documento es obligatorio";

            if (string.IsNullOrWhiteSpace(empleado.Nombre))
                return "El nombre del empleado es obligatorio";

            if (string.IsNullOrWhiteSpace(empleado.Apellido))
                return "El apellido del empleado es obligatorio";

            if (string.IsNullOrWhiteSpace(empleado.Sexo))
                return "Debe seleccionar el sexo";

            if (empleado.IdCargo <= 0)
                return "Debe seleccionar un cargo";

            if (empleado.IdEmpleado == 0)
                return _empleadoDatos.Registrar(empleado);

            return _empleadoDatos.Editar(empleado);
        }

        public string Eliminar(int idEmpleado)
        {
            if (idEmpleado <= 0)
                return "Debe seleccionar un empleado válido";

            return _empleadoDatos.Eliminar(idEmpleado);
        }
    }
}