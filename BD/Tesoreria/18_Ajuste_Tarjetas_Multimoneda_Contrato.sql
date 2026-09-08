USE CorexProdDB;
GO
/* ETAPA 14.4.2.1-A: contrato TVP para las monedas habilitadas de UNA tarjeta.
   SQL Server no permite CREATE TYPE OR ALTER; se crea sólo si aún no existe. */
IF TYPE_ID(N'dbo.TesTarjetaCreditoMonedaType') IS NULL
    EXEC(N'CREATE TYPE dbo.TesTarjetaCreditoMonedaType AS TABLE
    (
        Moneda VARCHAR(10) NOT NULL PRIMARY KEY
    );');
GO
/* La restricción PEN/USD se valida en el procedimiento consumidor, porque los TVP
   no admiten CHECK. El PK del TVP impide monedas duplicadas. */
