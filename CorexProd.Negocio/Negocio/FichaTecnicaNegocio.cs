using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class FichaTecnicaNegocio
    {
        private readonly FichaTecnicaDatos _datos = new FichaTecnicaDatos();

        // =========================
        // FICHA TECNICA
        // =========================

        public List<FichaTecnica> Listar()
        {
            return _datos.Listar();
        }

        public bool Registrar(FichaTecnica ficha, out string mensaje)
        {
            mensaje = string.Empty;

            if (ficha.IdProducto <= 0)
            {
                mensaje = "Debe seleccionar un producto.";
                return false;
            }

            if (ficha.Version <= 0)
            {
                mensaje = "La versión debe ser mayor a cero.";
                return false;
            }

            return _datos.Registrar(ficha, out mensaje);
        }

        public bool Editar(FichaTecnica ficha, out string mensaje)
        {
            mensaje = string.Empty;

            if (ficha.IdFichaTecnica <= 0)
            {
                mensaje = "La ficha técnica no es válida.";
                return false;
            }

            if (ficha.IdProducto <= 0)
            {
                mensaje = "Debe seleccionar un producto.";
                return false;
            }

            if (ficha.Version <= 0)
            {
                mensaje = "La versión debe ser mayor a cero.";
                return false;
            }

            return _datos.Editar(ficha, out mensaje);
        }

        // =========================
        // DETALLE
        // =========================

        public List<FichaTecnicaDetalle> ListarDetalle(int idFichaTecnica)
        {
            return _datos.ListarDetalle(idFichaTecnica);
        }

        public bool RegistrarDetalle(FichaTecnicaDetalle detalle, out string mensaje)
        {
            mensaje = string.Empty;

            if (detalle.IdFichaTecnica <= 0)
            {
                mensaje = "La ficha técnica no es válida.";
                return false;
            }

            if (detalle.IdInsumo <= 0)
            {
                mensaje = "Debe seleccionar un insumo.";
                return false;
            }

            if (detalle.Cantidad <= 0)
            {
                mensaje = "La cantidad debe ser mayor a cero.";
                return false;
            }

            if (detalle.IdUnidadMedida <= 0)
            {
                mensaje = "Debe seleccionar una unidad de medida.";
                return false;
            }

            return _datos.RegistrarDetalle(detalle, out mensaje);
        }

        public bool EditarDetalle(FichaTecnicaDetalle detalle, out string mensaje)
        {
            mensaje = string.Empty;

            if (detalle.IdFichaTecnicaDetalle <= 0)
            {
                mensaje = "El detalle seleccionado no es válido.";
                return false;
            }

            if (detalle.Cantidad <= 0)
            {
                mensaje = "La cantidad debe ser mayor a cero.";
                return false;
            }

            return _datos.EditarDetalle(detalle, out mensaje);
        }

        public bool EliminarDetalle(int idFichaTecnicaDetalle, out string mensaje)
        {
            mensaje = string.Empty;

            if (idFichaTecnicaDetalle <= 0)
            {
                mensaje = "El detalle seleccionado no es válido.";
                return false;
            }

            return _datos.EliminarDetalle(idFichaTecnicaDetalle, out mensaje);
        }
    }
}