USE CorexProdDB;
GO

/*
    Procedimiento aislado de prueba para validar el contrato multimoneda.
    No reemplaza ni modifica dbo.USP_TES_TARJETA_GUARDAR.
*/
CREATE OR ALTER PROCEDURE dbo.USP_TES_TARJETA_GUARDAR_MULTIMONEDA_TEST
    @IdTarjetaCredito INT OUTPUT,
    @IdEntidadFinanciera INT,
    @Titular VARCHAR(180),
    @IdEmpresaResponsable INT = NULL,
    @EmpresaResponsable VARCHAR(180),
    @Alias VARCHAR(120),
    @Ultimos4 VARCHAR(4) = NULL,
    @Monedas dbo.TesTarjetaCreditoMonedaType READONLY,
    @DiaCorte TINYINT = NULL,
    @DiaPago TINYINT = NULL,
    @Estado VARCHAR(20) = 'ACTIVA',
    @Observacion VARCHAR(1000) = '',
    @Usuario VARCHAR(80),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @IdTarjetaCredito = ISNULL(@IdTarjetaCredito, 0);
    SET @Titular = LTRIM(RTRIM(ISNULL(@Titular, '')));
    SET @EmpresaResponsable = LTRIM(RTRIM(ISNULL(@EmpresaResponsable, '')));
    SET @Alias = LTRIM(RTRIM(ISNULL(@Alias, '')));
    SET @Ultimos4 = NULLIF(LTRIM(RTRIM(ISNULL(@Ultimos4, ''))), '');
    SET @Estado = UPPER(LTRIM(RTRIM(ISNULL(@Estado, 'ACTIVA'))));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    DECLARE @MonedasNormalizadas TABLE
    (
        Moneda VARCHAR(10) NOT NULL PRIMARY KEY
    );
    DECLARE @MonedaLegacy VARCHAR(10);
    DECLARE @EstadoAnterior VARCHAR(20);
    DECLARE @MonedaAnterior VARCHAR(10);
    DECLARE @MonedasAnterior VARCHAR(100);
    DECLARE @MonedasNuevas VARCHAR(100);
    DECLARE @MonedaRetirada VARCHAR(10);

    IF @Usuario = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el usuario que registra la tarjeta.';
        RETURN;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.TesEntidadesFinancieras
        WHERE IdEntidadFinanciera = @IdEntidadFinanciera
          AND Activo = 1
    )
    BEGIN
        SET @Mensaje = 'La entidad financiera no existe o esta inactiva.';
        RETURN;
    END;

    IF @Titular = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el titular de la tarjeta.';
        RETURN;
    END;

    IF @IdEmpresaResponsable IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.Empresas
           WHERE IdEmpresa = @IdEmpresaResponsable
             AND Estado = 1
       )
    BEGIN
        SET @Mensaje = 'La empresa responsable no existe o esta inactiva.';
        RETURN;
    END;

    IF @IdEmpresaResponsable IS NOT NULL AND @EmpresaResponsable = ''
    BEGIN
        SELECT @EmpresaResponsable = Nombre
        FROM dbo.Empresas
        WHERE IdEmpresa = @IdEmpresaResponsable;
    END;

    IF @EmpresaResponsable = ''
    BEGIN
        SET @Mensaje = 'Debe indicar la empresa responsable del pago.';
        RETURN;
    END;

    IF @Alias = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el alias de la tarjeta.';
        RETURN;
    END;

    IF @Ultimos4 IS NOT NULL AND (LEN(@Ultimos4) <> 4 OR @Ultimos4 LIKE '%[^0-9]%')
    BEGIN
        SET @Mensaje = 'Ultimos4 debe contener exactamente cuatro digitos.';
        RETURN;
    END;

    IF @DiaCorte IS NOT NULL AND (@DiaCorte < 1 OR @DiaCorte > 31)
    BEGIN
        SET @Mensaje = 'El dia de corte debe estar entre 1 y 31.';
        RETURN;
    END;

    IF @DiaPago IS NOT NULL AND (@DiaPago < 1 OR @DiaPago > 31)
    BEGIN
        SET @Mensaje = 'El dia de pago debe estar entre 1 y 31.';
        RETURN;
    END;

    IF @Estado NOT IN ('ACTIVA', 'INACTIVA')
    BEGIN
        SET @Mensaje = 'El estado administrativo debe ser ACTIVA o INACTIVA.';
        RETURN;
    END;

    IF (SELECT COUNT(*) FROM @Monedas) NOT BETWEEN 1 AND 2
    BEGIN
        SET @Mensaje = 'Debe indicar una o dos monedas habilitadas.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM @Monedas WHERE NULLIF(LTRIM(RTRIM(Moneda)), '') IS NULL)
    BEGIN
        SET @Mensaje = 'Las monedas habilitadas no pueden estar vacias.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Monedas
        WHERE UPPER(LTRIM(RTRIM(Moneda))) NOT IN ('PEN', 'USD')
    )
    BEGIN
        SET @Mensaje = 'Las monedas habilitadas solo pueden ser PEN o USD.';
        RETURN;
    END;

    IF EXISTS
    (
        SELECT UPPER(LTRIM(RTRIM(Moneda)))
        FROM @Monedas
        GROUP BY UPPER(LTRIM(RTRIM(Moneda)))
        HAVING COUNT(*) > 1
    )
    BEGIN
        SET @Mensaje = 'No se permiten monedas habilitadas duplicadas.';
        RETURN;
    END;

    INSERT INTO @MonedasNormalizadas (Moneda)
    SELECT UPPER(LTRIM(RTRIM(Moneda)))
    FROM @Monedas;

    SET @MonedaLegacy = CASE WHEN EXISTS (SELECT 1 FROM @MonedasNormalizadas WHERE Moneda = 'PEN') THEN 'PEN' ELSE 'USD' END;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF ISNULL(@IdTarjetaCredito, 0) = 0
        BEGIN
            INSERT INTO dbo.TesTarjetasCredito
            (
                IdEntidadFinanciera, Titular, IdEmpresaResponsable, EmpresaResponsable,
                Alias, Ultimos4, Moneda, DiaCorte, DiaPago, Estado, Activo, Observacion,
                UsuarioCreacion
            )
            VALUES
            (
                @IdEntidadFinanciera, @Titular, @IdEmpresaResponsable, @EmpresaResponsable,
                @Alias, @Ultimos4, @MonedaLegacy, @DiaCorte, @DiaPago, @Estado,
                CASE WHEN @Estado = 'ACTIVA' THEN 1 ELSE 0 END, @Observacion, @Usuario
            );

            SET @IdTarjetaCredito = SCOPE_IDENTITY();

            INSERT INTO dbo.TesTarjetaCreditoMonedas (IdTarjetaCredito, Moneda)
            SELECT @IdTarjetaCredito, Moneda
            FROM @MonedasNormalizadas;

            INSERT INTO dbo.TesTarjetaCreditoHistorial
            (
                IdTarjetaCredito, Usuario, Accion, EstadoNuevo, Descripcion
            )
            VALUES
            (
                @IdTarjetaCredito, @Usuario, 'CREACION_TARJETA', @Estado, 'Tarjeta de credito creada.'
            );

            SET @Mensaje = 'Tarjeta de credito registrada correctamente.';
        END
        ELSE
        BEGIN
            SELECT
                @EstadoAnterior = Estado,
                @MonedaAnterior = Moneda
            FROM dbo.TesTarjetasCredito WITH (UPDLOCK, HOLDLOCK)
            WHERE IdTarjetaCredito = @IdTarjetaCredito;

            IF @EstadoAnterior IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'La tarjeta de credito no existe.';
                RETURN;
            END;

            IF @EstadoAnterior = 'ANULADA'
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede editar una tarjeta anulada.';
                RETURN;
            END;

            SELECT @MonedasAnterior = STUFF
            (
                (
                    SELECT ',' + M.Moneda
                    FROM dbo.TesTarjetaCreditoMonedas AS M
                    WHERE M.IdTarjetaCredito = @IdTarjetaCredito
                    ORDER BY M.Moneda
                    FOR XML PATH(''), TYPE
                ).value('.', 'VARCHAR(MAX)'),
                1, 1, ''
            );

            SELECT @MonedasNuevas = STUFF
            (
                (
                    SELECT ',' + M.Moneda
                    FROM @MonedasNormalizadas AS M
                    ORDER BY M.Moneda
                    FOR XML PATH(''), TYPE
                ).value('.', 'VARCHAR(MAX)'),
                1, 1, ''
            );

            SELECT TOP (1) @MonedaRetirada = Actual.Moneda
            FROM dbo.TesTarjetaCreditoMonedas AS Actual
            WHERE Actual.IdTarjetaCredito = @IdTarjetaCredito
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM @MonedasNormalizadas AS Nueva
                  WHERE Nueva.Moneda = Actual.Moneda
              )
              AND EXISTS
              (
                  SELECT 1
                  FROM dbo.TesTarjetaCreditoPeriodos AS Periodo
                  WHERE Periodo.IdTarjetaCredito = @IdTarjetaCredito
                    AND Periodo.Moneda = Actual.Moneda
              );

            IF @MonedaRetirada IS NOT NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = CONCAT('No se puede retirar la moneda ', @MonedaRetirada, ' porque tiene periodos asociados.');
                RETURN;
            END;

            DELETE Actual
            FROM dbo.TesTarjetaCreditoMonedas AS Actual
            WHERE Actual.IdTarjetaCredito = @IdTarjetaCredito
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM @MonedasNormalizadas AS Nueva
                  WHERE Nueva.Moneda = Actual.Moneda
              );

            INSERT INTO dbo.TesTarjetaCreditoMonedas (IdTarjetaCredito, Moneda)
            SELECT @IdTarjetaCredito, Nueva.Moneda
            FROM @MonedasNormalizadas AS Nueva
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM dbo.TesTarjetaCreditoMonedas AS Actual
                WHERE Actual.IdTarjetaCredito = @IdTarjetaCredito
                  AND Actual.Moneda = Nueva.Moneda
            );

            UPDATE dbo.TesTarjetasCredito
            SET IdEntidadFinanciera = @IdEntidadFinanciera,
                Titular = @Titular,
                IdEmpresaResponsable = @IdEmpresaResponsable,
                EmpresaResponsable = @EmpresaResponsable,
                Alias = @Alias,
                Ultimos4 = @Ultimos4,
                Moneda = @MonedaLegacy,
                DiaCorte = @DiaCorte,
                DiaPago = @DiaPago,
                Estado = @Estado,
                Activo = CASE WHEN @Estado = 'ACTIVA' THEN 1 ELSE 0 END,
                Observacion = @Observacion,
                FechaModificacion = GETDATE(),
                UsuarioModificacion = @Usuario
            WHERE IdTarjetaCredito = @IdTarjetaCredito;

            INSERT INTO dbo.TesTarjetaCreditoHistorial
            (
                IdTarjetaCredito, Usuario, Accion, EstadoAnterior, EstadoNuevo, Descripcion
            )
            VALUES
            (
                @IdTarjetaCredito, @Usuario, 'EDICION_TARJETA', @EstadoAnterior, @Estado,
                CONCAT('Datos administrativos de tarjeta actualizados. Monedas: ', ISNULL(@MonedasAnterior, ''), ' -> ', ISNULL(@MonedasNuevas, ''), '.')
            );

            SET @Mensaje = 'Tarjeta de credito guardada correctamente.';
        END;

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Tarjeta de credito guardada correctamente.';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;

    SELECT @Resultado AS Resultado, @Mensaje AS Mensaje, @IdTarjetaCredito AS IdTarjetaCredito;
END;
GO
