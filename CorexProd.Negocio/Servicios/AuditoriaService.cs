using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;

namespace CorexProd.Negocio.Servicios
{
    public static class AuditoriaService
    {
        public static void Registrar(
            string usuario,
            string accion,
            string modulo,
            string descripcion)
        {
            try
            {
                Auditoria auditoria = new()
                {
                    Usuario = usuario,
                    Accion = accion,
                    Modulo = modulo,
                    Descripcion = descripcion,
                    Equipo = Environment.MachineName
                };

                AuditoriaDatos datos = new();
                datos.Registrar(auditoria);
            }
            catch
            {
                // Evita que falle el sistema si falla auditoría
            }
        }
    }
}