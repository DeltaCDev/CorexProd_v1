using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;

namespace CorexProd.Negocio.Negocio
{
    public class AreaProduccionNegocio
    {
        private readonly AreaProduccionDatos _datos = new();

        public List<AreaProduccion> Listar() => _datos.Listar();

        public string Guardar(AreaProduccion area)
        {
            area.CodigoArea = (area.CodigoArea ?? string.Empty).Trim().ToUpperInvariant();
            area.NombreArea = (area.NombreArea ?? string.Empty).Trim();
            area.Descripcion = (area.Descripcion ?? string.Empty).Trim();
            area.ModoEnvio = (area.ModoEnvio ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(area.CodigoArea))
                return "El código del área es obligatorio.";
            if (area.CodigoArea.Length > 20)
                return "El código del área no puede exceder 20 caracteres.";
            if (string.IsNullOrWhiteSpace(area.NombreArea))
                return "El nombre del área es obligatorio.";
            if (area.NombreArea.Length > 100)
                return "El nombre del área no puede exceder 100 caracteres.";
            if (area.OrdenSecuencia <= 0)
                return "El orden de secuencia debe ser mayor que cero.";
            if (area.ModoEnvio is not ("UNICO" or "PARCIAL"))
                return "El modo de envío seleccionado no es válido.";
            if (!area.Activo && area.EsInicio)
                return "Un área inactiva no puede configurarse como inicio.";
            if (!area.Activo && area.EsTermino)
                return "Un área inactiva no puede configurarse como término.";
            if (area.UsuarioRegistro <= 0 && !area.UsuarioModificacion.HasValue)
                return "No se pudo identificar al usuario de la operación.";

            return _datos.Guardar(area);
        }

        public string CambiarEstado(AreaProduccion area, bool activo, int idUsuario)
        {
            if (area.IdAreaProduccion <= 0)
                return "Seleccione un área de producción.";
            if (idUsuario <= 0)
                return "No se pudo identificar al usuario de la operación.";
            if (area.Activo == activo)
                return activo ? "El área ya se encuentra activa." : "El área ya se encuentra inactiva.";

            return _datos.CambiarEstado(area.IdAreaProduccion, activo, idUsuario);
        }
    }
}
