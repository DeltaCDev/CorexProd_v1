SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/*
    Compatibilidad de anulación de OCI.
    Algunas bases no tienen OrdenesCompraInterna.IdProforma.
    La anulación de la OCI no debe fallar por esa diferencia de modelo.
*/
CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_ANULAR
    @IdOrdenCompraInterna INT,
    @MotivoAnulacion VARCHAR(500),
    @UsuarioAnulacion VARCHAR(80),
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MotivoAnulacion = LTRIM(RTRIM(ISNULL(@MotivoAnulacion, '')));
    SET @UsuarioAnulacion = LTRIM(RTRIM(ISNULL(@UsuarioAnulacion, '')));

    IF @MotivoAnulacion = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion.';
        RETURN;
    END;

    IF @UsuarioAnulacion = ''
    BEGIN
        SET @Mensaje = 'No se pudo identificar al usuario que anula la OCI.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.OrdenesCompraInterna WITH (UPDLOCK)
        SET Estado = 'Anulado',
            MotivoAnulacion = @MotivoAnulacion,
            UsuarioAnulacion = @UsuarioAnulacion,
            FechaAnulacion = GETDATE()
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
          AND Estado <> 'Anulado'
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.OrdenTrabajo OT
              WHERE OT.IdOrdenCompraInterna = @IdOrdenCompraInterna
                AND UPPER(OT.Estado) NOT IN ('ANULADA', 'ANULADO')
          )
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.GuiasInternas G
              WHERE G.IdOrdenCompraInterna = @IdOrdenCompraInterna
                AND G.Estado NOT IN ('Anulado', 'Anulada')
          );

        IF @@ROWCOUNT = 1
        BEGIN
            IF OBJECT_ID('dbo.USP_ALM_STOCK_RESERVA_ANULAR_SCOPE', 'P') IS NOT NULL
            BEGIN
                EXEC dbo.USP_ALM_STOCK_RESERVA_ANULAR_SCOPE
                    @IdOrdenCompraInterna = @IdOrdenCompraInterna,
                    @IdOrdenTrabajo = NULL,
                    @DocumentoReferencia = NULL,
                    @Usuario = @UsuarioAnulacion,
                    @Observacion = @MotivoAnulacion;
            END;

            /*
                Solo se actualiza la Proforma cuando la instalación realmente
                tiene la relación IdProforma. Se usa SQL dinámico para que el
                procedimiento compile también en bases sin esa columna.
            */
            IF COL_LENGTH('dbo.OrdenesCompraInterna', 'IdProforma') IS NOT NULL
               AND OBJECT_ID('dbo.Proformas', 'U') IS NOT NULL
            BEGIN
                EXEC sys.sp_executesql N'
                    UPDATE P
                    SET TieneOrdenCompraInterna = CAST(CASE WHEN EXISTS
                        (
                            SELECT 1
                            FROM dbo.OrdenesCompraInterna O
                            WHERE O.IdProforma = P.IdProforma
                              AND UPPER(O.Estado) <> ''ANULADO''
                        ) THEN 1 ELSE 0 END AS BIT),
                        Estado = CASE
                            WHEN P.Estado = ''Anulado'' THEN ''Anulado''
                            WHEN EXISTS
                            (
                                SELECT 1
                                FROM dbo.OrdenesCompraInterna O
                                WHERE O.IdProforma = P.IdProforma
                                  AND UPPER(O.Estado) <> ''ANULADO''
                            ) THEN ''Registrado''
                            ELSE ''Emitido''
                        END
                    FROM dbo.Proformas P
                    WHERE P.IdProforma =
                    (
                        SELECT O.IdProforma
                        FROM dbo.OrdenesCompraInterna O
                        WHERE O.IdOrdenCompraInterna = @IdOCI
                    );',
                    N'@IdOCI INT',
                    @IdOCI = @IdOrdenCompraInterna;
            END;

            COMMIT TRANSACTION;
            SET @Mensaje = 'OCI anulada correctamente. Las reservas activas fueron anuladas.';
            RETURN;
        END;

        ROLLBACK TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraInterna WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna)
            SET @Mensaje = 'No se encontro la OCI seleccionada.';
        ELSE IF EXISTS
        (
            SELECT 1 FROM dbo.OrdenesCompraInterna
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna AND Estado = 'Anulado'
        )
            SET @Mensaje = 'La OCI ya se encuentra anulada.';
        ELSE IF EXISTS
        (
            SELECT 1 FROM dbo.GuiasInternas
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND Estado NOT IN ('Anulado', 'Anulada')
        )
            SET @Mensaje = 'No se puede anular la OCI porque tiene una Guia Interna emitida. Primero debe anular la guia.';
        ELSE IF EXISTS
        (
            SELECT 1 FROM dbo.OrdenTrabajo
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
              AND UPPER(Estado) NOT IN ('ANULADA', 'ANULADO')
        )
            SET @Mensaje = 'No se puede anular la OCI porque tiene una Orden de Trabajo emitida.';
        ELSE
            SET @Mensaje = 'No se pudo anular la OCI.';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO
