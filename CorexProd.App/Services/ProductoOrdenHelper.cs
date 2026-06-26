using System.Text.RegularExpressions;

namespace CorexProd.App.Services;

public static partial class ProductoOrdenHelper
{
    private static readonly Dictionary<string, int> Tallas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["XXS"] = 10,
        ["XS"] = 20,
        ["S"] = 30,
        ["M"] = 40,
        ["L"] = 50,
        ["XL"] = 60,
        ["XXL"] = 70,
        ["XXXL"] = 80,
        ["XXXXL"] = 90
    };

    public static (string CodigoBase, int TallaOrden, string Talla, string Codigo) CrearClave(string codigo, string nombre)
    {
        codigo = (codigo ?? string.Empty).Trim().ToUpperInvariant();
        nombre = (nombre ?? string.Empty).Trim().ToUpperInvariant();

        string talla = ObtenerTalla(codigo, nombre);
        string codigoBase = string.IsNullOrWhiteSpace(talla)
            ? codigo
            : RemoverTalla(codigo, talla);

        return (codigoBase, ObtenerOrdenTalla(talla), talla, codigo);
    }

    private static string ObtenerTalla(string codigo, string nombre)
    {
        Match nombreMatch = NombreTallaRegex().Match(nombre);
        if (nombreMatch.Success)
            return nombreMatch.Groups["talla"].Value;

        Match codigoMatch = CodigoTallaRegex().Match(codigo);
        return codigoMatch.Success ? codigoMatch.Groups["talla"].Value : string.Empty;
    }

    private static string RemoverTalla(string codigo, string talla)
    {
        if (string.IsNullOrWhiteSpace(talla) || !codigo.EndsWith(talla, StringComparison.OrdinalIgnoreCase))
            return codigo;

        return codigo[..^talla.Length];
    }

    private static int ObtenerOrdenTalla(string talla)
    {
        if (string.IsNullOrWhiteSpace(talla))
            return 9999;

        if (Tallas.TryGetValue(talla, out int orden))
            return orden;

        Match xlMatch = ExtraGrandeRegex().Match(talla);
        if (xlMatch.Success && int.TryParse(xlMatch.Groups["numero"].Value, out int numeroXl))
            return 80 + (numeroXl * 10);

        if (int.TryParse(talla, out int numero))
            return 1000 + numero;

        return 9000;
    }

    [GeneratedRegex(@"(?<talla>XXS|XS|S|M|L|XL|XXL|XXXL|XXXXL|[2-9]XL|\d{1,3})$", RegexOptions.IgnoreCase)]
    private static partial Regex CodigoTallaRegex();

    [GeneratedRegex(@"\bTALLA\s+(?<talla>XXS|XS|S|M|L|XL|XXL|XXXL|XXXXL|[2-9]XL|\d{1,3})\b", RegexOptions.IgnoreCase)]
    private static partial Regex NombreTallaRegex();

    [GeneratedRegex(@"^(?<numero>[2-9])XL$", RegexOptions.IgnoreCase)]
    private static partial Regex ExtraGrandeRegex();
}
