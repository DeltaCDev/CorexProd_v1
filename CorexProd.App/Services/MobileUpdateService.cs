using CorexProd.App.Models;

namespace CorexProd.App.Services;

public sealed class MobileUpdateService
{
    private readonly CorexProdApiClient _apiClient;

    public MobileUpdateService(CorexProdApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<bool> CheckAndInstallAsync(Page page, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiClient.BaseUrl))
            return false;

        AppUpdateInfo update;
        try
        {
            update = await _apiClient.GetAppUpdateInfoAsync(cancellationToken);
        }
        catch
        {
            // La actualización no debe impedir el inicio de sesión si el API no responde.
            return false;
        }

        string versionLocal = AppInfo.Current.VersionString;
        if (!EsSuperior(update.Version, versionLocal))
            return false;

        bool descargar = await page.DisplayAlertAsync(
            "Actualización disponible",
            $"Está disponible CorexProd v{update.Version}.\nVersión instalada: v{versionLocal}\n\n¿Desea actualizar ahora?",
            "Actualizar",
            "Más tarde");

        if (!descargar)
            return false;

        try
        {
            byte[] apk = await _apiClient.DownloadAppUpdateAsync(cancellationToken);
            string nombre = $"CorexProd_{update.Version}.apk";
            string ruta = Path.Combine(FileSystem.CacheDirectory, nombre);
            await File.WriteAllBytesAsync(ruta, apk, cancellationToken);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = "Instalar actualización de CorexProd",
                File = new ReadOnlyFile(ruta)
            });

            return true;
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync("Actualización", $"No se pudo descargar la actualización.\n{ex.Message}", "Aceptar");
            return false;
        }
    }

    private static bool EsSuperior(string remota, string local)
    {
        if (!Version.TryParse(remota.TrimStart('v', 'V'), out Version? versionRemota))
            return false;

        return Version.TryParse(local.TrimStart('v', 'V'), out Version? versionLocal)
            && versionRemota > versionLocal;
    }
}
