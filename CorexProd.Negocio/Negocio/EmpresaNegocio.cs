using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class EmpresaNegocio
    {
        private readonly EmpresaDatos _empresaDatos = new();

        public List<Empresa> Listar()
        {
            return _empresaDatos.Listar();
        }

        public Empresa? ObtenerPredeterminada()
        {
            return _empresaDatos.ObtenerPredeterminada();
        }

        public string Guardar(Empresa empresa)
        {
            Normalizar(empresa);

            if (string.IsNullOrWhiteSpace(empresa.Ruc))
                return "El RUC es obligatorio";

            if (empresa.Ruc.Length != 11 || !empresa.Ruc.All(char.IsDigit))
                return "Ingrese un RUC valido de 11 digitos";

            if (string.IsNullOrWhiteSpace(empresa.Nombre))
                return "El nombre de la empresa es obligatorio";

            if (string.IsNullOrWhiteSpace(empresa.NombreComercial))
                empresa.NombreComercial = empresa.Nombre;

            if (empresa.IdEmpresa == 0 && !Listar().Any())
            {
                empresa.EsPredeterminada = true;
            }

            if (empresa.IdEmpresa == 0)
                return _empresaDatos.Registrar(empresa);

            return _empresaDatos.Editar(empresa);
        }

        public string Eliminar(int idEmpresa)
        {
            if (idEmpresa <= 0)
                return "Debe seleccionar una empresa valida";

            return _empresaDatos.Eliminar(idEmpresa);
        }

        private static void Normalizar(Empresa empresa)
        {
            empresa.Ruc = empresa.Ruc.Trim();
            empresa.Nombre = empresa.Nombre.Trim();
            empresa.NombreComercial = empresa.NombreComercial.Trim();
            empresa.Telefono = empresa.Telefono.Trim();
            empresa.Correo = empresa.Correo.Trim();
            empresa.Departamento = empresa.Departamento.Trim();
            empresa.Provincia = empresa.Provincia.Trim();
            empresa.Distrito = empresa.Distrito.Trim();
            empresa.Direccion = empresa.Direccion.Trim();
            empresa.CodigoCliente = empresa.CodigoCliente.Trim();
            empresa.LicenciaActivacion = empresa.LicenciaActivacion.Trim();
        }
    }
}
