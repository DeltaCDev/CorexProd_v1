USE CorexProdDB;
GO
/* ETAPA 14.4.1. Fuente nueva: TesTarjetaCreditoMonedas y Periodos.Moneda.
   TesTarjetasCredito.Moneda se conserva transitoriamente para compatibilidad. */
SET XACT_ABORT ON;
IF OBJECT_ID('dbo.TesTarjetaCreditoMonedas','U') IS NULL
CREATE TABLE dbo.TesTarjetaCreditoMonedas
(
 IdTarjetaCredito INT NOT NULL, Moneda VARCHAR(10) NOT NULL,
 CONSTRAINT PK_TesTarjetaCreditoMonedas PRIMARY KEY(IdTarjetaCredito,Moneda),
 CONSTRAINT FK_TesTarjetaCreditoMonedas_Tarjeta FOREIGN KEY(IdTarjetaCredito) REFERENCES dbo.TesTarjetasCredito(IdTarjetaCredito),
 CONSTRAINT CK_TesTarjetaCreditoMonedas_Moneda CHECK(Moneda IN('PEN','USD'))
);
INSERT dbo.TesTarjetaCreditoMonedas(IdTarjetaCredito,Moneda)
SELECT T.IdTarjetaCredito,T.Moneda FROM dbo.TesTarjetasCredito T
WHERE T.Moneda IN('PEN','USD') AND NOT EXISTS(SELECT 1 FROM dbo.TesTarjetaCreditoMonedas M WHERE M.IdTarjetaCredito=T.IdTarjetaCredito AND M.Moneda=T.Moneda);
IF COL_LENGTH('dbo.TesTarjetaCreditoPeriodos','Moneda') IS NULL ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD Moneda VARCHAR(10) NULL;
GO
BEGIN TRANSACTION;
UPDATE P SET Moneda=T.Moneda FROM dbo.TesTarjetaCreditoPeriodos P INNER JOIN dbo.TesTarjetasCredito T ON T.IdTarjetaCredito=P.IdTarjetaCredito WHERE P.Moneda IS NULL;
IF EXISTS(SELECT 1 FROM dbo.TesTarjetaCreditoPeriodos WHERE Moneda IS NULL OR Moneda NOT IN('PEN','USD')) THROW 50001,'Migracion detenida: existen periodos sin moneda valida.',1;
ALTER TABLE dbo.TesTarjetaCreditoPeriodos ALTER COLUMN Moneda VARCHAR(10) NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_TesTarCredPer_Moneda') ALTER TABLE dbo.TesTarjetaCreditoPeriodos ADD CONSTRAINT CK_TesTarCredPer_Moneda CHECK(Moneda IN('PEN','USD'));
IF EXISTS(SELECT 1 FROM dbo.TesTarjetaCreditoPeriodos GROUP BY IdTarjetaCredito,PeriodoAnio,PeriodoMes,Moneda HAVING COUNT(*)>1) THROW 50002,'Migracion detenida: hay duplicados Tarjeta/Año/Mes/Moneda.',1;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_TesTarCredPer_Tarjeta_Periodo' AND object_id=OBJECT_ID('dbo.TesTarjetaCreditoPeriodos')) DROP INDEX UX_TesTarCredPer_Tarjeta_Periodo ON dbo.TesTarjetaCreditoPeriodos;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_TesTarCredPer_Tarjeta_Periodo_Moneda' AND object_id=OBJECT_ID('dbo.TesTarjetaCreditoPeriodos')) CREATE UNIQUE INDEX UX_TesTarCredPer_Tarjeta_Periodo_Moneda ON dbo.TesTarjetaCreditoPeriodos(IdTarjetaCredito,PeriodoAnio,PeriodoMes,Moneda);
COMMIT;
GO
/* Los procedimientos de tarjeta se actualizan en 14.4.2 junto con sus contratos C#.
   No se cambia su firma aquí para no romper el cliente actualmente desplegado. */
