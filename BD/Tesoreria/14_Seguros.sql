USE CorexProdDB;
GO

/*
    ETAPA 15.1 - Seguros.
    Reutiliza obligaciones financieras, periodos/cuotas, pagos e historial.
    No modifica CxP, tarjetas ni Programacion General.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

MERGE dbo.TesTiposObligacionFinanciera AS T
USING (VALUES
    ('SEGURO', 'Seguro', 'Obligaciones financieras originadas por polizas de seguro')
) AS S(Codigo, Nombre, Descripcion)
ON T.Codigo = S.Codigo
WHEN MATCHED THEN
    UPDATE SET Nombre = S.Nombre, Descripcion = S.Descripcion, Activo = 1
WHEN NOT MATCHED THEN
    INSERT (Codigo, Nombre, Descripcion, Activo)
    VALUES (S.Codigo, S.Nombre, S.Descripcion, 1);
GO

IF COL_LENGTH('dbo.TesObligacionFinancieraCuotas', 'EsCuota') IS NULL
BEGIN
    ALTER TABLE dbo.TesObligacionFinancieraCuotas
        ADD EsCuota BIT NOT NULL
            CONSTRAINT DF_TesObFinCuotas_EsCuota DEFAULT(1) WITH VALUES;
END;
GO

IF OBJECT_ID('dbo.TesSeguros', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TesSeguros
    (
        IdSeguro INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TesSeguros PRIMARY KEY,
        IdObligacionFinanciera INT NOT NULL,
        TipoSeguro VARCHAR(80) NOT NULL,
        NumeroPoliza VARCHAR(100) NOT NULL,
        FechaInicioCobertura DATE NOT NULL,
        FechaFinCobertura DATE NOT NULL,
        Periodicidad VARCHAR(20) NOT NULL,
        CONSTRAINT FK_TesSeguros_Obligacion FOREIGN KEY (IdObligacionFinanciera)
            REFERENCES dbo.TesObligacionesFinancieras(IdObligacionFinanciera),
        CONSTRAINT CK_TesSeguros_Periodicidad CHECK (Periodicidad IN ('UNICO', 'MENSUAL', 'BIMESTRAL', 'TRIMESTRAL', 'SEMESTRAL', 'ANUAL')),
        CONSTRAINT CK_TesSeguros_FechasCobertura CHECK (FechaFinCobertura >= FechaInicioCobertura)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TesSeguros_Obligacion'
      AND object_id = OBJECT_ID('dbo.TesSeguros')
)
    CREATE UNIQUE INDEX UX_TesSeguros_Obligacion ON dbo.TesSeguros(IdObligacionFinanciera);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TesSeguros_Poliza'
      AND object_id = OBJECT_ID('dbo.TesSeguros')
)
    CREATE INDEX IX_TesSeguros_Poliza ON dbo.TesSeguros(NumeroPoliza);
GO

IF TYPE_ID('dbo.TesSeguroPeriodoType') IS NULL
BEGIN
    CREATE TYPE dbo.TesSeguroPeriodoType AS TABLE
    (
        NumeroCuota INT NOT NULL,
        TotalCuotas INT NOT NULL,
        EsCuota BIT NOT NULL,
        FechaVencimiento DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_SEGURO_GUARDAR
    @IdEntidadAseguradora INT,
    @TipoTitular VARCHAR(20),
    @IdTitular INT = NULL,
    @NombreTitular VARCHAR(180),
    @IdEmpresaResponsable INT = NULL,
    @CodigoReferencia VARCHAR(80) = '',
    @NumeroContrato VARCHAR(80) = '',
    @Concepto VARCHAR(250),
    @Moneda VARCHAR(10),
    @MontoOriginal DECIMAL(18,2),
    @FechaInicio DATE,
    @Observacion VARCHAR(1000) = '',
    @TipoSeguro VARCHAR(80),
    @NumeroPoliza VARCHAR(100),
    @FechaInicioCobertura DATE,
    @FechaFinCobertura DATE,
    @Periodicidad VARCHAR(20),
    @Usuario VARCHAR(80),
    @Periodos dbo.TesSeguroPeriodoType READONLY,
    @IdObligacionFinanciera INT OUTPUT,
    @IdSeguro INT OUTPUT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @IdObligacionFinanciera = 0;
    SET @IdSeguro = 0;
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @TipoTitular = UPPER(LTRIM(RTRIM(ISNULL(@TipoTitular, ''))));
    SET @NombreTitular = LTRIM(RTRIM(ISNULL(@NombreTitular, '')));
    SET @CodigoReferencia = LTRIM(RTRIM(ISNULL(@CodigoReferencia, '')));
    SET @NumeroContrato = LTRIM(RTRIM(ISNULL(@NumeroContrato, '')));
    SET @Concepto = LTRIM(RTRIM(ISNULL(@Concepto, '')));
    SET @Moneda = UPPER(LTRIM(RTRIM(ISNULL(@Moneda, ''))));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @TipoSeguro = LTRIM(RTRIM(ISNULL(@TipoSeguro, '')));
    SET @NumeroPoliza = LTRIM(RTRIM(ISNULL(@NumeroPoliza, '')));
    SET @Periodicidad = UPPER(LTRIM(RTRIM(ISNULL(@Periodicidad, ''))));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    IF @TipoTitular NOT IN ('EMPRESA', 'PERSONA', 'OTRO')
    BEGIN SET @Mensaje = 'El tipo de titular debe ser EMPRESA, PERSONA u OTRO.'; RETURN; END;
    IF @TipoTitular = 'EMPRESA' AND @IdTitular IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdTitular AND Estado = 1)
    BEGIN SET @Mensaje = 'La empresa titular no existe o esta inactiva.'; RETURN; END;
    IF @NombreTitular = '' AND @TipoTitular = 'EMPRESA' AND @IdTitular IS NOT NULL
        SELECT @NombreTitular = Nombre FROM dbo.Empresas WHERE IdEmpresa = @IdTitular;
    IF @NombreTitular = '' BEGIN SET @Mensaje = 'Debe indicar el titular del seguro.'; RETURN; END;
    IF @IdEmpresaResponsable IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresaResponsable AND Estado = 1)
    BEGIN SET @Mensaje = 'La empresa responsable no existe o esta inactiva.'; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM dbo.TesEntidadesFinancieras WHERE IdEntidadFinanciera = @IdEntidadAseguradora AND Activo = 1 AND TipoEntidad = 'ASEGURADORA')
    BEGIN SET @Mensaje = 'La entidad seleccionada debe ser una aseguradora activa.'; RETURN; END;
    IF @TipoSeguro = '' BEGIN SET @Mensaje = 'Debe indicar el tipo de seguro.'; RETURN; END;
    IF @NumeroPoliza = '' BEGIN SET @Mensaje = 'Debe indicar el numero de poliza.'; RETURN; END;
    IF @Concepto = '' BEGIN SET @Mensaje = 'Debe ingresar el concepto del seguro.'; RETURN; END;
    IF @Moneda NOT IN ('PEN', 'USD') BEGIN SET @Mensaje = 'La moneda debe ser PEN o USD.'; RETURN; END;
    IF @MontoOriginal IS NULL OR @MontoOriginal <= 0 BEGIN SET @Mensaje = 'El monto original debe ser mayor a cero.'; RETURN; END;
    IF @FechaInicio IS NULL OR @FechaInicioCobertura IS NULL OR @FechaFinCobertura IS NULL OR @FechaFinCobertura < @FechaInicioCobertura
    BEGIN SET @Mensaje = 'Las fechas de cobertura no son validas.'; RETURN; END;
    IF @Periodicidad NOT IN ('UNICO', 'MENSUAL', 'BIMESTRAL', 'TRIMESTRAL', 'SEMESTRAL', 'ANUAL')
    BEGIN SET @Mensaje = 'La periodicidad del seguro no es valida.'; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM @Periodos) BEGIN SET @Mensaje = 'Debe registrar al menos un periodo de pago.'; RETURN; END;
    IF EXISTS (SELECT 1 FROM @Periodos WHERE Importe <= 0 OR FechaVencimiento < @FechaInicio)
    BEGIN SET @Mensaje = 'Los importes y fechas de vencimiento de los periodos no son validos.'; RETURN; END;
    IF EXISTS (SELECT 1 FROM @Periodos WHERE EsCuota = 0) AND (SELECT COUNT(1) FROM @Periodos) <> 1
    BEGIN SET @Mensaje = 'Un periodo unico no puede coexistir con otros periodos.'; RETURN; END;
    IF EXISTS (SELECT 1 FROM @Periodos WHERE EsCuota = 0 AND (NumeroCuota <> 1 OR TotalCuotas <> 1))
    BEGIN SET @Mensaje = 'El periodo unico debe usar NumeroCuota y TotalCuotas igual a 1.'; RETURN; END;
    IF EXISTS (SELECT 1 FROM @Periodos WHERE EsCuota = 1 AND (NumeroCuota <= 0 OR TotalCuotas <= 1 OR NumeroCuota > TotalCuotas))
    BEGIN SET @Mensaje = 'La numeracion de las cuotas del seguro no es valida.'; RETURN; END;
    IF EXISTS (SELECT 1 FROM @Periodos WHERE EsCuota = 1)
       AND (SELECT COUNT(1) FROM @Periodos) <> (SELECT MAX(TotalCuotas) FROM @Periodos)
    BEGIN SET @Mensaje = 'La cantidad de cuotas no coincide con el cronograma.'; RETURN; END;
    IF EXISTS (SELECT 1 FROM @Periodos WHERE EsCuota = 1 AND TotalCuotas <> (SELECT MAX(TotalCuotas) FROM @Periodos))
    BEGIN SET @Mensaje = 'Todas las cuotas del seguro deben tener el mismo TotalCuotas.'; RETURN; END;
    IF EXISTS (SELECT 1 FROM @Periodos GROUP BY NumeroCuota HAVING COUNT(1) > 1)
    BEGIN SET @Mensaje = 'No se puede duplicar el numero de cuota dentro del seguro.'; RETURN; END;
    IF ROUND((SELECT SUM(Importe) FROM @Periodos), 2) <> ROUND(@MontoOriginal, 2)
    BEGIN SET @Mensaje = 'El monto original debe coincidir con la suma de los periodos.'; RETURN; END;
    IF @Periodicidad = 'UNICO' AND (SELECT COUNT(1) FROM @Periodos) <> 1
    BEGIN SET @Mensaje = 'La periodicidad UNICO requiere exactamente un periodo.'; RETURN; END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @IdTipoSeguro INT;
        SELECT @IdTipoSeguro = IdTipoObligacionFinanciera
        FROM dbo.TesTiposObligacionFinanciera WITH (UPDLOCK, HOLDLOCK)
        WHERE Codigo = 'SEGURO' AND Activo = 1;
        IF @IdTipoSeguro IS NULL
        BEGIN ROLLBACK TRANSACTION; SET @Mensaje = 'No existe el tipo de obligacion financiera SEGURO.'; RETURN; END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.TesSeguros S WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN dbo.TesObligacionesFinancieras O ON O.IdObligacionFinanciera = S.IdObligacionFinanciera
            WHERE O.IdEntidadFinanciera = @IdEntidadAseguradora
              AND S.NumeroPoliza = @NumeroPoliza
              AND O.Estado <> 'ANULADA'
        )
        BEGIN ROLLBACK TRANSACTION; SET @Mensaje = 'Ya existe una poliza activa con el mismo numero para la aseguradora.'; RETURN; END;

        INSERT INTO dbo.TesObligacionesFinancieras
        (
            IdTipoObligacionFinanciera, IdEntidadFinanciera, TipoTitular, IdTitular, NombreTitular,
            IdEmpresaResponsable, CodigoReferencia, NumeroContrato, NumeroOperacion, Concepto, Moneda,
            MontoOriginal, FechaInicio, TotalCuotas, Estado, OrigenTipo, Observacion, UsuarioRegistro
        )
        VALUES
        (
            @IdTipoSeguro, @IdEntidadAseguradora, @TipoTitular, @IdTitular, @NombreTitular,
            @IdEmpresaResponsable, @CodigoReferencia, @NumeroContrato, '', @Concepto, @Moneda,
            @MontoOriginal, @FechaInicio, (SELECT MAX(TotalCuotas) FROM @Periodos), 'PENDIENTE', 'SEGURO', @Observacion, @Usuario
        );
        SET @IdObligacionFinanciera = SCOPE_IDENTITY();

        INSERT INTO dbo.TesSeguros
        (IdObligacionFinanciera, TipoSeguro, NumeroPoliza, FechaInicioCobertura, FechaFinCobertura, Periodicidad)
        VALUES
        (@IdObligacionFinanciera, @TipoSeguro, @NumeroPoliza, @FechaInicioCobertura, @FechaFinCobertura, @Periodicidad);
        SET @IdSeguro = SCOPE_IDENTITY();

        INSERT INTO dbo.TesObligacionFinancieraCuotas
        (IdObligacionFinanciera, NumeroCuota, TotalCuotas, EsCuota, FechaVencimiento, Importe, TotalPagado, SaldoPendiente, Estado, Observacion)
        SELECT @IdObligacionFinanciera, NumeroCuota, TotalCuotas, EsCuota, FechaVencimiento, Importe, 0, Importe, 'PENDIENTE', ISNULL(Observacion, '')
        FROM @Periodos;

        INSERT INTO dbo.TesObligacionFinancieraHistorial
        (IdObligacionFinanciera, Usuario, Accion, EstadoAnterior, EstadoNuevo, Descripcion)
        VALUES
        (@IdObligacionFinanciera, @Usuario, 'CREACION_SEGURO', NULL, 'PENDIENTE', CONCAT('Seguro creado. Poliza: ', @NumeroPoliza, '. Tipo: ', @TipoSeguro, '.'));

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Seguro registrado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_SEGURO_LISTAR
    @Texto VARCHAR(160) = NULL,
    @Estado VARCHAR(30) = NULL,
    @IdEntidadAseguradora INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Texto = NULLIF(LTRIM(RTRIM(ISNULL(@Texto, ''))), '');
    SET @Estado = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@Estado, '')))), '');

    SELECT O.IdObligacionFinanciera, S.IdSeguro, S.TipoSeguro, S.NumeroPoliza, E.Nombre AS Aseguradora,
           O.NombreTitular, ER.Nombre AS EmpresaResponsable, O.Concepto, O.Moneda, O.MontoOriginal,
           O.FechaInicio, S.FechaInicioCobertura, S.FechaFinCobertura, S.Periodicidad, O.TotalCuotas,
           ISNULL(TOT.TotalPagado, 0) AS TotalPagado, ISNULL(TOT.SaldoPendiente, 0) AS SaldoPendiente, O.Estado, O.Observacion
    FROM dbo.TesSeguros S
    INNER JOIN dbo.TesObligacionesFinancieras O ON O.IdObligacionFinanciera = S.IdObligacionFinanciera
    INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = O.IdEntidadFinanciera
    LEFT JOIN dbo.Empresas ER ON ER.IdEmpresa = O.IdEmpresaResponsable
    OUTER APPLY
    (
        SELECT SUM(Q.Importe - ISNULL(PG.TotalPagado, 0)) AS SaldoPendiente, SUM(ISNULL(PG.TotalPagado, 0)) AS TotalPagado
        FROM dbo.TesObligacionFinancieraCuotas Q
        OUTER APPLY (SELECT SUM(P.Monto) AS TotalPagado FROM dbo.TesObligacionFinancieraPagos P WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera AND P.Estado = 'ACTIVO') PG
        WHERE Q.IdObligacionFinanciera = O.IdObligacionFinanciera AND Q.Estado <> 'ANULADA'
    ) TOT
    WHERE (@Estado IS NULL OR @Estado = 'TODOS' OR O.Estado = @Estado)
      AND (@IdEntidadAseguradora IS NULL OR O.IdEntidadFinanciera = @IdEntidadAseguradora)
      AND (@Texto IS NULL OR E.Nombre LIKE '%' + @Texto + '%' OR S.NumeroPoliza LIKE '%' + @Texto + '%' OR S.TipoSeguro LIKE '%' + @Texto + '%' OR O.Concepto LIKE '%' + @Texto + '%')
    ORDER BY S.FechaFinCobertura DESC, S.IdSeguro DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_SEGURO_OBTENER
    @IdObligacionFinanciera INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.TesSeguros S
        INNER JOIN dbo.TesObligacionesFinancieras O ON O.IdObligacionFinanciera = S.IdObligacionFinanciera
        INNER JOIN dbo.TesTiposObligacionFinanciera T ON T.IdTipoObligacionFinanciera = O.IdTipoObligacionFinanciera
        WHERE S.IdObligacionFinanciera = @IdObligacionFinanciera AND T.Codigo = 'SEGURO'
    )
    BEGIN
        RAISERROR('El seguro solicitado no existe.', 16, 1);
        RETURN;
    END;

    SELECT S.IdSeguro, S.IdObligacionFinanciera, S.TipoSeguro, S.NumeroPoliza, S.FechaInicioCobertura, S.FechaFinCobertura, S.Periodicidad,
           O.IdEntidadFinanciera AS IdEntidadAseguradora, E.Nombre AS Aseguradora, O.TipoTitular, O.IdTitular, O.NombreTitular,
           O.IdEmpresaResponsable, ER.Nombre AS EmpresaResponsable, O.CodigoReferencia, O.NumeroContrato, O.Concepto, O.Moneda,
           O.MontoOriginal, O.FechaInicio, O.TotalCuotas, O.Estado, O.Observacion, O.UsuarioRegistro, O.FechaRegistro,
           O.UsuarioAnulacion, O.FechaAnulacion, O.MotivoAnulacion
    FROM dbo.TesSeguros S
    INNER JOIN dbo.TesObligacionesFinancieras O ON O.IdObligacionFinanciera = S.IdObligacionFinanciera
    INNER JOIN dbo.TesEntidadesFinancieras E ON E.IdEntidadFinanciera = O.IdEntidadFinanciera
    LEFT JOIN dbo.Empresas ER ON ER.IdEmpresa = O.IdEmpresaResponsable
    WHERE S.IdObligacionFinanciera = @IdObligacionFinanciera;

    SELECT Q.IdCuotaObligacionFinanciera, Q.IdObligacionFinanciera, Q.NumeroCuota, Q.TotalCuotas, Q.EsCuota, Q.FechaVencimiento,
           Q.Importe, ISNULL(PG.TotalPagado, 0) AS TotalPagado, Q.Importe - ISNULL(PG.TotalPagado, 0) AS SaldoPendiente,
           Q.Estado, Q.Observacion, Q.FechaRegistro
    FROM dbo.TesObligacionFinancieraCuotas Q
    OUTER APPLY (SELECT SUM(P.Monto) AS TotalPagado FROM dbo.TesObligacionFinancieraPagos P WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera AND P.Estado = 'ACTIVO') PG
    WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
    ORDER BY Q.NumeroCuota, Q.IdCuotaObligacionFinanciera;

    SELECT P.IdPagoObligacionFinanciera, P.IdCuotaObligacionFinanciera, Q.IdObligacionFinanciera, Q.NumeroCuota,
           P.FechaPago, P.Monto, P.IdBanco, P.IdCuentaBancaria, P.NumeroOperacion, P.Observacion, P.Estado,
           P.UsuarioRegistro, P.FechaRegistro, P.UsuarioAnulacion, P.FechaAnulacion, P.MotivoAnulacion
    FROM dbo.TesObligacionFinancieraPagos P
    INNER JOIN dbo.TesObligacionFinancieraCuotas Q ON Q.IdCuotaObligacionFinanciera = P.IdCuotaObligacionFinanciera
    WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
    ORDER BY P.FechaPago, P.IdPagoObligacionFinanciera;

    SELECT H.IdHistorialObligacionFinanciera, H.IdObligacionFinanciera, H.IdCuotaObligacionFinanciera, H.IdPagoObligacionFinanciera,
           H.Usuario, H.Accion, H.EstadoAnterior, H.EstadoNuevo, H.Descripcion, H.FechaHora
    FROM dbo.TesObligacionFinancieraHistorial H
    WHERE H.IdObligacionFinanciera = @IdObligacionFinanciera
    ORDER BY H.FechaHora DESC, H.IdHistorialObligacionFinanciera DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_SEGURO_ANULAR
    @IdObligacionFinanciera INT,
    @Usuario VARCHAR(80),
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.TesSeguros S
        INNER JOIN dbo.TesObligacionesFinancieras O ON O.IdObligacionFinanciera = S.IdObligacionFinanciera
        INNER JOIN dbo.TesTiposObligacionFinanciera T ON T.IdTipoObligacionFinanciera = O.IdTipoObligacionFinanciera
        WHERE S.IdObligacionFinanciera = @IdObligacionFinanciera AND T.Codigo = 'SEGURO'
    )
    BEGIN SET @Resultado = 0; SET @Mensaje = 'El seguro solicitado no existe.'; RETURN; END;

    EXEC dbo.USP_TES_OBFIN_ANULAR
        @IdObligacionFinanciera = @IdObligacionFinanciera,
        @Usuario = @Usuario,
        @Motivo = @Motivo,
        @Resultado = @Resultado OUTPUT,
        @Mensaje = @Mensaje OUTPUT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_SEGURO_PERIODO_ANULAR
    @IdCuotaObligacionFinanciera INT,
    @Usuario VARCHAR(80),
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));
    SET @Motivo = LTRIM(RTRIM(ISNULL(@Motivo, '')));
    IF @Motivo = '' BEGIN SET @Mensaje = 'Debe ingresar el motivo de anulacion del periodo.'; RETURN; END;

    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @IdObligacionFinanciera INT, @EstadoPeriodo VARCHAR(30), @EstadoObligacion VARCHAR(30), @ActivosRestantes INT;
        SELECT @IdObligacionFinanciera = Q.IdObligacionFinanciera, @EstadoPeriodo = Q.Estado, @EstadoObligacion = O.Estado
        FROM dbo.TesObligacionFinancieraCuotas Q WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesObligacionesFinancieras O WITH (UPDLOCK, HOLDLOCK) ON O.IdObligacionFinanciera = Q.IdObligacionFinanciera
        INNER JOIN dbo.TesSeguros S ON S.IdObligacionFinanciera = O.IdObligacionFinanciera
        WHERE Q.IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera;
        IF @IdObligacionFinanciera IS NULL BEGIN ROLLBACK TRANSACTION; SET @Mensaje = 'El periodo de seguro no existe.'; RETURN; END;
        IF @EstadoObligacion = 'ANULADA' BEGIN ROLLBACK TRANSACTION; SET @Mensaje = 'No se puede anular un periodo de un seguro anulado.'; RETURN; END;
        IF @EstadoPeriodo = 'ANULADA' BEGIN ROLLBACK TRANSACTION; SET @Mensaje = 'El periodo ya se encuentra anulado.'; RETURN; END;
        IF EXISTS (SELECT 1 FROM dbo.TesObligacionFinancieraPagos WHERE IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera AND Estado = 'ACTIVO')
        BEGIN ROLLBACK TRANSACTION; SET @Mensaje = 'No se puede anular un periodo con pagos activos.'; RETURN; END;
        SELECT @ActivosRestantes = COUNT(1) FROM dbo.TesObligacionFinancieraCuotas WHERE IdObligacionFinanciera = @IdObligacionFinanciera AND Estado <> 'ANULADA';
        IF @ActivosRestantes <= 1 BEGIN ROLLBACK TRANSACTION; SET @Mensaje = 'No se puede anular el ultimo periodo. Anule el seguro completo.'; RETURN; END;

        UPDATE dbo.TesObligacionFinancieraCuotas
        SET Estado = 'ANULADA', SaldoPendiente = Importe - TotalPagado
        WHERE IdCuotaObligacionFinanciera = @IdCuotaObligacionFinanciera;

        DECLARE @ConPago INT, @ConSaldo INT, @EstadoNuevo VARCHAR(30);
        SELECT @ConPago = SUM(CASE WHEN TotalPagado > 0 THEN 1 ELSE 0 END), @ConSaldo = SUM(CASE WHEN SaldoPendiente > 0 THEN 1 ELSE 0 END)
        FROM dbo.TesObligacionFinancieraCuotas
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera AND Estado <> 'ANULADA';
        SET @EstadoNuevo = CASE WHEN @ConSaldo = 0 THEN 'CANCELADA' WHEN @ConPago > 0 THEN 'PARCIAL' ELSE 'PENDIENTE' END;
        UPDATE dbo.TesObligacionesFinancieras SET Estado = @EstadoNuevo, UsuarioModificacion = @Usuario, FechaModificacion = GETDATE()
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera;
        INSERT INTO dbo.TesObligacionFinancieraHistorial
        (IdObligacionFinanciera, IdCuotaObligacionFinanciera, Usuario, Accion, EstadoAnterior, EstadoNuevo, Descripcion)
        VALUES (@IdObligacionFinanciera, @IdCuotaObligacionFinanciera, @Usuario, 'ANULACION_PERIODO', @EstadoPeriodo, 'ANULADA', @Motivo);
        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Periodo de seguro anulado correctamente.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO
