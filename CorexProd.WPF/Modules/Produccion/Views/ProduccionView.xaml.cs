using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Produccion.Views
{
 public partial class ProduccionView:UserControl
 {
  private readonly OrdenTrabajoNegocio _negocio=new();
  public ProduccionView(){InitializeComponent();Cargar();}
  private void Cargar(){try{OrdenesGrid.ItemsSource=_negocio.Listar();}catch(Exception ex){NotificationService.Error($"No se pudieron cargar las OT: {ex.Message}");}}
  private void Actualizar_Click(object sender,RoutedEventArgs e)=>Cargar();
  private void Abrir_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot)Abrir(ot);}
  private void OrdenesGrid_MouseDoubleClick(object sender,MouseButtonEventArgs e){if(OrdenesGrid.SelectedItem is OrdenTrabajo ot)Abrir(ot);}
  private void Kardex_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot)AbrirVentana(()=>new OrdenTrabajoDetalleWindow(ot.IdOrdenTrabajo){Owner=Application.Current.MainWindow,Title=$"Kardex de {ot.NumeroOT}"});}
  private void Historial_Click(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is OrdenTrabajo ot)AbrirVentana(()=>new OrdenTrabajoHistorialWindow(ot){Owner=Application.Current.MainWindow,Title=$"Historial de {ot.NumeroOT}"});}
  private void Abrir(OrdenTrabajo ot){AbrirVentana(()=>new OrdenTrabajoDetalleWindow(ot.IdOrdenTrabajo){Owner=Application.Current.MainWindow});Cargar();}
  private static void AbrirVentana(Func<Window> crear){try{crear().ShowDialog();}catch(Exception ex){NotificationService.Error($"No se pudo abrir la ventana: {ex.Message}");}}
 }
}
