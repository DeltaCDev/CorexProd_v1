/*
    ETAPA 12 - Programacion General de Tesoreria
    Consolida Cuentas por Pagar y Prestamos por fecha de vencimiento.
    No crea tablas de programacion: la informacion se deriva de cuotas reales.
*/

CREATE OR ALTER PROCEDURE dbo.USP_TES_PROGRAMACION_GENERAL_RANGO
    @FechaInicio DATE,
    @FechaFin DATE,
    @IncluirPendientesAnteriores BIT = 0,
    @Estado VARCHAR(30) = NULL,
    @Categoria VARCHAR(40) = NULL,
    @Moneda VARCHAR(10) = NULL,
    @Texto VARCHAR(160) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');
    SET @Categoria = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Categoria, '')))), '');
    SET @Moneda = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Moneda, '')))), '');
    SET @Texto = NULLIF(LTRIM(RTRIM(ISNULL(@Texto, ''))), '');

    IF @FechaInicio IS NULL OR @FechaFin IS NULL OR @FechaInicio > @FechaFin
    BEGIN
        RAISERROR('El rango de fechas no es valido.', 16, 1);
        RETURN;
    END;

    ;WITH CxpBase AS
    (
        SELECT
            Q.IdCuota AS OrigenDetalleId,
            C.IdCuentaPorPagar AS OrigenId,
            Q.FechaVencimiento,
            CASE
                WHEN T.Codigo = 'FACTURA_CREDITO' OR Q.TipoCuota = 'CUOTA_FACTURA' THEN 'FACTURA_CREDITO'
                WHEN T.Codigo = 'LETRA' OR Q.TipoCuota = 'LETRA' THEN 'LETRA'
            END AS Categoria,
            'CXP' AS OrigenTipo,
            CASE
                WHEN T.Codigo = 'FACTURA_CREDITO' OR Q.TipoCuota = 'CUOTA_FACTURA' THEN 'Factura credito'
                WHEN T.Codigo = 'LETRA' OR Q.TipoCuota = 'LETRA' THEN 'Letra por pagar'
            END AS Subcategoria,
            P.NombreRazonSocial AS EntidadProveedor,
            CAST('' AS VARCHAR(180)) AS Titular,
            CAST('' AS VARCHAR(180)) AS EmpresaResponsable,
            C.Observacion AS Concepto,
            CASE
                WHEN T.Codigo = 'FACTURA_CREDITO' OR Q.TipoCuota = 'CUOTA_FACTURA'
                    THEN ISNULL(DP.NumeroDocumento, CONCAT('Factura cuota ', Q.NumeroCuota, '/', Q.TotalCuotas))
                ELSE ISNULL(NULLIF(Q.NumeroLetra, ''), CONCAT('Letra cuota ', Q.NumeroCuota, '/', Q.TotalCuotas))
            END AS Referencia,
            C.FechaDocumento,
            Q.NumeroCuota,
            Q.TotalCuotas,
            C.Moneda,
            Q.Importe,
            ISNULL(PG.TotalPagado, 0) AS Pagado,
            Q.Importe - ISNULL(PG.TotalPagado, 0) AS Saldo,
            CASE WHEN Q.Estado = 'PAGADA' THEN 'CANCELADA' ELSE Q.Estado END AS Estado,
            C.Estado AS EstadoOrigen
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
        OUTER APPLY
        (
            SELECT TOP (1) D.NumeroDocumento
            FROM dbo.TesCuentaPorPagarDocumentos D
            WHERE D.IdCuentaPorPagar = C.IdCuentaPorPagar
              AND D.Estado = 'ACTIVO'
              AND D.FactorEfecto = 1
            ORDER BY D.FechaDocumento, D.IdCuentaPorPagarDocumento
        ) DP
        WHERE C.Estado <> 'ANULADA'
          AND Q.Estado <> 'ANULADA'
          AND (
                T.Codigo IN ('LETRA', 'FACTURA_CREDITO')
                OR Q.TipoCuota IN ('LETRA', 'CUOTA_FACTURA')
              )
    ),
    PrestamosBase AS
    (
        SELECT
            Q.IdCuotaObligacionFinanciera AS OrigenDetalleId,
            O.IdObligacionFinanciera AS OrigenId,
            Q.FechaVencimiento,
            CAST('PRESTAMO' AS VARCHAR(40)) AS Categoria,
            CAST('PRESTAMO' AS VARCHAR(40)) AS OrigenTipo,
            CAST('Prestamo' AS VARCHAR(80)) AS Subcategoria,
            E.Nombre AS EntidadProveedor,
            O.NombreTitular AS Titular,
            ISNULL(ER.Nombre, '') AS EmpresaResponsable,
            O.Concepto,
            CASE
                WHEN NULLIF(O.CodigoReferencia, '') IS NOT NULL THEN O.CodigoReferencia
                WHEN NULLIF(O.NumeroOperacion, '') IS NOT NULL THEN O.NumeroOperacion
                WHEN NULLIF(O.NumeroContrato, '') IS NOT NULL THEN O.NumeroContrato
                ELSE CONCAT('Prestamo #', O.IdObligacionFinanciera)
            END AS Referencia,
            CAST(NULL AS DATE) AS FechaDocumento,
            Q.NumeroCuota,
            Q.TotalCuotas,
            O.Moneda,
            Q.Importe,
            Q.TotalPagado AS Pagado,
            Q.SaldoPendiente AS Saldo,
            Q.Estado,
            O.Estado AS EstadoOrigen
        FROM dbo.TesObligacionFinancieraCuotas Q
        INNER JOIN dbo.TesObligacionesFinancieras O ON O.IdObligacionFinanciera = Q.IdObligacionFinanciera
        INNER JOIN dbo.TesTiposObligacionFinanciera T ON T.IdTipoObligacionFinanciera = O.IdTipoObligacionFinanciera
        INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = O.IdEntidadFinanciera
        LEFT JOIN dbo.Empresas ER ON ER.IdEmpresa = O.IdEmpresaResponsable
        WHERE O.Estado <> 'ANULADA'
          AND Q.Estado <> 'ANULADA'
          AND T.Codigo = 'PRESTAMO'
    ),
    UnionProgramacion AS
    (
        SELECT * FROM CxpBase
        UNION ALL
        SELECT * FROM PrestamosBase
    )
    SELECT
        FechaVencimiento,
        Categoria,
        Subcategoria,
        EntidadProveedor,
        Titular,
        EmpresaResponsable,
        Concepto,
        Referencia,
        FechaDocumento,
        NumeroCuota,
        TotalCuotas,
        Moneda,
        Importe,
        Pagado,
        Saldo,
        Estado,
        OrigenTipo,
        OrigenId,
        OrigenDetalleId,
        CASE WHEN FechaVencimiento < @FechaInicio THEN 1 ELSE 0 END AS EsPendienteAnterior
    FROM UnionProgramacion
    WHERE
        (
            FechaVencimiento BETWEEN @FechaInicio AND @FechaFin
            OR
            (
                @IncluirPendientesAnteriores = 1
                AND FechaVencimiento < @FechaInicio
                AND Saldo > 0
                AND Estado IN ('PENDIENTE', 'PARCIAL')
            )
        )
      AND (
            (@Estado IS NULL AND Estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA'))
            OR (@Estado IS NOT NULL AND Estado = @Estado)
          )
      AND (@Categoria IS NULL OR Categoria = @Categoria)
      AND (@Moneda IS NULL OR Moneda = @Moneda)
      AND (
            @Texto IS NULL
            OR EntidadProveedor LIKE '%' + @Texto + '%'
            OR Titular LIKE '%' + @Texto + '%'
            OR EmpresaResponsable LIKE '%' + @Texto + '%'
            OR Concepto LIKE '%' + @Texto + '%'
            OR Referencia LIKE '%' + @Texto + '%'
          )
    ORDER BY FechaVencimiento, Categoria, EntidadProveedor, NumeroCuota, OrigenId, OrigenDetalleId;
END;
GO
