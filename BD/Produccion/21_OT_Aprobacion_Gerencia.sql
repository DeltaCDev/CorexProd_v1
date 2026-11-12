/* Aprobación de OT por Gerencia antes de iniciar producción. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.OrdenTrabajo', 'EstadoAprobacion') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD EstadoAprobacion VARCHAR(25) NOT NULL CONSTRAINT DF_OrdenTrabajo_EstadoAprobacion DEFAULT('PENDIENTE_APROBACION');
IF COL_LENGTH('dbo.OrdenTrabajo', 'IdUsuarioAprobacionGerencia') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD IdUsuarioAprobacionGerencia INT NULL;
IF COL_LENGTH('dbo.OrdenTrabajo', 'FechaAprobacionGerencia') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD FechaAprobacionGerencia DATETIME2(0) NULL;
IF COL_LENGTH('dbo.OrdenTrabajo', 'MotivoRechazoAprobacion') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD MotivoRechazoAprobacion VARCHAR(500) NULL;
IF COL_LENGTH('dbo.OrdenTrabajo', 'ObservacionAprobacion') IS NULL
    ALTER TABLE dbo.OrdenTrabajo ADD ObservacionAprobacion VARCHAR(500) NULL;
GO

/* Las OT históricas que ya estaban en proceso o terminadas no deben quedar
   bloqueadas por esta nueva aprobación. Las OT pendientes y no iniciadas sí
   quedan pendientes de revisión de Gerencia. */
UPDATE O
SET EstadoAprobacion = CASE
        WHEN UPPER(ISNULL(O.Estado, '')) IN ('EN_PROCESO', 'PROCESO', 'PARCIAL', 'TERMINADO', 'TERMINADA', 'FINALIZADO', 'FINALIZADA')
             OR EXISTS (
                 SELECT 1
                 FROM dbo.OrdenTrabajoDetalle D
                 WHERE D.IdOrdenTrabajo = O.IdOrdenTrabajo
                   AND ISNULL(D.CantidadLanzada, 0) > 0
             )
        THEN 'APROBADA'
        ELSE 'PENDIENTE_APROBACION'
    END
FROM dbo.OrdenTrabajo AS O
WHERE EstadoAprobacion = 'PENDIENTE_APROBACION';
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_APROBAR_GERENCIA
    @IdOrdenTrabajo INT,
    @IdUsuarioGerencia INT,
    @Observacion VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioGerencia AND Estado=1 AND AprobacionOs=1)
            THROW 51000, 'El usuario no tiene autorización de Gerencia.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.OrdenTrabajo WHERE IdOrdenTrabajo=@IdOrdenTrabajo)
            THROW 51000, 'La OT no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.OrdenTrabajo WHERE IdOrdenTrabajo=@IdOrdenTrabajo AND UPPER(Estado) IN ('ANULADO','ANULADA'))
            THROW 51000, 'No se puede aprobar una OT anulada.', 1;
        UPDATE dbo.OrdenTrabajo
        SET EstadoAprobacion='APROBADA', IdUsuarioAprobacionGerencia=@IdUsuarioGerencia,
            FechaAprobacionGerencia=SYSDATETIME(), MotivoRechazoAprobacion=NULL,
            ObservacionAprobacion=NULLIF(LTRIM(RTRIM(ISNULL(@Observacion,''))), '')
        WHERE IdOrdenTrabajo=@IdOrdenTrabajo;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_RECHAZAR_GERENCIA
    @IdOrdenTrabajo INT,
    @IdUsuarioGerencia INT,
    @Motivo VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(ISNULL(@Motivo,''))), '') IS NULL
        THROW 51000, 'Ingrese el motivo del rechazo.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioGerencia AND Estado=1 AND AprobacionOs=1)
        THROW 51000, 'El usuario no tiene autorización de Gerencia.', 1;
    UPDATE dbo.OrdenTrabajo
    SET EstadoAprobacion='RECHAZADA', IdUsuarioAprobacionGerencia=@IdUsuarioGerencia,
        FechaAprobacionGerencia=SYSDATETIME(), MotivoRechazoAprobacion=@Motivo
    WHERE IdOrdenTrabajo=@IdOrdenTrabajo
      AND UPPER(Estado) NOT IN ('ANULADO','ANULADA');
END;
GO
