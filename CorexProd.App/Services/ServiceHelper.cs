using Microsoft.Extensions.DependencyInjection;

namespace CorexProd.App.Services;

public static class ServiceHelper
{
    public static T GetRequiredService<T>() where T : notnull
    {
        return IPlatformApplication.Current!.Services.GetRequiredService<T>();
    }
}
