using System.Reflection;
using CorexProd.App.Models;

namespace CorexProd.App.Pages;

public partial class OciPage
{
    private async void OnVerDetalleVerticalClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not object item)
            return;

        OciResumen? oci = item.GetType()
            .GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?
            .GetValue(item) as OciResumen;

        if (oci == null)
            return;

        await Shell.Current.GoToAsync($"{nameof(OciDetallePage)}?id={oci.IdOrdenCompraInterna}");
    }
}
