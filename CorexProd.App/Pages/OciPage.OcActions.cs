using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public partial class OciPage
{
    private async void OnImprimirClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OciListItem item)
            return;

        try
        {
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("OC demo", $"Se imprimiria la OC {item.NumeroOci}.", "OK");
                return;
            }

            await DescargarYAbrirOcPdfAsync(item.Item);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Imprimir OC", ex.Message, "OK");
        }
    }

    private async void OnCopiarClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OciListItem item)
            return;

        await Shell.Current.GoToAsync($"{nameof(ProformaEditorPage)}?id={item.Item.IdOrdenCompraInterna}&modo=copiar");
    }

    private async void OnEditarClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OciListItem item)
            return;

        try
        {
            OciDetalleResponse detalle = await _apiClient.GetOciDetalleAsync(item.Item.IdOrdenCompraInterna);
            if (!PuedeEditarDetalle(detalle))
            {
                await DisplayAlertAsync("Editar OC", "Solo se puede editar una OC pendiente sin OT, guias internas, anulaciones ni otra accion realizada.", "OK");
                await LoadAsync();
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(ProformaEditorPage)}?id={item.Item.IdOrdenCompraInterna}&modo=editar");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Editar OC", ex.Message, "OK");
        }
    }

    private async Task DescargarYAbrirOcPdfAsync(OciResumen oci)
    {
        byte[] pdf = await _apiClient.GetOciPdfAsync(oci.IdOrdenCompraInterna);
        string numero = string.IsNullOrWhiteSpace(oci.NumeroOci)
            ? $"oc-{oci.IdOrdenCompraInterna}"
            : FormatearNumeroOc(oci.NumeroOci).Replace("/", "-").Replace("\\", "-");
        string path = System.IO.Path.Combine(FileSystem.CacheDirectory, $"{numero}.pdf");
        await File.WriteAllBytesAsync(path, pdf);

        await Launcher.OpenAsync(new OpenFileRequest
        {
            Title = "Imprimir OC",
            File = new ReadOnlyFile(path, "application/pdf")
        });
    }

    private static bool PuedeEditarDetalle(OciDetalleResponse detalle)
    {
        string estado = DocumentoFiltroHelper.Normalizar(detalle.Cabecera.Estado);
        return estado is "PENDIENTE" or "EMITIDA" or "EMITIDO"
            && !detalle.Cabecera.TieneGuiaSalida
            && !detalle.Cabecera.TieneOrdenTrabajo
            && string.IsNullOrWhiteSpace(detalle.Cabecera.MotivoAnulacion)
            && !detalle.Cabecera.FechaAnulacion.HasValue
            && detalle.Detalles.All(x => (x.CantidadDespachada ?? 0) <= 0);
    }
}
