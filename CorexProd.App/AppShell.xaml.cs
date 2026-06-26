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
        Routing.RegisterRoute(nameof(ProformasPage), typeof(ProformasPage));
        Routing.RegisterRoute(nameof(OciPage), typeof(OciPage));
        Routing.RegisterRoute(nameof(OrdenesTrabajoPage), typeof(OrdenesTrabajoPage));
        Routing.RegisterRoute(nameof(OrdenTrabajoDetallePage), typeof(OrdenTrabajoDetallePage));
        Routing.RegisterRoute(nameof(IngresoManualStockPage), typeof(IngresoManualStockPage));
        Routing.RegisterRoute(nameof(ModuloPage), typeof(ModuloPage));
    }
}
