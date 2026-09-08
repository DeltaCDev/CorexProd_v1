using ClosedXML.Excel;
using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Helpers;
using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Data;

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string configPath = Path.Combine(root, "CorexProd.WPF", "bin", "Debug", "net8.0-windows", "CorexProd.WPF.dll.config");
Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(
    new ExeConfigurationFileMap { ExeConfigFilename = configPath }, ConfigurationUserLevel.None);
string connectionString = configuration.ConnectionStrings.ConnectionStrings["ConexionSQL"]?.ConnectionString
    ?? throw new InvalidOperationException("No se encontró ConexionSQL.");
string outputDirectory = Path.Combine(root, ".tmp", "etapa-13-1-5-validacion", "reportes");
Directory.CreateDirectory(outputDirectory);

foreach ((DateTime desde, DateTime hasta, string nombre) in new[]
{
    (new DateTime(2026, 8, 31), new DateTime(2026, 9, 6), "2026-08-31_a_2026-09-06"),
    (new DateTime(2026, 9, 28), new DateTime(2026, 10, 4), "2026-09-28_a_2026-10-04")
})
{
    List<ProgramacionPagoGeneralItem> pagos = args.Contains("--sample", StringComparer.OrdinalIgnoreCase)
        ? CrearPagosMuestra(desde)
        : ObtenerPagos(connectionString, desde, hasta);
    string excelPath = Path.Combine(outputDirectory, $"ProgramacionGeneral_{nombre}.xlsx");
    string pdfPath = Path.Combine(outputDirectory, $"ProgramacionGeneral_{nombre}.pdf");

    ProgramacionGeneralExcelExporter.Exportar(excelPath, pagos, desde, hasta, incluyePendientesAnteriores: false);
    ProgramacionGeneralPdfExporter.Exportar(pdfPath, pagos, desde, hasta, incluyePendientesAnteriores: false);

    List<string> cuotasExcel = LeerCuotasExcel(excelPath);
    string encabezadoCuota = LeerEncabezadoCuotaExcel(excelPath);
    List<string> letrasReales = pagos
        .Where(p => p.Categoria.Equals("LETRA", StringComparison.OrdinalIgnoreCase)
            && p.NumeroCuota.HasValue && p.TotalCuotas.HasValue)
        .Select(p => $"{p.NumeroCuota}/{p.TotalCuotas}")
        .Distinct()
        .ToList();

    Console.WriteLine($"{nombre}|registros={pagos.Count}|letras={string.Join(',', letrasReales)}|excel-encabezado={encabezadoCuota}|excel-cuotas={string.Join(',', cuotasExcel)}");
}

static List<ProgramacionPagoGeneralItem> ObtenerPagos(string connectionString, DateTime desde, DateTime hasta)
{
    List<ProgramacionPagoGeneralItem> pagos = [];
    using SqlConnection connection = new(connectionString);
    using SqlCommand command = new("USP_TES_PROGRAMACION_GENERAL_RANGO", connection)
    {
        CommandType = CommandType.StoredProcedure
    };
    command.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = desde.Date;
    command.Parameters.Add("@FechaFin", SqlDbType.Date).Value = hasta.Date;
    command.Parameters.Add("@IncluirPendientesAnteriores", SqlDbType.Bit).Value = false;
    command.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = DBNull.Value;
    command.Parameters.Add("@Categoria", SqlDbType.VarChar, 40).Value = DBNull.Value;
    command.Parameters.Add("@Moneda", SqlDbType.VarChar, 10).Value = DBNull.Value;
    command.Parameters.Add("@Texto", SqlDbType.VarChar, 160).Value = DBNull.Value;

    connection.Open();
    using SqlDataReader reader = command.ExecuteReader();
    while (reader.Read())
    {
        pagos.Add(new ProgramacionPagoGeneralItem
        {
            FechaVencimiento = reader.GetDateTime(reader.GetOrdinal("FechaVencimiento")),
            Categoria = LeerTexto(reader, "Categoria"),
            Subcategoria = LeerTexto(reader, "Subcategoria"),
            EntidadProveedor = LeerTexto(reader, "EntidadProveedor"),
            Titular = LeerTexto(reader, "Titular"),
            EmpresaResponsable = LeerTexto(reader, "EmpresaResponsable"),
            Concepto = LeerTexto(reader, "Concepto"),
            Referencia = LeerTexto(reader, "Referencia"),
            FechaDocumento = LeerFechaOpcional(reader, "FechaDocumento"),
            NumeroCuota = LeerEnteroOpcional(reader, "NumeroCuota"),
            TotalCuotas = LeerEnteroOpcional(reader, "TotalCuotas"),
            Moneda = LeerTexto(reader, "Moneda"),
            Importe = reader.GetDecimal(reader.GetOrdinal("Importe")),
            Pagado = reader.GetDecimal(reader.GetOrdinal("Pagado")),
            Saldo = reader.GetDecimal(reader.GetOrdinal("Saldo")),
            Estado = LeerTexto(reader, "Estado"),
            OrigenTipo = LeerTexto(reader, "OrigenTipo"),
            OrigenId = reader.GetInt32(reader.GetOrdinal("OrigenId")),
            OrigenDetalleId = reader.GetInt32(reader.GetOrdinal("OrigenDetalleId")),
            EsPendienteAnterior = reader.GetBoolean(reader.GetOrdinal("EsPendienteAnterior"))
        });
    }
    return pagos;
}

static List<string> LeerCuotasExcel(string path)
{
    using XLWorkbook workbook = new(path);
    return workbook.Worksheet("DETALLE").Column(7).CellsUsed()
        .Select(cell => cell.GetString())
        .Where(value => value.Contains('/') || value.Contains(" de "))
        .Distinct()
        .ToList();
}

static string LeerEncabezadoCuotaExcel(string path)
{
    using XLWorkbook workbook = new(path);
    return workbook.Worksheet("DETALLE").Column(7).CellsUsed()
        .Select(cell => cell.GetString())
        .FirstOrDefault(value => value == "CUOTA") ?? string.Empty;
}

static List<ProgramacionPagoGeneralItem> CrearPagosMuestra(DateTime fecha) =>
[
    new()
    {
        FechaVencimiento = fecha,
        Categoria = "LETRA",
        EntidadProveedor = "COLORTEX PERU S.A.",
        Referencia = "560135",
        FechaDocumento = new DateTime(2026, 6, 24),
        NumeroCuota = 4,
        TotalCuotas = 6,
        Moneda = "PEN",
        Importe = 5418.25m,
        Saldo = 5418.25m,
        Estado = "PENDIENTE"
    },
    new()
    {
        FechaVencimiento = fecha,
        Categoria = "FACTURA_CREDITO",
        EntidadProveedor = "Proveedor factura",
        NumeroCuota = 3,
        TotalCuotas = 5,
        Moneda = "PEN",
        Importe = 100m,
        Saldo = 100m,
        Estado = "PENDIENTE"
    },
    new()
    {
        FechaVencimiento = fecha,
        Categoria = "PRESTAMO",
        EntidadProveedor = "Banco",
        NumeroCuota = 5,
        TotalCuotas = 6,
        Moneda = "PEN",
        Importe = 250m,
        Saldo = 250m,
        Estado = "PENDIENTE"
    }
];

static string LeerTexto(SqlDataReader reader, string column) => reader[column] == DBNull.Value ? string.Empty : reader[column].ToString() ?? string.Empty;
static int? LeerEnteroOpcional(SqlDataReader reader, string column) => reader[column] == DBNull.Value ? null : Convert.ToInt32(reader[column]);
static DateTime? LeerFechaOpcional(SqlDataReader reader, string column) => reader[column] == DBNull.Value ? null : Convert.ToDateTime(reader[column]);
