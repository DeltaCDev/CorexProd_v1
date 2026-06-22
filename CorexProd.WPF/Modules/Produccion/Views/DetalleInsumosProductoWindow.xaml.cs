using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Modules.Produccion.Views
{
 public partial class DetalleInsumosProductoWindow:Window
 {
  public DetalleInsumosProductoWindow(OrdenTrabajoValidacionProducto producto,string ordenCompra,IReadOnlyCollection<OrdenTrabajoInsumoDetalle> insumos){InitializeComponent();ProductoText.Text=$"Producto: {producto.Producto}";OrdenText.Text=$"Orden de compra del cliente: {ordenCompra}";ProduccionText.Text=$"Producción solicitada: {producto.CantidadRequerida:N3} unidades";InsumosGrid.ItemsSource=insumos;int faltan=insumos.Count(x=>x.CantidadFaltante>0);ResultadoText.Text=faltan==0&&insumos.Count>0?"Resultado: SE PUEDE PRODUCIR":"Resultado: NO SE PUEDE PRODUCIR";FaltantesText.Text=producto.EstadoInsumos=="SIN FICHA"?"El producto no tiene una ficha técnica válida.":$"Insumos faltantes: {faltan}";}
  private void Imprimir_Click(object sender,RoutedEventArgs e){PrintDialog p=new();if(p.ShowDialog()==true)p.PrintVisual(Contenido,$"Detalle de insumos - {ProductoText.Text}");}
 }
}
