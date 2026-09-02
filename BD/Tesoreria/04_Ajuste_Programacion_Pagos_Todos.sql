USE CorexProdDB;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_CXP_PROGRAMACION_RANGO
    @FechaDesde DATE,
    @FechaHasta DATE,
    @IdProveedor INT = NULL,
    @Estado VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');

    SELECT
        Q.IdCuota,
        Q.IdCuentaPorPagar,
        C.IdProveedor,
        P.NombreRazonSocial AS NombreProveedor,
        P.NumeroDocumento AS NumeroDocumentoProveedor,
        C.IdTipoObligacion,
        T.Nombre AS TipoObligacion,
        C.Moneda,
        C.FechaDocumento,
        Q.NumeroCuota,
        Q.TotalCuotas,
        Q.NumeroLetra,
        Q.FechaGiro,
        Q.FechaVencimiento,
        Q.Importe,
        ISNULL(PG.TotalPagado, 0) AS TotalPagado,
        Q.Importe - ISNULL(PG.TotalPagado, 0) AS SaldoPendiente,
        Q.Estado,
        C.OrigenTipo,
        C.OrigenId,
        C.Observacion
    FROM dbo.TesCuentaPorPagarCuotas Q
    INNER JOIN dbo.TesCuentasPorPagar C ON C.IdCuentaPorPagar = Q.IdCuentaPorPagar
    INNER JOIN dbo.Proveedores P ON P.IdProveedor = C.IdProveedor
    INNER JOIN dbo.TesTiposObligacion T ON T.IdTipoObligacion = C.IdTipoObligacion
    OUTER APPLY
    (
        SELECT SUM(PA.Importe) AS TotalPagado
        FROM dbo.TesCuentaPorPagarPagos PA
        WHERE PA.IdCuota = Q.IdCuota
          AND PA.Estado = 'ACTIVO'
    ) PG
    WHERE Q.FechaVencimiento >= @FechaDesde
      AND Q.FechaVencimiento <= @FechaHasta
      AND C.Estado <> 'ANULADA'
      AND Q.Estado <> 'ANULADA'
      AND (@IdProveedor IS NULL OR C.IdProveedor = @IdProveedor)
      AND (
            (@Estado IS NULL AND Q.Estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA'))
            OR (@Estado IS NOT NULL AND Q.Estado = @Estado)
          )
    ORDER BY Q.FechaVencimiento, P.NombreRazonSocial, Q.NumeroCuota;
END;
GO
