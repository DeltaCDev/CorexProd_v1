using CorexProd.WPF.Modules.Tesoreria.ViewModels; using System.Windows.Controls;
namespace CorexProd.WPF.Modules.Tesoreria.Views { public partial class SegurosView : UserControl { public SegurosView() { InitializeComponent(); DataContext = new SegurosViewModel(); } } }
