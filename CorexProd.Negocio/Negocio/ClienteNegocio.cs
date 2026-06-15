using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class ClienteNegocio
    {
        private readonly ClienteDatos _clienteDatos = new();

        public List<Cliente> Listar()
        {
            return _clienteDatos.Listar();
        }

        public string Guardar(Cliente cliente)
        {
            cliente.TipoDocumento = cliente.TipoDocumento.Trim();
            cliente.NumeroDocumento = cliente.NumeroDocumento.Trim();
            cliente.NombreRazonSocial = cliente.NombreRazonSocial.Trim();
            cliente.Direccion = cliente.Direccion.Trim();
            cliente.Telefono = cliente.Telefono.Trim();
            cliente.Correo = cliente.Correo.Trim();

            if (string.IsNullOrWhiteSpace(cliente.TipoDocumento))
                return "El tipo de documento es obligatorio";

            if (cliente.TipoDocumento != "S/N" && string.IsNullOrWhiteSpace(cliente.NumeroDocumento))
                return "El número de documento es obligatorio";

            if (string.IsNullOrWhiteSpace(cliente.NombreRazonSocial))
                return "El nombre o razón social es obligatorio";

            if (cliente.IdCliente == 0)
                return _clienteDatos.Registrar(cliente);

            return _clienteDatos.Editar(cliente);
        }

        public string Eliminar(int idCliente)
        {
            if (idCliente <= 0)
                return "Debe seleccionar un cliente válido";

            return _clienteDatos.Eliminar(idCliente);
        }
    }
}
