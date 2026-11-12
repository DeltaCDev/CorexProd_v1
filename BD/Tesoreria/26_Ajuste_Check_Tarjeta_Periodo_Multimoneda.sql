/*
    ETAPA 14.4.2.12 — CHECK de períodos de tarjeta compatible con multimoneda

    Sustituye únicamente CK_TesTarCredPer_MontoEstado. Las columnas legacy
    Monto/TotalPagado/SaldoPendiente se conservan por compatibilidad, pero no
    determinan la validez financiera de un período multimoneda.

    Este script es idempotente y debe ejecutarse en CorexProdDB.
*/
USE CorexProdDB;
GO

IF OBJECT_ID(N'dbo.TesTarjetaCreditoPeriodos', N'U') IS NULL
    THROW 50010, 'No existe dbo.TesTarjetaCreditoPeriodos.', 1;
GO

/*
    Reglas por moneda:
      - importe NULL: la moneda no participa; total = 0 y saldo = NULL.
      - importe > 0: total entre 0 e importe y saldo = importe - total.
      - EstadoPEN/EstadoUSD admite ANULADA cuando el período está ANULADA;
        para períodos operables se exige PENDIENTE/PARCIAL/CANCELADA.
    ABIERTO sin importes exige ambas monedas no configuradas.
*/
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_TesTarCredPer_MontoEstado'
          AND parent_object_id = OBJECT_ID(N'dbo.TesTarjetaCreditoPeriodos')
    )
    BEGIN
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos
            DROP CONSTRAINT CK_TesTarCredPer_MontoEstado;
    END;

    ALTER TABLE dbo.TesTarjetaCreditoPeriodos
        ADD CONSTRAINT CK_TesTarCredPer_MontoEstado CHECK
    (
        (
            Estado = 'ABIERTO'
            AND ImportePEN IS NULL
            AND ImporteUSD IS NULL
            AND TotalPagadoPEN = 0
            AND TotalPagadoUSD = 0
            AND SaldoPendientePEN IS NULL
            AND SaldoPendienteUSD IS NULL
        )
        OR
        (
            Estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA')
            AND
            (
                (
                    ImportePEN IS NULL
                    AND TotalPagadoPEN = 0
                    AND SaldoPendientePEN IS NULL
                    AND (EstadoPEN IS NULL OR Estado = 'ANULADA')
                )
                OR
                (
                    ImportePEN > 0
                    AND TotalPagadoPEN >= 0
                    AND TotalPagadoPEN <= ImportePEN
                    AND SaldoPendientePEN >= 0
                    AND SaldoPendientePEN = ImportePEN - TotalPagadoPEN
                    AND EstadoPEN IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA')
                )
            )
            AND
            (
                (
                    ImporteUSD IS NULL
                    AND TotalPagadoUSD = 0
                    AND SaldoPendienteUSD IS NULL
                    AND (EstadoUSD IS NULL OR Estado = 'ANULADA')
                )
                OR
                (
                    ImporteUSD > 0
                    AND TotalPagadoUSD >= 0
                    AND TotalPagadoUSD <= ImporteUSD
                    AND SaldoPendienteUSD >= 0
                    AND SaldoPendienteUSD = ImporteUSD - TotalPagadoUSD
                    AND EstadoUSD IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA')
                )
            )
        )
        );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
