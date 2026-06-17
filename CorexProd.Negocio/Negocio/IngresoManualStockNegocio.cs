using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class IngresoManualStockNegocio
    {
        private readonly IngresoManualStockDatos _datos = new();

        public List<IngresoManualStock> Listar(DateTime? fechaDesde, DateTime? fechaHasta, int? idProveedor, int? idAlmacen, string estado, string numeroDocumento)
        {
            return _datos.Listar(fechaDesde, fechaHasta, idProveedor, idAlmacen, estado, numeroDocumento);
        }

        public IngresoManualStock? Obtener(int idIngresoManualStock)
        {
            return _datos.Obtener(idIngresoManualStock);
        }

        public string Guardar(IngresoManualStock ingreso, string usuario)
        {
            string validacion = ValidarIngreso(ingreso);

            if (!string.IsNullOrWhiteSpace(validacion))
            {
                return validacion;
            }

            return _datos.Guardar(ingreso, usuario);
        }

        public string Abastecer(int idIngresoManualStock, string usuario)
        {
            if (idIngresoManualStock <= 0)
            {
                return "Debe seleccionar un ingreso manual valido";
            }

            return _datos.Abastecer(idIngresoManualStock, usuario);
        }

        public string Anular(int idIngresoManualStock, string usuario, string motivo)
        {
            if (idIngresoManualStock <= 0)
            {
                return "Debe seleccionar un ingreso manual valido";
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return "Debe ingresar el motivo de anulacion";
            }

            return _datos.Anular(idIngresoManualStock, usuario, motivo);
        }

        public List<ProveedorStock> ListarProveedores()
        {
            return _datos.ListarProveedores();
        }

        public int RegistrarProveedorRapido(ProveedorStock proveedor, out string mensaje)
        {
            if (string.IsNullOrWhiteSpace(proveedor.NombreRazonSocial))
            {
                mensaje = "Debe ingresar el nombre o razon social del proveedor";
                return 0;
            }

            return _datos.RegistrarProveedorRapido(proveedor, out mensaje);
        }

        public List<AlmacenStock> ListarAlmacenes()
        {
            return _datos.ListarAlmacenes();
        }

        public List<TipoDocumentoStock> ListarTiposDocumento()
        {
            return _datos.ListarTiposDocumento();
        }

        public List<ProductoStockBusqueda> BuscarProductos(int idAlmacen, string texto)
        {
            if (idAlmacen <= 0 || string.IsNullOrWhiteSpace(texto))
            {
                return [];
            }

            return _datos.BuscarProductos(idAlmacen, texto);
        }

        private static string ValidarIngreso(IngresoManualStock ingreso)
        {
            if (ingreso.FechaEmision == default)
            {
                return "Debe ingresar la fecha de emision";
            }

            if (ingreso.IdProveedor <= 0)
            {
                return "Debe seleccionar un proveedor";
            }

            if (ingreso.IdTipoDocumento <= 0)
            {
                return "Debe seleccionar un tipo de documento";
            }

            if (ingreso.IdAlmacen <= 0)
            {
                return "Debe seleccionar un almacen";
            }

            if (ingreso.Detalles.Count == 0)
            {
                return "Debe agregar al menos un producto";
            }

            if (ingreso.TipoNumeracion.Equals("Manual", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(ingreso.Serie) || string.IsNullOrWhiteSpace(ingreso.Numero)))
            {
                return "Debe ingresar serie y numero para la numeracion manual";
            }

            if (ingreso.Detalles.GroupBy(d => d.IdProducto).Any(g => g.Count() > 1))
            {
                return "No se permiten productos repetidos";
            }

            foreach (IngresoManualStockDetalle detalle in ingreso.Detalles)
            {
                if (detalle.IdProducto <= 0)
                {
                    return "Todos los productos deben estar seleccionados";
                }

                if (detalle.Cantidad <= 0)
                {
                    return "La cantidad debe ser mayor que cero";
                }

                if (detalle.PrecioUnitario < 0)
                {
                    return "El precio unitario no puede ser negativo";
                }

                if (detalle.Descuento < 0)
                {
                    return "El descuento no puede ser negativo";
                }

                if (detalle.Descuento > detalle.Cantidad * detalle.PrecioUnitario)
                {
                    return "El descuento no puede superar el importe bruto del producto";
                }
            }

            return string.Empty;
        }
    }
}
