using System.Text.RegularExpressions;

namespace CorexProd.Entidad.Utilidades
{
    public static partial class CodigoModeloProducto
    {
        public static string Obtener(string? codigoProducto)
        {
            if (string.IsNullOrWhiteSpace(codigoProducto))
                return string.Empty;

            string codigo = codigoProducto.Trim().ToUpperInvariant();

            Match tallaNumerica = TallaNumericaRegex().Match(codigo);
            if (tallaNumerica.Success)
                return codigo[..tallaNumerica.Index];

            Match tallaAlfabetica = TallaAlfabeticaRegex().Match(codigo);
            if (tallaAlfabetica.Success)
                return codigo[..tallaAlfabetica.Index];

            return codigo;
        }

        [GeneratedRegex(@"(?<=\d)T\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex TallaNumericaRegex();

        [GeneratedRegex(@"(?:XXXL|XXL|XL|XS|L|M|S)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex TallaAlfabeticaRegex();
    }
}
