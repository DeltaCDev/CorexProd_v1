using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
 public partial class ValidacionInsumosWindow:Window
 {
  public ValidacionInsumosWindow(IReadOnlyCollection<OrdenTrabajoValidacionProducto> productos){InitializeComponent();ProductosGrid.ItemsSource=productos;MensajeText.Text=productos.Any(x=>x.EstadoInsumos!="DISPONIBLES")?"Se encontraron productos con insumos faltantes o sin ficha. Puede continuar con la generación de la Orden de Trabajo. El consumo de producción podrá generar saldos negativos.":"Todos los productos cuentan con los insumos necesarios.";}
  private void Continuar_Click(object sender,RoutedEventArgs e)=>DialogResult=true;
 }
}
