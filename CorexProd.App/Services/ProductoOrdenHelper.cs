using System.Text.RegularExpressions;

namespace CorexProd.App.Services;

public static partial class ProductoOrdenHelper
{
    private static readonly Dictionary<string, int> Tallas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["XXS"] = 0,
        ["XS"] = 1,
        ["S"] = 2,
        ["M"] = 3,
        ["L"] = 4,
        ["XL"] = 5,
        ["XXL"] = 6,
        ["XXXL"] = 7,
        ["XXXXL"] = 8
    };

    public static ProductoOrdenClave CrearClave(string codigo, string nombre)
    {
        codigo = (codigo ?? string.Empty).Trim().ToUpperInvariant();
        nombre = (nombre ?? string.Empty).Trim().ToUpperInvariant();
        string codigoOrden = codigo;

        Match numeroMatch = PrimerNumeroRegex().Match(codigoOrden);
        string cliente = numeroMatch.Success ? codigoOrden[..numeroMatch.Index] : codigoOrden;
        string desdeNumero = numeroMatch.Success ? codigoOrden[numeroMatch.Index..] : string.Empty;

        Match numeroInicialMatch = NumeroInicialRegex().Match(desdeNumero);
        int? numero = numeroInicialMatch.Success ? int.Parse(numeroInicialMatch.Value) : null;
        string restoCodigo = numeroInicialMatch.Success ? desdeNumero[numeroInicialMatch.Length..] : string.Empty;

        string variante = restoCodigo;
        int ordenTalla = 0;
        int? tallaNumero = null;

        Match tallaNumericaMatch = TallaNumericaRegex().Match(restoCodigo);
        if (tallaNumericaMatch.Success)
        {
            variante = tallaNumericaMatch.Groups["variante"].Value;
            tallaNumero = int.Parse(tallaNumericaMatch.Groups["numero"].Value);
            ordenTalla = 100;
        }
        else
        {
            string? tallaTexto = ObtenerTallaTexto(restoCodigo);
            if (!string.IsNullOrWhiteSpace(tallaTexto))
            {
                variante = restoCodigo[..^tallaTexto.Length];
                ordenTalla = Tallas[tallaTexto];
            }
        }

        return new ProductoOrdenClave(
            cliente,
            numero.HasValue ? 0 : 1,
            numero ?? int.MaxValue,
            variante,
            ordenTalla,
            tallaNumero ?? int.MaxValue,
            codigoOrden,
            nombre);
    }

    private static string? ObtenerTallaTexto(string restoCodigo)
    {
        return Tallas.Keys
            .OrderByDescending(x => x.Length)
            .FirstOrDefault(talla => restoCodigo.EndsWith(talla, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex("[0-9]")]
    private static partial Regex PrimerNumeroRegex();

    [GeneratedRegex("^[0-9]+")]
    private static partial Regex NumeroInicialRegex();

    [GeneratedRegex("^(?<variante>.*)T(?<numero>[0-9]+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TallaNumericaRegex();
}

public sealed record ProductoOrdenClave(
    string Cliente,
    int NumeroNuloOrden,
    int Numero,
    string Variante,
    int OrdenTalla,
    int TallaNumero,
    string CodigoOrden,
    string NombreProducto);
