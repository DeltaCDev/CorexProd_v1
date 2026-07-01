using CorexProd.App.Pages;
using CorexProd.App.Services;
using Microsoft.Extensions.Logging;

namespace CorexProd.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton(_ => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        });
        builder.Services.AddSingleton<CorexProdApiClient>();
        builder.Services.AddSingleton<SessionState>();

        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<StockProductosPage>();
        builder.Services.AddTransient<StockInsumosPage>();
        builder.Services.AddTransient<ProformasPage>();
        builder.Services.AddTransient<ProformaEditorPage>();
        builder.Services.AddTransient<OciPage>();
        builder.Services.AddTransient<OrdenesTrabajoPage>();
        builder.Services.AddTransient<OrdenTrabajoDetallePage>();
        builder.Services.AddTransient<IngresoManualStockPage>();
        builder.Services.AddTransient<ModuloPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
