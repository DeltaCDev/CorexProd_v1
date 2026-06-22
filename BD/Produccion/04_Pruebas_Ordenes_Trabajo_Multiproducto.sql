SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @IdOCI INT=(SELECT TOP(1)o.IdOrdenCompraInterna FROM dbo.OrdenesCompraInterna o WHERE o.Estado<>'Anulado' AND o.TieneOrdenTrabajo=0 AND (SELECT COUNT(*) FROM dbo.OrdenCompraInternaDetalle d WHERE d.IdOrdenCompraInterna=o.IdOrdenCompraInterna)>=3 ORDER BY o.IdOrdenCompraInterna DESC);
DECLARE @Usuario INT=(SELECT TOP(1)IdUsuario FROM dbo.Usuarios WHERE Estado=1 ORDER BY IdUsuario);
DECLARE @ProductosTest INT=(SELECT COUNT(*) FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOCI);
IF @IdOCI IS NULL THROW 51000,'No existe una OCI multiproducto disponible para la prueba.',1;

BEGIN TRAN;
BEGIN TRY
    UPDATE dbo.OrdenCompraInternaDetalle SET CantidadDespachada=0 WHERE IdOrdenCompraInterna=@IdOCI;
    UPDATE dbo.OrdenesCompraInterna SET Estado='PENDIENTE',TieneOrdenTrabajo=0 WHERE IdOrdenCompraInterna=@IdOCI;
    DECLARE @Plan dbo.TipoOTPlanificacion;
    INSERT @Plan SELECT IdOrdenCompraInternaDetalle,Cantidad-CantidadDespachada FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOCI AND Cantidad>CantidadDespachada;
    DECLARE @IdOT INT,@Numero VARCHAR(30);
    EXEC dbo.USP_PRO_OT_CREAR @IdOCI,@Usuario,N'PRUEBA AUTOMATIZADA',@Plan,@IdOT OUTPUT,@Numero OUTPUT;
    IF (SELECT COUNT(*) FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@IdOT)<>@ProductosTest THROW 51000,'No se crearon todos los detalles de producto.',1;
    IF (SELECT COUNT(*) FROM dbo.OrdenTrabajoDetalleArea WHERE IdOrdenTrabajo=@IdOT)<>@ProductosTest*(SELECT COUNT(*) FROM dbo.AreaProduccion WHERE Activo=1) THROW 51000,'No se copiaron las áreas para cada producto.',1;

    DECLARE @Lanza dbo.TipoOTLanzamiento;
    INSERT @Lanza SELECT IdDetalleOT,CantidadPlanificada,NULL,NULL FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@IdOT;
    EXEC dbo.USP_PRO_OT_LANZAR @IdOT,@Usuario,@Usuario,@Lanza;

    DECLARE @AreaInicio INT=(SELECT IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND EsInicio=1);
    DECLARE @Envio dbo.TipoOTTransferencia;
    INSERT @Envio SELECT d.IdDetalleOT,a.CantidadPendiente FROM dbo.OrdenTrabajoDetalle d JOIN dbo.OrdenTrabajoDetalleArea a ON a.IdDetalleOT=d.IdDetalleOT AND a.IdAreaProduccion=@AreaInicio WHERE d.IdOrdenTrabajo=@IdOT;
    DECLARE @Operacion BIGINT;
    EXEC dbo.USP_PRO_OT_TRANSFERIR @IdOT,@AreaInicio,@Usuario,@Usuario,N'ENVIO TOTAL',@Envio,@Operacion OUTPUT;
    IF (SELECT COUNT(*) FROM dbo.OrdenTrabajoTransferenciaDetalle WHERE IdOperacionTransferencia=@Operacion)<>@ProductosTest THROW 51000,'Enviar todos no generó un detalle por producto.',1;

    DECLARE @AreaParcial INT=(SELECT TOP(1)IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND OrdenSecuencia>(SELECT OrdenSecuencia FROM dbo.AreaProduccion WHERE IdAreaProduccion=@AreaInicio) ORDER BY OrdenSecuencia);
    DECLARE @IdAreaMerma BIGINT=(SELECT TOP(1)IdDetalleArea FROM dbo.OrdenTrabajoDetalleArea WHERE IdOrdenTrabajo=@IdOT AND IdAreaProduccion=@AreaParcial AND ManejaMerma=1 AND CantidadPendiente>=1 ORDER BY IdDetalleOT);
    IF @IdAreaMerma IS NOT NULL
    BEGIN
        EXEC dbo.USP_PRO_OT_MERMA_REGISTRAR @IdAreaMerma,0.50,N'MERMA DE PRUEBA',@Usuario,@Usuario;
        IF NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoMerma WHERE IdDetalleArea=@IdAreaMerma AND Cantidad=0.50) THROW 51000,'La merma no quedó asociada al producto y área.',1;
    END;
    DELETE FROM @Envio;
    INSERT @Envio SELECT IdDetalleOT,CASE WHEN CantidadPendiente>1 THEN CantidadPendiente/2 ELSE CantidadPendiente END FROM dbo.OrdenTrabajoDetalleArea WHERE IdOrdenTrabajo=@IdOT AND IdAreaProduccion=@AreaParcial;
    EXEC dbo.USP_PRO_OT_TRANSFERIR @IdOT,@AreaParcial,@Usuario,@Usuario,N'PARCIALES DIFERENTES',@Envio,@Operacion OUTPUT;
    IF EXISTS(SELECT 1 FROM @Envio e JOIN dbo.OrdenTrabajoDetalleArea a ON a.IdDetalleOT=e.IdDetalleOT AND a.IdAreaProduccion=@AreaParcial WHERE a.CantidadEnviada<>e.Cantidad) THROW 51000,'Los saldos parciales no se actualizaron de forma independiente.',1;

    ROLLBACK;
    PRINT 'OK: OT multiproducto, copia de áreas, envío total separado, parciales y merma por producto.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;

-- La operación inválida debe revertir el grupo completo.
BEGIN TRAN;
BEGIN TRY
    UPDATE dbo.OrdenCompraInternaDetalle SET CantidadDespachada=0 WHERE IdOrdenCompraInterna=@IdOCI;
    UPDATE dbo.OrdenesCompraInterna SET Estado='PENDIENTE',TieneOrdenTrabajo=0 WHERE IdOrdenCompraInterna=@IdOCI;
    DECLARE @PlanError dbo.TipoOTPlanificacion;
    INSERT @PlanError SELECT IdOrdenCompraInternaDetalle,Cantidad-CantidadDespachada FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOCI AND Cantidad>CantidadDespachada;
    DECLARE @IdOTError INT,@NumeroError VARCHAR(30);
    EXEC dbo.USP_PRO_OT_CREAR @IdOCI,@Usuario,N'PRUEBA ROLLBACK',@PlanError,@IdOTError OUTPUT,@NumeroError OUTPUT;
    DECLARE @LanzaError dbo.TipoOTLanzamiento;
    INSERT @LanzaError SELECT IdDetalleOT,CantidadPlanificada,NULL,NULL FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@IdOTError;
    EXEC dbo.USP_PRO_OT_LANZAR @IdOTError,@Usuario,@Usuario,@LanzaError;
    DECLARE @AreaError INT=(SELECT IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND EsInicio=1);
    DECLARE @EnvioError dbo.TipoOTTransferencia;
    INSERT @EnvioError SELECT IdDetalleOT,CASE WHEN ROW_NUMBER()OVER(ORDER BY IdDetalleOT)=1 THEN CantidadLanzada+1 ELSE CantidadLanzada END FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@IdOTError;
    DECLARE @OperacionError BIGINT;
    EXEC dbo.USP_PRO_OT_TRANSFERIR @IdOTError,@AreaError,@Usuario,@Usuario,N'DEBE FALLAR',@EnvioError,@OperacionError OUTPUT;
    THROW 51000,'La transferencia inválida fue aceptada.',1;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    IF ERROR_MESSAGE()='La transferencia inválida fue aceptada.' THROW;
    PRINT CONCAT('OK: grupo inválido rechazado y revertido: ',ERROR_MESSAGE());
END CATCH;
GO

-- Compatibilidad: una OT de un producto debe recorrer todas las áreas y terminar.
SET NOCOUNT ON;
SET XACT_ABORT OFF;
DECLARE @OCIUno INT=(SELECT TOP(1)o.IdOrdenCompraInterna FROM dbo.OrdenesCompraInterna o WHERE o.Estado<>'Anulado' AND o.TieneOrdenTrabajo=0 AND EXISTS(SELECT 1 FROM dbo.OrdenCompraInternaDetalle d WHERE d.IdOrdenCompraInterna=o.IdOrdenCompraInterna) ORDER BY o.IdOrdenCompraInterna DESC);
DECLARE @UsuarioUno INT=(SELECT TOP(1)IdUsuario FROM dbo.Usuarios WHERE Estado=1 ORDER BY IdUsuario);
BEGIN TRAN;
BEGIN TRY
    UPDATE dbo.OrdenCompraInternaDetalle SET CantidadDespachada=0 WHERE IdOrdenCompraInterna=@OCIUno;
    UPDATE dbo.OrdenesCompraInterna SET Estado='PENDIENTE',TieneOrdenTrabajo=0 WHERE IdOrdenCompraInterna=@OCIUno;
    DECLARE @DetalleConservar INT=(SELECT TOP(1)IdOrdenCompraInternaDetalle FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@OCIUno AND Cantidad>CantidadDespachada ORDER BY IdOrdenCompraInternaDetalle);
    UPDATE dbo.OrdenCompraInternaDetalle SET CantidadDespachada=Cantidad WHERE IdOrdenCompraInterna=@OCIUno AND IdOrdenCompraInternaDetalle<>@DetalleConservar;
    DECLARE @PlanUno dbo.TipoOTPlanificacion;
    INSERT @PlanUno SELECT TOP(1)IdOrdenCompraInternaDetalle,Cantidad-CantidadDespachada FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@OCIUno AND Cantidad>CantidadDespachada ORDER BY IdOrdenCompraInternaDetalle;
    DECLARE @OTUno INT,@NumeroUno VARCHAR(30);
    EXEC dbo.USP_PRO_OT_CREAR @OCIUno,@UsuarioUno,N'PRUEBA UN PRODUCTO',@PlanUno,@OTUno OUTPUT,@NumeroUno OUTPUT;
    DECLARE @LanzaUno dbo.TipoOTLanzamiento;
    INSERT @LanzaUno SELECT IdDetalleOT,CantidadPlanificada,NULL,NULL FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@OTUno;
    EXEC dbo.USP_PRO_OT_LANZAR @OTUno,@UsuarioUno,@UsuarioUno,@LanzaUno;
    DECLARE @OrdenActual INT=(SELECT MIN(OrdenSecuencia) FROM dbo.AreaProduccion WHERE Activo=1),@OrdenFinal INT=(SELECT MAX(OrdenSecuencia) FROM dbo.AreaProduccion WHERE Activo=1),@AreaActual INT,@OperacionUno BIGINT;
    DECLARE @EnvioUno dbo.TipoOTTransferencia;
    WHILE @OrdenActual<@OrdenFinal
    BEGIN
        SET @AreaActual=(SELECT IdAreaProduccion FROM dbo.AreaProduccion WHERE Activo=1 AND OrdenSecuencia=@OrdenActual);
        DELETE FROM @EnvioUno;
        INSERT @EnvioUno SELECT IdDetalleOT,CantidadPendiente FROM dbo.OrdenTrabajoDetalleArea WHERE IdOrdenTrabajo=@OTUno AND IdAreaProduccion=@AreaActual AND CantidadPendiente>0;
        EXEC dbo.USP_PRO_OT_TRANSFERIR @OTUno,@AreaActual,@UsuarioUno,@UsuarioUno,N'RECORRIDO COMPLETO',@EnvioUno,@OperacionUno OUTPUT;
        SET @OrdenActual=(SELECT MIN(OrdenSecuencia) FROM dbo.AreaProduccion WHERE Activo=1 AND OrdenSecuencia>@OrdenActual);
    END;
    IF NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajoDetalle WHERE IdOrdenTrabajo=@OTUno AND Estado='TERMINADO' AND CantidadPendiente=0) THROW 51000,'La OT de un producto no terminó correctamente.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.OrdenTrabajo WHERE IdOrdenTrabajo=@OTUno AND Estado='TERMINADA') THROW 51000,'La cabecera de la OT de un producto no quedó terminada.',1;
    ROLLBACK;
    PRINT 'OK: OT de un producto compatible y recorrido completo hasta TERMINADA.';
END TRY
BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
GO
