using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class FormaPagoOsNegocio
    {
        private readonly FormaPagoOsDatos _datos = new();

        public List<FormaPagoOs> Listar(bool soloActivos = false) => _datos.Listar(soloActivos);

        public string Guardar(FormaPagoOs forma)
        {
            forma.Nombre = forma.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(forma.Nombre))
                return "Debe ingresar el nombre de la forma de pago OS.";

            return _datos.Guardar(forma);
        }

        public string Eliminar(int idFormaPagoOs)
        {
            if (idFormaPagoOs <= 0)
                return "Debe seleccionar una forma de pago OS.";

            return _datos.Eliminar(idFormaPagoOs);
        }
    }
}
