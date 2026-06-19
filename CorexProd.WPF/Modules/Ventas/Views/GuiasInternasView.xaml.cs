using CorexProd.WPF.Modules.Ventas.ViewModels;
using System.Windows.Controls;
namespace CorexProd.WPF.Modules.Ventas.Views { public partial class GuiasInternasView:UserControl { public GuiasInternasView(){InitializeComponent();DataContext=new GuiasInternasViewModel();} } }
