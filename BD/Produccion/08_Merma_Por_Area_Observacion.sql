SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.OrdenTrabajoMerma','Observacion') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenTrabajoMerma ADD Observacion NVARCHAR(500) NOT NULL
        CONSTRAINT DF_OrdenTrabajoMerma_Observacion DEFAULT(N'');
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_MERMA_REGISTRAR
    @IdDetalleArea BIGINT,
    @Cantidad DECIMAL(18,2),
    @Motivo NVARCHAR(200),
    @IdUsuarioSesion INT,
    @IdUsuarioAutoriza INT,
    @Observacion NVARCHAR(500)=N''
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @IdOT INT,@IdDet INT,@Pendiente DECIMAL(18,2),@Maneja BIT,@Estado VARCHAR(20);
        SELECT @IdOT=IdOrdenTrabajo,@IdDet=IdDetalleOT,@Pendiente=CantidadPendiente,@Maneja=ManejaMerma,@Estado=Estado
        FROM dbo.OrdenTrabajoDetalleArea WITH(UPDLOCK,HOLDLOCK) WHERE IdDetalleArea=@IdDetalleArea;
        IF @IdOT IS NULL THROW 51000,'No se encontro el producto en el area.',1;
        IF @Maneja<>1 THROW 51000,'El area no permite registrar merma.',1;
        IF @Estado IN('FINALIZADA','BLOQUEADA','ANULADA') OR @Pendiente<=0 THROW 51000,'El producto ya no tiene saldo pendiente en el area.',1;
        IF @Cantidad<=0 THROW 51000,'La cantidad de merma debe ser mayor a cero.',1;
        IF @Cantidad>@Pendiente THROW 51000,'La merma no puede superar el pendiente disponible.',1;
        IF NULLIF(LTRIM(RTRIM(@Motivo)),N'') IS NULL THROW 51000,'Ingrese el motivo de la merma.',1;
        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioSesion AND Estado=1)
           OR NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario=@IdUsuarioAutoriza AND Estado=1)
            THROW 51000,'El usuario de sesion o autorizador no es valido.',1;

        UPDATE dbo.OrdenTrabajoDetalleArea
        SET CantidadMerma=CantidadMerma+@Cantidad,
            Estado=CASE WHEN CantidadPendiente-@Cantidad=0 THEN 'FINALIZADA' ELSE 'PARCIAL' END,
            FechaFin=CASE WHEN CantidadPendiente-@Cantidad=0 THEN SYSDATETIME() ELSE FechaFin END
        WHERE IdDetalleArea=@IdDetalleArea;

        INSERT dbo.OrdenTrabajoMerma(IdOrdenTrabajo,IdDetalleOT,IdDetalleArea,Cantidad,Motivo,Observacion,IdUsuarioSesion,IdUsuarioAutoriza)
        VALUES(@IdOT,@IdDet,@IdDetalleArea,@Cantidad,LTRIM(RTRIM(@Motivo)),ISNULL(@Observacion,N''),@IdUsuarioSesion,@IdUsuarioAutoriza);

        COMMIT;
    END TRY
    BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
