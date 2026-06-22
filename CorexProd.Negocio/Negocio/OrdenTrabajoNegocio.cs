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
        public List<OrdenTrabajoValidacionProducto> ValidarInsumos(int idOci)=>_datos.ValidarInsumos(idOci);
        public List<OrdenTrabajoInsumoDetalle> DetalleInsumos(int idDetalleOci)=>_datos.DetalleInsumos(idDetalleOci);
        public (int Id,string Numero) Crear(int idOci,int idUsuario,string observacion,IEnumerable<OrdenTrabajoPlanificacion> items)=>_datos.Crear(idOci,idUsuario,observacion,items);

        public Usuario Autorizar(string usuario,string clave)
        {
            Usuario? u=_usuarios.Login(usuario.Trim());
            if(u==null || !u.Estado || !PasswordService.VerifyPassword(clave,u.Clave)) throw new InvalidOperationException("Las credenciales del usuario autorizador no son válidas.");
            return u;
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
        public void ConfirmarConsumo(int idDetalleOt,int idUsuario)=>_datos.ConfirmarConsumo(idDetalleOt,idUsuario);
    }
}
