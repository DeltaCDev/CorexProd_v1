using System.Text.Json.Serialization;

namespace CorexProd.App.Models;

public sealed record LoginRequest(string Usuario, string Clave);

public sealed record LoginResponse(
    LoginUser Usuario,
    IReadOnlyList<string> Menus,
    DateTime FechaHora,
    string Mensaje);

public sealed record LoginUser(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    int IdRol,
    string NombreRol);

public sealed record HealthResponse(
    string Estado,
    string BaseDatos,
    string Servidor,
    DateTime FechaHora);

public sealed record ApiListResponse<T>(
    int Total,
    IReadOnlyList<T> Items);

public sealed record ProductoStock(
    int IdProducto,
    string Codigo,
    string CodigoModelo,
    string Producto,
    string EtiquetaCliente,
    string Categoria,
    decimal StockActual);

public sealed record InsumoStock(
    int IdInsumo,
    string Codigo,
    string Insumo,
    string Categoria,
    string Unidad,
    decimal StockActual);

public sealed record ProformaResumen(
    int IdProforma,
    string SerieNumero,
    DateTime FechaEmision,
    DateTime FechaVencimiento,
    string OrdenCompraCliente,
    string NombreCliente,
    decimal Total,
    string Estado,
    bool TieneOrdenCompraInterna);

public sealed record ProformaDetalleResponse(
    ProformaCabecera Cabecera,
    IReadOnlyList<DocumentoDetalle> Detalles);

public sealed record ProformaCabecera(
    int IdProforma,
    string SerieNumero,
    DateTime FechaEmision,
    DateTime FechaVencimiento,
    string OrdenCompraCliente,
    string NombreCliente,
    string Observacion,
    decimal Subtotal,
    decimal Descuento,
    decimal Igv,
    decimal Total,
    string Estado);

public sealed record OciResumen(
    int IdOrdenCompraInterna,
    string NumeroOci,
    string NumeroProforma,
    DateTime FechaEmision,
    string OrdenCompraCliente,
    string NombreCliente,
    decimal Total,
    string Estado,
    bool TieneGuiaSalida,
    bool TieneOrdenTrabajo);

public sealed record OciDetalleResponse(
    OciCabecera Cabecera,
    IReadOnlyList<DocumentoDetalle> Detalles);

public sealed record OciCabecera(
    int IdOrdenCompraInterna,
    string NumeroOci,
    string NumeroProforma,
    DateTime FechaEmision,
    string OrdenCompraCliente,
    string NombreCliente,
    decimal Subtotal,
    decimal Descuento,
    decimal Igv,
    decimal Total,
    string Estado);

public sealed record DocumentoDetalle(
    string CodigoProducto,
    string NombreProducto,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal Importe,
    string Observacion,
    decimal? StockActual = null,
    decimal? CantidadDespachada = null);

public sealed record ProformaPrepararResponse(
    string SiguienteNumero,
    IReadOnlyList<ClienteApi> Clientes,
    IReadOnlyList<ProductoProformaApi> Productos);

public sealed record ClienteApi(
    int IdCliente,
    string NumeroDocumento,
    string NombreRazonSocial)
{
    public string Display => string.IsNullOrWhiteSpace(NumeroDocumento)
        ? NombreRazonSocial
        : $"{NumeroDocumento} - {NombreRazonSocial}";

    public override string ToString() => Display;
}

public sealed record ProductoProformaApi(
    int IdProducto,
    string Codigo,
    string NombreProducto,
    string EtiquetaCliente,
    int IdUnidadMedida,
    string NombreUnidad)
{
    public string Display => string.IsNullOrWhiteSpace(EtiquetaCliente)
        ? $"{Codigo} | {NombreProducto}"
        : $"{Codigo} | {NombreProducto} | {EtiquetaCliente}";

    public override string ToString() => Display;
}

public sealed record ProformaGuardarRequest(
    int IdCliente,
    DateTime FechaVencimiento,
    string OrdenCompraCliente,
    string Observacion,
    decimal IgvPorcentaje,
    string CondicionTributaria,
    string Usuario,
    IReadOnlyList<ProformaGuardarDetalleRequest> Detalles);

public sealed record ProformaGuardarDetalleRequest(
    int IdProducto,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    string Observacion);

public sealed record DocumentoAccionRequest(
    string Usuario,
    string Motivo);

public sealed record ProformaGuardarResponse(
    string Mensaje,
    int IdProforma,
    string SerieNumero,
    decimal Subtotal,
    decimal Igv,
    decimal Total);

public sealed record DocumentoAccionResponse(string Mensaje);

public sealed record StockManualPrepararResponse(
    IReadOnlyList<ProveedorStockApi> Proveedores,
    IReadOnlyList<AlmacenStockApi> Almacenes,
    IReadOnlyList<TipoDocumentoStockApi> TiposDocumento);

public sealed record ProveedorStockApi(int IdProveedor, string NombreRazonSocial)
{
    public override string ToString() => NombreRazonSocial;
}

public sealed record AlmacenStockApi(int IdAlmacen, string NombreAlmacen)
{
    public override string ToString() => NombreAlmacen;
}

public sealed record TipoDocumentoStockApi(int IdTipoDocumento, string NombreTipoDocumento)
{
    public override string ToString() => NombreTipoDocumento;
}

public sealed record ProductoStockBusquedaApi(
    int IdProducto,
    string Codigo,
    string NombreProducto,
    string EtiquetaCliente,
    int IdUnidadMedida,
    string NombreUnidad,
    decimal StockActual)
{
    public string ProductoBusqueda => string.IsNullOrWhiteSpace(EtiquetaCliente)
        ? $"{Codigo} | {NombreProducto} | Stock: {StockActual:N2}"
        : $"{Codigo} | {NombreProducto} | {EtiquetaCliente} | Stock: {StockActual:N2}";
}

public sealed record IngresoManualStockRequest(
    int IdProveedor,
    int IdTipoDocumento,
    int IdAlmacen,
    string Observacion,
    string Usuario,
    IReadOnlyList<IngresoManualStockDetalleRequest> Detalles);

public sealed record IngresoManualStockDetalleRequest(
    int IdProducto,
    decimal Cantidad);

public sealed record IngresoManualStockResponse(
    string Mensaje,
    int IdIngresoManualStock,
    string NumeroDocumento,
    decimal CantidadTotal);

public sealed record OrdenTrabajoResumen(
    int IdOrdenTrabajo,
    string NumeroOT,
    string NumeroOci,
    string OrdenCompraCliente,
    string TipoOT,
    string NombreCliente,
    DateTime FechaEmision,
    string Estado,
    int CantidadProductos,
    decimal TotalPlanificado,
    decimal TotalLanzado,
    decimal Avance);

public sealed record OrdenTrabajoDetalleResponse(
    OrdenTrabajoCabecera Cabecera,
    IReadOnlyList<OrdenTrabajoProducto> Detalles,
    IReadOnlyList<OrdenTrabajoArea> Areas);

public sealed record OrdenTrabajoCabecera(
    int IdOrdenTrabajo,
    string NumeroOT,
    int IdOrdenCompraInterna,
    string NumeroOci,
    string OrdenCompraCliente,
    string TipoOT,
    int IdCliente,
    string NombreCliente,
    DateTime FechaEmision,
    string Estado,
    string UsuarioCreacion,
    string UsuarioAutoriza,
    string Observacion,
    DateTime FechaRegistro);

public sealed record OrdenTrabajoProducto(
    int IdDetalleOT,
    int IdOrdenTrabajo,
    int IdProducto,
    string CodigoProducto,
    string NombreProducto,
    decimal CantidadRequerida,
    decimal CantidadPlanificada,
    decimal CantidadLanzada,
    decimal CantidadProducida,
    decimal CantidadPendiente,
    string Estado);

public sealed record OrdenTrabajoArea(
    long IdDetalleArea,
    int IdOrdenTrabajo,
    int IdDetalleOT,
    int IdAreaProduccion,
    string CodigoArea,
    string NombreArea,
    int OrdenSecuencia,
    bool EsInicio,
    bool EsTermino,
    bool ManejaMerma,
    string ModoEnvio,
    decimal CantidadRecibida,
    decimal CantidadEnviada,
    decimal CantidadMerma,
    decimal CantidadPendiente,
    string Estado,
    string CodigoProducto,
    string NombreProducto)
{
    public bool Disponible => CantidadPendiente > 0
        && !Estado.Equals("FINALIZADA", StringComparison.OrdinalIgnoreCase)
        && !Estado.Equals("BLOQUEADA", StringComparison.OrdinalIgnoreCase)
        && !Estado.Equals("ANULADA", StringComparison.OrdinalIgnoreCase);

    public string Producto => $"{CodigoProducto} - {NombreProducto}";
    public string Cantidades => $"Recibido {CantidadRecibida:N2} | Enviado {CantidadEnviada:N2} | Merma {CantidadMerma:N2} | Pend. {CantidadPendiente:N2}";
    public string AccionPrincipal => EsTermino ? "Terminar" : "Transferir";
}

public sealed record OrdenTrabajoLanzarRequest(
    int IdUsuarioSesion,
    int IdUsuarioAutoriza,
    IReadOnlyList<OrdenTrabajoLanzamientoDetalleRequest> Detalles);

public sealed record OrdenTrabajoLanzamientoDetalleRequest(
    int IdDetalleOT,
    decimal CantidadLanzada,
    string Motivo,
    string Observacion);

public sealed record OrdenTrabajoTransferirRequest(
    int IdAreaProduccion,
    int IdUsuarioSesion,
    int IdUsuarioAutoriza,
    bool EsTerminacion,
    string Observacion,
    IReadOnlyList<OrdenTrabajoTransferenciaDetalleRequest> Detalles);

public sealed record OrdenTrabajoTransferenciaDetalleRequest(
    int IdDetalleOT,
    decimal Cantidad);

public sealed record OrdenTrabajoMermaRequest(
    long IdDetalleArea,
    decimal Cantidad,
    string Motivo,
    string Observacion,
    int IdUsuarioSesion,
    int IdUsuarioAutoriza);

public sealed record OperacionOrdenTrabajoResponse(
    string Mensaje,
    long? IdOperacion);

public sealed record ApiProblem(
    string? Mensaje,
    string? Title,
    string? Detail,
    [property: JsonPropertyName("status")] int? Status);
