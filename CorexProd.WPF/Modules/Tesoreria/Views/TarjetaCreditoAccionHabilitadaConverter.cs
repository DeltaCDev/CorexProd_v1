using System;
using System.Globalization;
using System.Windows.Data;

namespace CorexProd.WPF.Modules.Tesoreria.Views
{
    public sealed class TarjetaCreditoAccionHabilitadaConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string estado = values.Length > 0 ? values[0]?.ToString() ?? string.Empty : string.Empty;
            decimal saldo = values.Length > 1 && values[1] is decimal value ? value : 0;
            bool tarjetaAnulada = values.Length > 2 && values[2] is bool anulada && anulada;
            int id = values.Length > 3 && values[3] is int identificador ? identificador : 0;

            return parameter?.ToString() switch
            {
                "PAGAR" => !tarjetaAnulada && saldo > 0 && (estado == "PENDIENTE" || estado == "PARCIAL"),
                "EDITAR_PERIODO" => !tarjetaAnulada && estado != "ANULADA",
                "ANULAR_PERIODO" => !tarjetaAnulada && estado != "ANULADA" && saldo <= 0 && id > 0,
                "REVERTIR" => !tarjetaAnulada && estado == "ACTIVO" && id > 0,
                _ => false
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
