using CorexProd.App.Models;

namespace CorexProd.App.Services;

public static class DemoData
{
    public static EmpresaInfo Empresa { get; } = new(
        0,
        "20123456789",
        "Delta Confecciones SRL",
        "999 888 777",
        "ventas@deltaconfecciones.demo",
        "Av. Industrial 123",
        string.Empty);

    public static IReadOnlyList<ProductoStock> Productos { get; } =
    [
        new(1, "MT009M", "MT009", "CONJUNTO MARCOBRE (CAMISA+PANT) - DRILL NARANJA TALLA M", "MARCOBRE", "Uniformes", 18),
        new(2, "MT009L", "MT009", "CONJUNTO MARCOBRE (CAMISA+PANT) - DRILL NARANJA TALLA L", "MARCOBRE", "Uniformes", 12),
        new(3, "SGS006M", "SGS006", "POLO SEGURIDAD SGS - TALLA M", "SGS DEL PERU S.A.C.", "Prendas", 35)
    ];

    public static IReadOnlyList<InsumoStock> Insumos { get; } =
    [
        new(1, "TEL-DRILL-NAR", "Tela drill naranja", "Tela", "m", 240),
        new(2, "BOT-18", "Boton 18 lineas", "Avios", "und", 1500),
        new(3, "HIL-NAR", "Hilo naranja 40/2", "Hilos", "cono", 28)
    ];

    public static IReadOnlyList<ProformaResumen> Proformas { get; } =
    [
        new(1, "PF-DEMO-001", DateTime.Today.AddDays(-2), DateTime.Today.AddDays(5), "OC-CLI-1001", "SGS DEL PERU S.A.C.", 2450, "Registrado", false),
        new(2, "PF-DEMO-002", DateTime.Today.AddDays(-1), DateTime.Today.AddDays(7), "OC-CLI-1002", "MARCOBRE S.A.C.", 3780, "Registrado", true)
    ];

    public static IReadOnlyList<OciResumen> Ocis { get; } =
    [
        new(1, "OCI-DEMO-001", "PF-DEMO-001", DateTime.Today.AddDays(-1), "OC-CLI-1001", "SGS DEL PERU S.A.C.", 2450, "PROCESO", false, false),
        new(2, "OCI-DEMO-002", "PF-DEMO-002", DateTime.Today, "OC-CLI-1002", "MARCOBRE S.A.C.", 3780, "Emitida", true, true)
    ];

    public static IReadOnlyList<GuiaInternaResumen> GuiasInternas { get; } =
    [
        new(1, "GI-DEMO-001", "OCI", 1, 1, "OCI-DEMO-001", "PF-DEMO-001", "OC-CLI-1001", DateTime.Today, 1, "Almacen Principal", Empresa.Ruc, Empresa.Nombre, "20601234567", "SGS DEL PERU S.A.C.", "Demo", "Demo", "Salida demo desde OCI", string.Empty, "Emitida", string.Empty, null, string.Empty, DateTime.Now),
        new(2, "GI-DEMO-002", "Manual", 0, null, string.Empty, string.Empty, string.Empty, DateTime.Today.AddDays(-1), 1, "Almacen Principal", Empresa.Ruc, Empresa.Nombre, string.Empty, "Consumo interno", "Demo", "Demo", "Salida manual demo", "Consumo interno", "Emitida", string.Empty, null, string.Empty, DateTime.Now.AddDays(-1))
    ];

    public static IReadOnlyList<OrdenTrabajoResumen> OrdenesTrabajo { get; } =
    [
        new(1, "OT-DEMO-001", "OCI-DEMO-001", "OC-CLI-1001", "Produccion", "SGS DEL PERU S.A.C.", DateTime.Today, "PENDIENTE", 2, 50, 15, 0.3m),
        new(2, "OT-DEMO-002", "OCI-DEMO-002", "OC-CLI-1002", "Produccion", "MARCOBRE S.A.C.", DateTime.Today.AddDays(-1), "PROCESO", 1, 35, 28, 0.8m)
    ];

    public static ProformaDetalleResponse ProformaDetalle(int id)
    {
        ProformaResumen p = Proformas.FirstOrDefault(x => x.IdProforma == id) ?? Proformas[0];
        return new(new(
            p.IdProforma,
            p.SerieNumero,
            p.FechaEmision,
            p.FechaVencimiento,
            p.OrdenCompraCliente,
            p.NombreCliente,
            "Ejemplo de proforma demo",
            2076.27m,
            0,
            373.73m,
            p.Total,
            p.Estado),
            DocumentoDetalles);
    }

    public static OciDetalleResponse OciDetalle(int id)
    {
        OciResumen o = Ocis.FirstOrDefault(x => x.IdOrdenCompraInterna == id) ?? Ocis[0];
        return new(new(
            o.IdOrdenCompraInterna,
            o.NumeroOci,
            o.NumeroProforma,
            o.FechaEmision,
            o.OrdenCompraCliente,
            o.NombreCliente,
            2076.27m,
            0,
            373.73m,
            o.Total,
            o.Estado),
            DocumentoDetalles);
    }

    public static GuiaInternaDetalleResponse GuiaInternaDetalle(int id)
    {
        GuiaInternaResumen guia = GuiasInternas.FirstOrDefault(x => x.IdGuiaInterna == id) ?? GuiasInternas[0];
        List<GuiaInternaDetalleApi> detalles =
        [
            new(1, 1, "MT009M", "CONJUNTO MARCOBRE TALLA M", 1, "UND", 25, 0, 25, 18, 0, 10, "Demo"),
            new(2, 2, "SGS006M", "POLO SEGURIDAD SGS TALLA M", 1, "UND", 35, 0, 35, 35, 0, 8, "Demo")
        ];

        return new(guia, detalles);
    }

    public static GuiaInternaManualPrepararResponse GuiaInternaManualPreparar => new(
        new("Manual", 1, "Almacen Principal", Empresa.Ruc, Empresa.Nombre, DateTime.Today),
        ProductosBusqueda);

    public static OrdenTrabajoDetalleResponse OrdenTrabajoDetalle(int id)
    {
        OrdenTrabajoResumen ot = OrdenesTrabajo.FirstOrDefault(x => x.IdOrdenTrabajo == id) ?? OrdenesTrabajo[0];
        List<OrdenTrabajoProducto> detalles =
        [
            new(1, ot.IdOrdenTrabajo, 1, "MT009M", "CONJUNTO MARCOBRE TALLA M", 25, 25, 10, 0, 15, "PENDIENTE"),
            new(2, ot.IdOrdenTrabajo, 2, "MT009L", "CONJUNTO MARCOBRE TALLA L", 25, 25, 5, 0, 20, "PENDIENTE")
        ];

        List<OrdenTrabajoArea> areas =
        [
            new(1, ot.IdOrdenTrabajo, 1, 1, "COR", "Corte", 1, true, false, false, "Manual", 0, 0, 0, 25, "PENDIENTE", "MT009M", "CONJUNTO MARCOBRE TALLA M"),
            new(2, ot.IdOrdenTrabajo, 2, 1, "COR", "Corte", 1, true, false, false, "Manual", 0, 0, 0, 25, "PENDIENTE", "MT009L", "CONJUNTO MARCOBRE TALLA L"),
            new(3, ot.IdOrdenTrabajo, 1, 2, "COS", "Costura", 2, false, false, true, "Manual", 10, 0, 0, 10, "PROCESO", "MT009M", "CONJUNTO MARCOBRE TALLA M")
        ];

        return new(new(
            ot.IdOrdenTrabajo,
            ot.NumeroOT,
            1,
            ot.NumeroOci,
            ot.OrdenCompraCliente,
            ot.TipoOT,
            1,
            ot.NombreCliente,
            ot.FechaEmision,
            ot.Estado,
            "Demo",
            "Demo",
            "Orden de trabajo demo",
            DateTime.Now),
            detalles,
            areas);
    }

    public static StockManualPrepararResponse StockManualPreparar { get; } = new(
        [new(1, "Proveedor Demo Textil")],
        [new(1, "Almacen Principal")],
        [new(1, "Nota de ingreso demo")]);

    public static IReadOnlyList<IngresoManualStockResumen> IngresosStockManual { get; } =
    [
        new(1, DateTime.Today, "Proveedor Demo Textil", "Nota de ingreso demo", "APP-DEMO-001", "Almacen Principal", "Ingreso inicial demo", "Abastecido", 60, "Demo", DateTime.Now.AddHours(-2)),
        new(2, DateTime.Today.AddDays(-1), "Proveedor Demo Textil", "Nota de ingreso demo", "APP-DEMO-002", "Almacen Principal", "Reposicion demo", "Abastecido", 18, "Demo", DateTime.Now.AddDays(-1))
    ];

    public static IngresoManualStockDetalleResponse IngresoStockManualDetalle(int id)
    {
        IngresoManualStockResumen ingreso = IngresosStockManual.FirstOrDefault(x => x.IdIngresoManualStock == id) ?? IngresosStockManual[0];
        List<IngresoManualStockDetalleApi> detalles =
        [
            new(1, ingreso.IdIngresoManualStock, 1, "MT009M", "CONJUNTO MARCOBRE TALLA M", "UND", 18, 25, 0, 0, 0),
            new(2, ingreso.IdIngresoManualStock, 2, "SGS006M", "POLO SEGURIDAD SGS TALLA M", "UND", 35, 35, 0, 0, 0)
        ];

        return new(new(
            ingreso.IdIngresoManualStock,
            ingreso.FechaEmision,
            ingreso.NombreProveedor,
            ingreso.NombreTipoDocumento,
            ingreso.NumeroDocumento,
            ingreso.NombreAlmacen,
            ingreso.Observacion,
            ingreso.Estado,
            ingreso.Total,
            0,
            ingreso.Total,
            ingreso.UsuarioCreador,
            ingreso.FechaCreacion,
            ingreso.UsuarioCreador,
            ingreso.FechaCreacion),
            detalles);
    }

    public static IReadOnlyList<ProductoStockBusquedaApi> ProductosBusqueda { get; } =
    [
        new(1, "MT009M", "CONJUNTO MARCOBRE TALLA M", "MARCOBRE", 1, "UND", 18),
        new(2, "SGS006M", "POLO SEGURIDAD SGS TALLA M", "SGS DEL PERU S.A.C.", 1, "UND", 35)
    ];

    private static IReadOnlyList<DocumentoDetalle> DocumentoDetalles { get; } =
    [
        new(1, 1, "MT009M", "CONJUNTO MARCOBRE (CAMISA+PANT) - DRILL NARANJA TALLA M", 25, 80, 0, 2000, "Demo", 18, 0),
        new(2, 2, "MT009L", "CONJUNTO MARCOBRE (CAMISA+PANT) - DRILL NARANJA TALLA L", 10, 78, 0, 780, "Demo", 12, 0)
    ];
}
