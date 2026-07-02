namespace CorexProd.App.Services;

public static class DocumentoFiltroHelper
{
    public static bool CoincideFecha(DateTime emision, DateTime? cierre, DateTime? desde, DateTime? hasta, bool predeterminado, bool activo)
    {
        if (predeterminado)
        {
            DateTime inicioMes = new(DateTime.Today.Year, DateTime.Today.Month, 1);
            return activo || EstaEnRango(emision, inicioMes, DateTime.Today)
                || (cierre.HasValue && EstaEnRango(cierre.Value, inicioMes, DateTime.Today));
        }

        DateTime fechaDesde = (desde ?? DateTime.Today).Date;
        DateTime fechaHasta = (hasta ?? fechaDesde).Date;
        DateTime inicio = fechaDesde <= fechaHasta ? fechaDesde : fechaHasta;
        DateTime fin = fechaDesde <= fechaHasta ? fechaHasta : fechaDesde;
        return EstaEnRango(emision, inicio, fin) || (cierre.HasValue && EstaEnRango(cierre.Value, inicio, fin));
    }

    public static bool CoincideTexto(string valor, string? filtro) =>
        string.IsNullOrWhiteSpace(filtro) || valor.Contains(filtro.Trim(), StringComparison.OrdinalIgnoreCase);

    public static string Normalizar(string? valor) => (valor ?? string.Empty)
        .Trim()
        .ToUpperInvariant()
        .Replace('Á', 'A')
        .Replace('É', 'E')
        .Replace('Í', 'I')
        .Replace('Ó', 'O')
        .Replace('Ú', 'U')
        .Replace(' ', '_');

    private static bool EstaEnRango(DateTime fecha, DateTime inicio, DateTime fin) =>
        fecha.Date >= inicio && fecha.Date <= fin;
}
