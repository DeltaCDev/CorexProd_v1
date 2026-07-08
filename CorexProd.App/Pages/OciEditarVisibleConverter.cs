using System.Globalization;
using CorexProd.App.Models;
using CorexProd.App.Services;

namespace CorexProd.App.Pages;

public sealed class OciEditarVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return false;

        object? itemValue = value.GetType().GetProperty("Item")?.GetValue(value);
        if (itemValue is not OciResumen oci)
            return false;

        string estado = DocumentoFiltroHelper.Normalizar(oci.Estado);
        return estado is "PENDIENTE" or "EMITIDA" or "EMITIDO"
            && !oci.TieneGuiaSalida
            && !oci.TieneOrdenTrabajo
            && !oci.TieneOtActiva
            && string.IsNullOrWhiteSpace(oci.MotivoAnulacion)
            && !oci.FechaAnulacion.HasValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
