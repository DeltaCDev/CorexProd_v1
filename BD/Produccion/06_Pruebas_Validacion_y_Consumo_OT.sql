SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @IdOCI2 INT=(SELECT TOP(1)d.IdOrdenCompraInterna FROM dbo.OrdenCompraInternaDetalle d JOIN dbo.FichaTecnica f ON f.IdProducto=d.IdProducto AND f.Estado=1 JOIN dbo.OrdenesCompraInterna o ON o.IdOrdenCompraInterna=d.IdOrdenCompraInterna WHERE o.TieneOrdenTrabajo=0 ORDER BY d.IdOrdenCompraInterna);
DECLARE @Usuario2 INT=(SELECT TOP(1)IdUsuario FROM dbo.Usuarios WHERE Estado=1 ORDER BY IdUsuario);
BEGIN TRAN;
BEGIN TRY
    UPDATE dbo.OrdenCompraInternaDetalle SET CantidadDespachada=0 WHERE IdOrdenCompraInterna=@IdOCI2;
    UPDATE dbo.OrdenesCompraInterna SET Estado='PENDIENTE',TieneOrdenTrabajo=0 WHERE IdOrdenCompraInterna=@IdOCI2;
    DECLARE @StockAntes DECIMAL(38,3)=(SELECT SUM(StockActual) FROM dbo.StockInsumos);
    EXEC dbo.USP_PRO_OT_VALIDAR_INSUMOS @IdOCI2;
    IF ISNULL(@StockAntes,0)<>ISNULL((SELECT SUM(StockActual) FROM dbo.StockInsumos),0) THROW 51000,'La consulta de validación descontó stock.',1;
    DECLARE @Plan dbo.TipoOTPlanificacion;
    INSERT @Plan SELECT IdOrdenCompraInternaDetalle,Cantidad FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOCI2;
    DECLARE @IdOT INT,@Numero VARCHAR(30);
    EXEC dbo.USP_PRO_OT_CREAR @IdOCI2,@Usuario2,N'PRUEBA CONSUMO NEGATIVO',@Plan,@IdOT OUTPUT,@Numero OUTPUT;
    IF NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajo WHERE IdOrdenTrabajo=@IdOT AND Estado='PENDIENTE') THROW 51000,'La OT no inició PENDIENTE.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.OrdenesCompraInterna WHERE IdOrdenCompraInterna=@IdOCI2 AND Estado='PROCESO') THROW 51000,'La OCI no cambió a PROCESO.',1;
    DECLARE @DetalleOT INT=(SELECT TOP(1)d.IdDetalleOT FROM dbo.OrdenTrabajoDetalle d WHERE d.IdOrdenTrabajo=@IdOT AND EXISTS(SELECT 1 FROM dbo.FichaTecnica f WHERE f.IdProducto=d.IdProducto AND f.Estado=1) ORDER BY d.IdDetalleOT);
    EXEC dbo.USP_PRO_OT_CONSUMO_CONFIRMAR @DetalleOT,@Usuario2;
    IF NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoConsumoInsumo WHERE IdDetalleOT=@DetalleOT) THROW 51000,'No se registró el consumo por OT.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoConsumoInsumo WHERE IdDetalleOT=@DetalleOT AND StockResultante<0) THROW 51000,'La prueba no permitió saldo negativo.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.KardexInsumos WHERE Observacion LIKE CONCAT('%',@Numero,'%')) THROW 51000,'No se registró la salida en kardex.',1;
    ROLLBACK;
    PRINT 'OK: OT PENDIENTE, OCI PROCESO, consumo negativo y kardex vinculados; consulta no descuenta stock.';
END TRY
BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
GO
