using CorexProd.App.Pages;

namespace CorexProd.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(StockProductosPage), typeof(StockProductosPage));
        Routing.RegisterRoute(nameof(StockInsumosPage), typeof(StockInsumosPage));
    }
}
