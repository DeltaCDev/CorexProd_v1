using CorexProd.Entidad.Entidades;
using System.Windows;
namespace CorexProd.WPF.Modules.Ventas.Views { public partial class GuiaInternaDetalleWindow:Window { public GuiaInternaDetalleWindow(GuiaInterna guia){InitializeComponent();DataContext=guia;} } }
