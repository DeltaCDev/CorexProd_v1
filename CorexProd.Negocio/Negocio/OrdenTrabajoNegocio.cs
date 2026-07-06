using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class OrdenTrabajoNegocio
    {
        private readonly OrdenTrabajoDatos _datos=new();
        private readonly UsuarioDatos _usuarios=new();
        public List<OrdenTrabajo> Listar()=>_datos.Listar();
        public OrdenTrabajo? Obtener(int id)=>_datos.Obtener(id);
        public List<OrdenTrabajoMovimiento> ListarMovimientos(int idOrdenTrabajo)=>idOrdenTrabajo>0?_datos.ListarMovimientos(idOrdenTrabajo):[];
        public List<OrdenTrabajoKardexIngreso> ListarIngresosKardex(int idOrdenTrabajo)=>idOrdenTrabajo>0?_datos.ListarIngresosKardex(idOrdenTrabajo):[];
        public List<OrdenTrabajoValidacionProducto> ValidarInsumos(int idOci)=>_datos.ValidarInsumos(idOci);
        public List<OrdenTrabajoValidacionProducto> ValidarInsumosManual(IEnumerable<OrdenTrabajoManualPlanificacion> items)=>_datos.ValidarInsumosManual(items);
        public List<OrdenTrabajoValidacionProducto> ListarPendientesRegularizacion(int idOrdenTrabajo)=>idOrdenTrabajo>0?_datos.ListarPendientesRegularizacion(idOrdenTrabajo):[];
        public List<OrdenTrabajoInsumoDetalle> DetalleInsumos(int idDetalleOci)=>_datos.DetalleInsumos(idDetalleOci);
        public (int Id,string Numero) Crear(int idOci,int idUsuario,string observacion,IEnumerable<OrdenTrabajoPlanificacion> items,int? idOrdenTrabajoRelacionada=null)=>_datos.Crear(idOci,idUsuario,observacion,items,idOrdenTrabajoRelacionada);
        public (int Id,string Numero) CrearManual(int idUsuario,string observacion,IEnumerable<OrdenTrabajoManualPlanificacion> items)
        {
            List<OrdenTrabajoManualPlanificacion> lista = items.ToList();
            if (idUsuario <= 0) throw new InvalidOperationException("No se pudo identificar al usuario de sesion.");
            if (lista.Count == 0) throw new InvalidOperationException("Seleccione al menos un producto.");
            if (lista.Any(x => x.IdProducto <= 0 || x.CantidadPlanificada <= 0)) throw new InvalidOperationException("Todas las cantidades deben ser mayores que cero.");
            return _datos.CrearManual(idUsuario, observacion, lista);
        }
        public void Anular(int idOrdenTrabajo, bool convertirProcesoAMerma = false, int idUsuarioSesion = 0, string motivoAnulacion = "", string usuarioAnulacion = "")
        {
            if (idOrdenTrabajo <= 0) throw new InvalidOperationException("Seleccione una OT valida.");
            motivoAnulacion = motivoAnulacion.Trim();
            usuarioAnulacion = string.IsNullOrWhiteSpace(usuarioAnulacion) ? "Sistema" : usuarioAnulacion.Trim();
            if (string.IsNullOrWhiteSpace(motivoAnulacion)) throw new InvalidOperationException("Ingrese el motivo de anulacion.");
            OrdenTrabajo? ot = _datos.Obtener(idOrdenTrabajo);
            if (ot == null) throw new InvalidOperationException("No se encontro la OT seleccionada.");
            if (!ot.PuedeAnular) throw new InvalidOperationException("Solo se puede anular una OT en estado Pendiente o En Proceso sin productos terminados.");
            if (ot.EstadoOperativo.Equals("En Proceso", StringComparison.OrdinalIgnoreCase)
                && ot.Detalles.Any(x => x.Estado.Equals("TERMINADO", StringComparison.OrdinalIgnoreCase) || x.CantidadProducida > 0))
                throw new InvalidOperationException("La OT tiene productos terminados y no puede anularse.");
            _datos.Anular(idOrdenTrabajo, convertirProcesoAMerma, idUsuarioSesion, motivoAnulacion, usuarioAnulacion);
        }

        public Usuario Autorizar(string usuario,string clave)
        {
            Usuario? u=_usuarios.Login(usuario.Trim());
            if(u==null || !u.Estado) throw new InvalidOperationException("Usuario autorizador no válido.");
            if(!PasswordService.VerifyPassword(clave,u.Clave)) throw new InvalidOperationException("Clave incorrecta.");
            return u;
        }

        public Usuario AutorizarPorClave(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
                throw new InvalidOperationException("Ingrese la clave del usuario autorizador.");

            Usuario? usuario = _usuarios
                .Listar()
                .Where(x => x.Estado)
                .FirstOrDefault(x => PasswordService.VerifyPassword(clave, x.Clave));

            return usuario ?? throw new InvalidOperationException("Clave incorrecta o usuario inactivo.");
        }

        public void Lanzar(int idOt,int idSesion,Usuario autoriza,IEnumerable<OrdenTrabajoLanzamiento> items)
        {
            List<OrdenTrabajoLanzamiento> lista=items.ToList(); if(lista.Count==0) throw new InvalidOperationException("Seleccione al menos un producto.");
            _datos.Lanzar(idOt,idSesion,autoriza.IdUsuario,lista);
        }
        public long Transferir(int idOt,int idArea,int idSesion,Usuario autoriza,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
        {
            List<OrdenTrabajoTransferenciaItem> lista=items.ToList(); if(lista.Count==0) throw new InvalidOperationException("Seleccione al menos un producto.");
            if(lista.GroupBy(x=>x.IdDetalleOT).Any(g=>g.Count()>1)) throw new InvalidOperationException("No se puede transferir un producto duplicado.");
            return _datos.Transferir(idOt,idArea,idSesion,autoriza.IdUsuario,observacion,lista);
        }
        public long Terminar(int idOt,int idArea,int idSesion,Usuario autoriza,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
        {
            List<OrdenTrabajoTransferenciaItem> lista=items.ToList(); if(lista.Count==0) throw new InvalidOperationException("Seleccione al menos un producto.");
            if(lista.GroupBy(x=>x.IdDetalleOT).Any(g=>g.Count()>1)) throw new InvalidOperationException("No se puede terminar un producto duplicado.");
            return _datos.Terminar(idOt,idArea,idSesion,autoriza.IdUsuario,observacion,lista);
        }
        public long TransferirConMerma(int idOt,int idArea,long idDetalleArea,int idSesion,Usuario autoriza,decimal cantidadMerma,string motivo,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
        {
            List<OrdenTrabajoTransferenciaItem> lista=items.ToList(); if(lista.Count!=1) throw new InvalidOperationException("La operacion con merma debe realizarse para un producto.");
            return _datos.TransferirConMerma(idOt,idArea,idDetalleArea,idSesion,autoriza.IdUsuario,cantidadMerma,motivo,observacion,lista);
        }
        public long TerminarConMerma(int idOt,int idArea,long idDetalleArea,int idSesion,Usuario autoriza,decimal cantidadMerma,string motivo,string observacion,IEnumerable<OrdenTrabajoTransferenciaItem> items)
        {
            List<OrdenTrabajoTransferenciaItem> lista=items.ToList(); if(lista.Count!=1) throw new InvalidOperationException("La operacion con merma debe realizarse para un producto.");
            return _datos.TerminarConMerma(idOt,idArea,idDetalleArea,idSesion,autoriza.IdUsuario,cantidadMerma,motivo,observacion,lista);
        }
        public void RegistrarMerma(long idArea,decimal cantidad,string motivo,string observacion,int idSesion,Usuario autoriza)=>_datos.RegistrarMerma(idArea,cantidad,motivo,observacion,idSesion,autoriza.IdUsuario);
        public void ReservarStockProceso(long idDetalleArea,decimal cantidad,string observacion,int idSesion,Usuario autoriza)
        {
            if (idDetalleArea <= 0) throw new InvalidOperationException("Seleccione el area de produccion.");
            if (cantidad <= 0) throw new InvalidOperationException("La cantidad a reservar debe ser mayor que cero.");
            _datos.ReservarStockProceso(idDetalleArea,cantidad,observacion,idSesion,autoriza.IdUsuario);
        }
        public void ConfirmarConsumo(int idDetalleOt,int idUsuario)=>_datos.ConfirmarConsumo(idDetalleOt,idUsuario);
    }
}
