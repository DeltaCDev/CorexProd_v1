using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class ProveedorNegocio
    {
        private readonly IngresoManualStockDatos _datos = new();

        public List<ProveedorStock> Listar()
        {
            return _datos.ListarProveedores();
        }

        public string Guardar(ProveedorStock proveedor)
        {
            proveedor.TipoDocumento = proveedor.TipoDocumento.Trim();
            proveedor.NumeroDocumento = proveedor.NumeroDocumento.Trim();
            proveedor.NombreRazonSocial = proveedor.NombreRazonSocial.Trim();
            proveedor.Direccion = proveedor.Direccion.Trim();
            proveedor.Telefono = proveedor.Telefono.Trim();
            proveedor.Correo = proveedor.Correo.Trim();

            if (string.IsNullOrWhiteSpace(proveedor.TipoDocumento))
                return "El tipo de documento es obligatorio";

            if (proveedor.TipoDocumento != "S/N" && string.IsNullOrWhiteSpace(proveedor.NumeroDocumento))
                return "El numero de documento es obligatorio";

            if (string.IsNullOrWhiteSpace(proveedor.NombreRazonSocial))
                return "El nombre o razon social es obligatorio";

            if (proveedor.IdProveedor == 0)
                return _datos.RegistrarProveedor(proveedor);

            return _datos.EditarProveedor(proveedor);
        }

        public string Eliminar(int idProveedor)
        {
            if (idProveedor <= 0)
                return "Debe seleccionar un proveedor valido";

            return _datos.EliminarProveedor(idProveedor);
        }
    }
}
