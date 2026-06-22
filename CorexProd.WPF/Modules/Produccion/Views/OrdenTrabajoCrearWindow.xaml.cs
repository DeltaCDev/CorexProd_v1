using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
 public partial class OrdenTrabajoCrearWindow:Window
 {
  private readonly OrdenCompraInterna _oci;private readonly List<OrdenTrabajoValidacionProducto> _productos;private readonly OrdenTrabajoNegocio _negocio=new();private bool _guardando;
  public OrdenTrabajoCrearWindow(OrdenCompraInterna oci,IEnumerable<OrdenTrabajoValidacionProducto> productos){InitializeComponent();_oci=oci;_productos=productos.ToList();CabeceraText.Text=$"OCI {oci.NumeroOci} | Cliente: {oci.NombreCliente}";OrdenCompraText.Text=$"Orden de compra del cliente: {oci.OrdenCompraCliente}";DetallesGrid.ItemsSource=_productos;}
  private void VerDetalle_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is not OrdenTrabajoValidacionProducto p)return;var insumos=_negocio.DetalleInsumos(p.IdOrdenCompraInternaDetalle);new DetalleInsumosProductoWindow(p,_oci.OrdenCompraCliente,insumos){Owner=this}.ShowDialog();}
  private void Crear_Click(object sender,RoutedEventArgs e){if(_guardando)return;try{_guardando=true;var items=_productos.Select(x=>new OrdenTrabajoPlanificacion{IdOrdenCompraInternaDetalle=x.IdOrdenCompraInternaDetalle,CantidadPlanificada=x.CantidadRequerida}).ToList();int usuario=SessionManager.UsuarioActual?.IdUsuario??0;var r=_negocio.Crear(_oci.IdOrdenCompraInterna,usuario,ObservacionText.Text.Trim(),items);NotificationService.Success($"Se creó {r.Numero} con {items.Count} producto(s). La OCI pasó a PROCESO.");DialogResult=true;}catch(Exception ex){NotificationService.Error(ex.Message);}finally{_guardando=false;}}
 }
}
