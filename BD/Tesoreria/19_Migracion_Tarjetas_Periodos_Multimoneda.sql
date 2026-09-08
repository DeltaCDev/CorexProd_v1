USE CorexProdDB;
GO

/*
 ETAPA 14.4.2.2 - Migracion controlada de periodos de tarjetas multimoneda.
 No elimina columnas, tablas, procedimientos ni consolida filas de periodos.
 Las columnas legacy se preservan para la siguiente etapa de procedimientos y C#.
*/

-- Auditoria previa (solo lectura).
SELECT
    (SELECT COUNT(*) FROM dbo.TesTarjetasCredito) AS Tarjetas,
    (SELECT COUNT(*) FROM dbo.TesTarjetaCreditoPeriodos) AS Periodos,
    (SELECT COUNT(*) FROM dbo.TesTarjetaCreditoPagos) AS Pagos,
    (SELECT COUNT(*) FROM dbo.TesTarjetaCreditoMonedas) AS MonedasHabilitadas;

SELECT IdTarjetaCredito, PeriodoAnio, PeriodoMes, COUNT(*) AS Cantidad
FROM dbo.TesTarjetaCreditoPeriodos
GROUP BY IdTarjetaCredito, PeriodoAnio, PeriodoMes
HAVING COUNT(*) > 1;

SELECT IdTarjetaCredito, PeriodoAnio, PeriodoMes, Moneda, COUNT(*) AS Cantidad
FROM dbo.TesTarjetaCreditoPeriodos
GROUP BY IdTarjetaCredito, PeriodoAnio, PeriodoMes, Moneda;

SELECT P.IdTarjetaPeriodo, P.IdTarjetaCredito, P.PeriodoAnio, P.PeriodoMes,
       P.Moneda, P.Monto, P.Estado, COUNT(PG.IdTarjetaPago) AS CantidadPagos
FROM dbo.TesTarjetaCreditoPeriodos AS P
LEFT JOIN dbo.TesTarjetaCreditoPagos AS PG ON PG.IdTarjetaPeriodo = P.IdTarjetaPeriodo
GROUP BY P.IdTarjetaPeriodo, P.IdTarjetaCredito, P.PeriodoAnio, P.PeriodoMes, P.Moneda, P.Monto, P.Estado
ORDER BY P.IdTarjetaCredito, P.PeriodoAnio, P.PeriodoMes, P.IdTarjetaPeriodo;

SELECT PG.IdTarjetaPeriodo, P.Moneda AS MonedaPeriodo, COUNT(*) AS Cantidad
FROM dbo.TesTarjetaCreditoPagos AS PG
INNER JOIN dbo.TesTarjetaCreditoPeriodos AS P ON P.IdTarjetaPeriodo = PG.IdTarjetaPeriodo
GROUP BY PG.IdTarjetaPeriodo, P.Moneda;

SELECT T.IdTarjetaCredito, T.Alias, T.Ultimos4, E.Nombre AS EntidadFinanciera,
       T.Moneda AS MonedaLegacy,
       STUFF((SELECT ',' + M.Moneda FROM dbo.TesTarjetaCreditoMonedas AS M
              WHERE M.IdTarjetaCredito = T.IdTarjetaCredito ORDER BY M.Moneda
              FOR XML PATH(''), TYPE).value('.', 'VARCHAR(MAX)'), 1, 1, '') AS MonedasHabilitadas
FROM dbo.TesTarjetasCredito AS T
INNER JOIN dbo.TesEntidadesFinancieras AS E ON E.IdEntidadFinanciera = T.IdEntidadFinanciera
ORDER BY T.IdTarjetaCredito;
GO

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos', 'ImportePEN') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD ImportePEN DECIMAL(18,2) NULL;
    IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos', 'ImporteUSD') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD ImporteUSD DECIMAL(18,2) NULL;
    IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos', 'TotalPagadoPEN') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD TotalPagadoPEN DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesTarCredPer_TotalPagadoPEN DEFAULT (0);
    IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos', 'TotalPagadoUSD') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD TotalPagadoUSD DECIMAL(18,2) NOT NULL CONSTRAINT DF_TesTarCredPer_TotalPagadoUSD DEFAULT (0);
    IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos', 'SaldoPendientePEN') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD SaldoPendientePEN DECIMAL(18,2) NULL;
    IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos', 'SaldoPendienteUSD') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD SaldoPendienteUSD DECIMAL(18,2) NULL;
    IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos', 'EstadoPEN') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD EstadoPEN VARCHAR(30) NULL;
    IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos', 'EstadoUSD') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD EstadoUSD VARCHAR(30) NULL;

    IF COL_LENGTH('dbo.TesTarjetaCreditoPagos', 'Moneda') IS NULL
        ALTER TABLE dbo.TesTarjetaCreditoPagos ADD Moneda VARCHAR(10) NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- No se reemplazan valores ya migrados; se completa desde la columna legacy.
    UPDATE P
    SET ImportePEN = CASE WHEN P.Moneda = 'PEN' THEN P.Monto ELSE P.ImportePEN END,
        ImporteUSD = CASE WHEN P.Moneda = 'USD' THEN P.Monto ELSE P.ImporteUSD END
    FROM dbo.TesTarjetaCreditoPeriodos AS P;

    -- Todo pago existente recibe la moneda inequívoca de su periodo origen.
    UPDATE PG
    SET Moneda = P.Moneda
    FROM dbo.TesTarjetaCreditoPagos AS PG
    INNER JOIN dbo.TesTarjetaCreditoPeriodos AS P ON P.IdTarjetaPeriodo = PG.IdTarjetaPeriodo
    WHERE PG.Moneda IS NULL;

    IF EXISTS (SELECT 1 FROM dbo.TesTarjetaCreditoPagos WHERE Moneda IS NULL)
        THROW 51001, 'Existen pagos cuya moneda no puede determinarse desde su periodo legacy.', 1;

    IF EXISTS (SELECT 1 FROM dbo.TesTarjetaCreditoPagos WHERE Moneda NOT IN ('PEN', 'USD'))
        THROW 51002, 'Existen pagos con moneda distinta de PEN o USD.', 1;

    IF EXISTS (SELECT 1 FROM dbo.TesTarjetaCreditoMonedas WHERE Moneda NOT IN ('PEN', 'USD'))
        THROW 51003, 'Existen monedas habilitadas de tarjeta distintas de PEN o USD.', 1;

    ;WITH PagosPorMoneda AS
    (
        SELECT IdTarjetaPeriodo,
               SUM(CASE WHEN Estado = 'ACTIVO' AND Moneda = 'PEN' THEN Importe ELSE 0 END) AS TotalPEN,
               SUM(CASE WHEN Estado = 'ACTIVO' AND Moneda = 'USD' THEN Importe ELSE 0 END) AS TotalUSD
        FROM dbo.TesTarjetaCreditoPagos
        GROUP BY IdTarjetaPeriodo
    )
    UPDATE P
    SET TotalPagadoPEN = ISNULL(X.TotalPEN, 0),
        TotalPagadoUSD = ISNULL(X.TotalUSD, 0),
        SaldoPendientePEN = CASE WHEN P.ImportePEN IS NULL THEN NULL ELSE P.ImportePEN - ISNULL(X.TotalPEN, 0) END,
        SaldoPendienteUSD = CASE WHEN P.ImporteUSD IS NULL THEN NULL ELSE P.ImporteUSD - ISNULL(X.TotalUSD, 0) END
    FROM dbo.TesTarjetaCreditoPeriodos AS P
    LEFT JOIN PagosPorMoneda AS X ON X.IdTarjetaPeriodo = P.IdTarjetaPeriodo;

    UPDATE P
    SET EstadoPEN = CASE
            WHEN P.ImportePEN IS NULL THEN NULL
            WHEN P.Estado = 'ANULADA' THEN 'ANULADA'
            WHEN P.SaldoPendientePEN <= 0 THEN 'CANCELADA'
            WHEN P.TotalPagadoPEN > 0 THEN 'PARCIAL'
            ELSE 'PENDIENTE'
        END,
        EstadoUSD = CASE
            WHEN P.ImporteUSD IS NULL THEN NULL
            WHEN P.Estado = 'ANULADA' THEN 'ANULADA'
            WHEN P.SaldoPendienteUSD <= 0 THEN 'CANCELADA'
            WHEN P.TotalPagadoUSD > 0 THEN 'PARCIAL'
            ELSE 'PENDIENTE'
        END
    FROM dbo.TesTarjetaCreditoPeriodos AS P;

    UPDATE P
    SET Estado = CASE
            WHEN P.Estado = 'ANULADA' THEN 'ANULADA'
            WHEN P.ImportePEN IS NULL AND P.ImporteUSD IS NULL THEN 'ABIERTO'
            WHEN P.EstadoPEN = 'PENDIENTE' OR P.EstadoUSD = 'PENDIENTE' THEN 'PENDIENTE'
            WHEN P.EstadoPEN = 'PARCIAL' OR P.EstadoUSD = 'PARCIAL' THEN 'PARCIAL'
            WHEN P.EstadoPEN = 'CANCELADA' OR P.EstadoUSD = 'CANCELADA' THEN 'CANCELADA'
            ELSE P.Estado
        END
    FROM dbo.TesTarjetaCreditoPeriodos AS P;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.TesTarjetaCreditoPeriodos')
          AND name = 'UX_TesTarCredPer_Tarjeta_Periodo_Destino'
    )
    AND NOT EXISTS
    (
        SELECT 1 FROM dbo.TesTarjetaCreditoPeriodos
        GROUP BY IdTarjetaCredito, PeriodoAnio, PeriodoMes
        HAVING COUNT(*) > 1
    )
        CREATE UNIQUE INDEX UX_TesTarCredPer_Tarjeta_Periodo_Destino
            ON dbo.TesTarjetaCreditoPeriodos(IdTarjetaCredito, PeriodoAnio, PeriodoMes);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Validaciones finales (solo lectura).
SELECT
    (SELECT COUNT(*) FROM dbo.TesTarjetasCredito) AS Tarjetas,
    (SELECT COUNT(*) FROM dbo.TesTarjetaCreditoPeriodos) AS Periodos,
    (SELECT COUNT(*) FROM dbo.TesTarjetaCreditoPagos) AS Pagos,
    (SELECT COUNT(*) FROM dbo.TesTarjetaCreditoMonedas) AS MonedasHabilitadas;

SELECT * FROM dbo.TesTarjetaCreditoPagos WHERE Moneda IS NULL;
SELECT * FROM dbo.TesTarjetaCreditoPagos WHERE Moneda NOT IN ('PEN', 'USD');
SELECT * FROM dbo.TesTarjetaCreditoPeriodos WHERE ImportePEN IS NULL AND ImporteUSD IS NULL;

SELECT IdTarjetaCredito, PeriodoAnio, PeriodoMes, COUNT(*) AS Cantidad
FROM dbo.TesTarjetaCreditoPeriodos
GROUP BY IdTarjetaCredito, PeriodoAnio, PeriodoMes
HAVING COUNT(*) > 1;

SELECT IdTarjetaPeriodo, Moneda, Monto, TotalPagado, SaldoPendiente,
       ImportePEN, ImporteUSD, TotalPagadoPEN, TotalPagadoUSD,
       SaldoPendientePEN, SaldoPendienteUSD, EstadoPEN, EstadoUSD, Estado
FROM dbo.TesTarjetaCreditoPeriodos
ORDER BY IdTarjetaPeriodo;
GO
