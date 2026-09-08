USE CorexProdDB;
GO

/*
    ETAPA 9.1 - Edicion controlada de Prestamos / Obligaciones Financieras.
    Alcance: SQL Server. No modifica CxP, C#, WPF, API, menus ni permisos.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_TesObFinCuotas_Obligacion_Numero'
      AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraCuotas')
      AND filter_definition IS NULL
)
BEGIN
    DROP INDEX UX_TesObFinCuotas_Obligacion_Numero ON dbo.TesObligacionFinancieraCuotas;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_TesObFinCuotas_Obligacion_Numero'
      AND object_id = OBJECT_ID('dbo.TesObligacionFinancieraCuotas')
)
BEGIN
    CREATE UNIQUE INDEX UX_TesObFinCuotas_Obligacion_Numero
    ON dbo.TesObligacionFinancieraCuotas(IdObligacionFinanciera, NumeroCuota)
    WHERE Estado <> 'ANULADA';
END;
GO

IF TYPE_ID('dbo.TesObligacionFinancieraCuotaEdicionType') IS NULL
BEGIN
    CREATE TYPE dbo.TesObligacionFinancieraCuotaEdicionType AS TABLE
    (
        IdCuotaObligacionFinanciera INT NULL,
        NumeroCuota INT NOT NULL,
        TotalCuotas INT NOT NULL,
        FechaVencimiento DATE NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_OBFIN_ACTUALIZAR_PRESTAMO
    @IdObligacionFinanciera INT,
    @IdEntidadFinanciera INT,
    @TipoTitular VARCHAR(20),
    @IdTitular INT = NULL,
    @NombreTitular VARCHAR(180),
    @IdEmpresaResponsable INT = NULL,
    @CodigoReferencia VARCHAR(80) = '',
    @NumeroContrato VARCHAR(80) = '',
    @NumeroOperacion VARCHAR(80) = '',
    @Concepto VARCHAR(250),
    @Moneda VARCHAR(10),
    @MontoOriginal DECIMAL(18,2) = NULL,
    @FechaInicio DATE,
    @TotalCuotas INT,
    @Observacion VARCHAR(1000) = '',
    @Usuario VARCHAR(80),
    @Cuotas dbo.TesObligacionFinancieraCuotaEdicionType READONLY,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Resultado = 0;
    SET @Mensaje = '';
    SET @TipoTitular = UPPER(LTRIM(RTRIM(ISNULL(@TipoTitular, ''))));
    SET @NombreTitular = LTRIM(RTRIM(ISNULL(@NombreTitular, '')));
    SET @CodigoReferencia = LTRIM(RTRIM(ISNULL(@CodigoReferencia, '')));
    SET @NumeroContrato = LTRIM(RTRIM(ISNULL(@NumeroContrato, '')));
    SET @NumeroOperacion = LTRIM(RTRIM(ISNULL(@NumeroOperacion, '')));
    SET @Concepto = LTRIM(RTRIM(ISNULL(@Concepto, '')));
    SET @Moneda = UPPER(LTRIM(RTRIM(ISNULL(@Moneda, 'PEN'))));
    SET @Observacion = LTRIM(RTRIM(ISNULL(@Observacion, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, 'Sistema')));

    IF @Usuario = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el usuario que edita el prestamo.';
        RETURN;
    END;

    IF @IdObligacionFinanciera <= 0
    BEGIN
        SET @Mensaje = 'Debe indicar una obligacion financiera valida.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.TesEntidadesFinancieras WHERE IdEntidadFinanciera = @IdEntidadFinanciera AND Activo = 1)
    BEGIN
        SET @Mensaje = 'La entidad financiera no existe o esta inactiva.';
        RETURN;
    END;

    IF @TipoTitular NOT IN ('EMPRESA', 'PERSONA', 'OTRO')
    BEGIN
        SET @Mensaje = 'El tipo de titular debe ser EMPRESA, PERSONA u OTRO.';
        RETURN;
    END;

    IF @TipoTitular = 'EMPRESA' AND @IdTitular IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdTitular AND Estado = 1)
    BEGIN
        SET @Mensaje = 'La empresa titular no existe o esta inactiva.';
        RETURN;
    END;

    IF @IdEmpresaResponsable IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.Empresas WHERE IdEmpresa = @IdEmpresaResponsable AND Estado = 1)
    BEGIN
        SET @Mensaje = 'La empresa responsable no existe o esta inactiva.';
        RETURN;
    END;

    IF @NombreTitular = '' AND @TipoTitular = 'EMPRESA' AND @IdTitular IS NOT NULL
    BEGIN
        SELECT @NombreTitular = Nombre
        FROM dbo.Empresas
        WHERE IdEmpresa = @IdTitular;
    END;

    IF @NombreTitular = ''
    BEGIN
        SET @Mensaje = 'Debe indicar el titular de la obligacion.';
        RETURN;
    END;

    IF @Concepto = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el concepto del prestamo.';
        RETURN;
    END;

    IF @Moneda NOT IN ('PEN', 'USD')
    BEGIN
        SET @Mensaje = 'La moneda debe ser PEN o USD.';
        RETURN;
    END;

    IF @MontoOriginal IS NOT NULL AND @MontoOriginal <= 0
    BEGIN
        SET @Mensaje = 'El monto original debe ser mayor a cero.';
        RETURN;
    END;

    IF @FechaInicio IS NULL
    BEGIN
        SET @Mensaje = 'Debe ingresar la fecha de inicio.';
        RETURN;
    END;

    IF @TotalCuotas <= 0
    BEGIN
        SET @Mensaje = 'El total de cuotas debe ser mayor a cero.';
        RETURN;
    END;

    DECLARE @EditaCronograma BIT = CASE WHEN EXISTS (SELECT 1 FROM @Cuotas) THEN 1 ELSE 0 END;

    IF @EditaCronograma = 1
    BEGIN
        IF (SELECT COUNT(1) FROM @Cuotas) <> @TotalCuotas
        BEGIN
            SET @Mensaje = 'La cantidad de cuotas del cronograma debe coincidir con el total de cuotas.';
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM @Cuotas
            WHERE NumeroCuota <= 0
               OR TotalCuotas <= 0
               OR NumeroCuota > TotalCuotas
               OR TotalCuotas <> @TotalCuotas
        )
        BEGIN
            SET @Mensaje = 'La numeracion del cronograma no es valida.';
            RETURN;
        END;

        IF EXISTS
        (
            SELECT NumeroCuota
            FROM @Cuotas
            GROUP BY NumeroCuota
            HAVING COUNT(1) > 1
        )
        BEGIN
            SET @Mensaje = 'No se puede duplicar el numero de cuota dentro del prestamo.';
            RETURN;
        END;

        IF EXISTS (SELECT 1 FROM @Cuotas WHERE Importe <= 0)
        BEGIN
            SET @Mensaje = 'Los importes de las cuotas deben ser mayores a cero.';
            RETURN;
        END;

        IF EXISTS (SELECT 1 FROM @Cuotas WHERE FechaVencimiento < @FechaInicio)
        BEGIN
            SET @Mensaje = 'La fecha de vencimiento no puede ser anterior a la fecha de inicio.';
            RETURN;
        END;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @EstadoAnterior VARCHAR(30),
            @MonedaAnterior VARCHAR(10),
            @MontoOriginalAnterior DECIMAL(18,2),
            @FechaInicioAnterior DATE,
            @TotalCuotasAnterior INT,
            @IdTipoObligacionAnterior INT,
            @CodigoTipo VARCHAR(40),
            @ExisteHistorialFinanciero BIT = 0,
            @TotalCronograma DECIMAL(18,2) = 0,
            @TotalPagadoObligacion DECIMAL(18,2) = 0,
            @SaldoObligacion DECIMAL(18,2) = 0,
            @EstadoNuevo VARCHAR(30);

        SELECT
            @EstadoAnterior = O.Estado,
            @MonedaAnterior = O.Moneda,
            @MontoOriginalAnterior = O.MontoOriginal,
            @FechaInicioAnterior = O.FechaInicio,
            @TotalCuotasAnterior = O.TotalCuotas,
            @IdTipoObligacionAnterior = O.IdTipoObligacionFinanciera,
            @CodigoTipo = T.Codigo
        FROM dbo.TesObligacionesFinancieras O WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.TesTiposObligacionFinanciera T ON T.IdTipoObligacionFinanciera = O.IdTipoObligacionFinanciera
        WHERE O.IdObligacionFinanciera = @IdObligacionFinanciera;

        IF @EstadoAnterior IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La obligacion financiera no existe.';
            RETURN;
        END;

        IF @CodigoTipo <> 'PRESTAMO'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'Solo se permite editar obligaciones financieras de tipo PRESTAMO con este procedimiento.';
            RETURN;
        END;

        IF @EstadoAnterior = 'ANULADA'
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede editar una obligacion financiera anulada.';
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.TesObligacionFinancieraPagos P
            INNER JOIN dbo.TesObligacionFinancieraCuotas Q ON Q.IdCuotaObligacionFinanciera = P.IdCuotaObligacionFinanciera
            WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
        )
        BEGIN
            SET @ExisteHistorialFinanciero = 1;
        END;

        IF @ExisteHistorialFinanciero = 1 AND @Moneda <> @MonedaAnterior
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede cambiar la moneda porque el prestamo ya tiene pagos registrados o anulados.';
            RETURN;
        END;

        IF @EstadoAnterior = 'CANCELADA'
        BEGIN
            IF @EditaCronograma = 1
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede modificar el cronograma de una obligacion cancelada.';
                RETURN;
            END;

            IF @Moneda <> @MonedaAnterior
               OR ISNULL(@MontoOriginal, -1) <> ISNULL(@MontoOriginalAnterior, -1)
               OR @FechaInicio <> @FechaInicioAnterior
               OR @TotalCuotas <> @TotalCuotasAnterior
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'Una obligacion cancelada solo permite editar datos administrativos no financieros.';
                RETURN;
            END;
        END;

        IF @ExisteHistorialFinanciero = 1
           AND @EditaCronograma = 1
           AND @TotalCuotas <> @TotalCuotasAnterior
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'No se puede cambiar el total de cuotas porque el prestamo ya tiene historial financiero.';
            RETURN;
        END;

        IF @EditaCronograma = 0
           AND EXISTS
           (
                SELECT 1
                FROM dbo.TesObligacionFinancieraCuotas
                WHERE IdObligacionFinanciera = @IdObligacionFinanciera
                  AND Estado <> 'ANULADA'
                  AND FechaVencimiento < @FechaInicio
           )
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La fecha de inicio no puede ser posterior a vencimientos del cronograma actual.';
            RETURN;
        END;

        IF @EditaCronograma = 1
        BEGIN
            IF EXISTS
            (
                SELECT 1
                FROM @Cuotas C
                WHERE C.IdCuotaObligacionFinanciera IS NOT NULL
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.TesObligacionFinancieraCuotas Q
                      WHERE Q.IdCuotaObligacionFinanciera = C.IdCuotaObligacionFinanciera
                        AND Q.IdObligacionFinanciera = @IdObligacionFinanciera
                        AND Q.Estado <> 'ANULADA'
                  )
            )
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'Una de las cuotas enviadas no pertenece al prestamo o se encuentra anulada.';
                RETURN;
            END;

            IF @ExisteHistorialFinanciero = 1
            BEGIN
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.TesObligacionFinancieraCuotas Q
                    WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
                      AND Q.Estado <> 'ANULADA'
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM @Cuotas C
                          WHERE C.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
                      )
                )
                BEGIN
                    ROLLBACK TRANSACTION;
                    SET @Mensaje = 'No se puede eliminar cuotas porque el prestamo ya tiene historial financiero.';
                    RETURN;
                END;

                IF EXISTS (SELECT 1 FROM @Cuotas WHERE IdCuotaObligacionFinanciera IS NULL)
                BEGIN
                    ROLLBACK TRANSACTION;
                    SET @Mensaje = 'No se puede agregar cuotas porque el prestamo ya tiene historial financiero.';
                    RETURN;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.TesObligacionFinancieraCuotas Q
                    INNER JOIN @Cuotas C ON C.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
                    WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
                      AND (Q.NumeroCuota <> C.NumeroCuota OR Q.Importe <> C.Importe)
                )
                BEGIN
                    ROLLBACK TRANSACTION;
                    SET @Mensaje = 'No se puede cambiar numero ni importe de cuotas porque el prestamo ya tiene historial financiero.';
                    RETURN;
                END;
            END;

            IF EXISTS
            (
                SELECT 1
                FROM dbo.TesObligacionFinancieraCuotas Q
                INNER JOIN dbo.TesObligacionFinancieraPagos P ON P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
                WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM @Cuotas C
                      WHERE C.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
                  )
            )
            BEGIN
                ROLLBACK TRANSACTION;
                SET @Mensaje = 'No se puede eliminar una cuota con historial financiero.';
                RETURN;
            END;
        END;

        UPDATE dbo.TesObligacionesFinancieras
        SET IdEntidadFinanciera = @IdEntidadFinanciera,
            TipoTitular = @TipoTitular,
            IdTitular = @IdTitular,
            NombreTitular = @NombreTitular,
            IdEmpresaResponsable = @IdEmpresaResponsable,
            CodigoReferencia = @CodigoReferencia,
            NumeroContrato = @NumeroContrato,
            NumeroOperacion = @NumeroOperacion,
            Concepto = @Concepto,
            Moneda = @Moneda,
            MontoOriginal = @MontoOriginal,
            FechaInicio = @FechaInicio,
            TotalCuotas = CASE WHEN @EditaCronograma = 1 THEN @TotalCuotas ELSE TotalCuotas END,
            Observacion = @Observacion,
            UsuarioModificacion = @Usuario,
            FechaModificacion = GETDATE()
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera;

        INSERT INTO dbo.TesObligacionFinancieraHistorial
        (
            IdObligacionFinanciera,
            Usuario,
            Accion,
            EstadoAnterior,
            EstadoNuevo,
            Descripcion
        )
        VALUES
        (
            @IdObligacionFinanciera,
            @Usuario,
            'EDICION',
            @EstadoAnterior,
            @EstadoAnterior,
            'Datos de cabecera del prestamo actualizados.'
        );

        IF @EditaCronograma = 1
        BEGIN
            UPDATE Q
            SET Estado = 'ANULADA',
                SaldoPendiente = Q.Importe - Q.TotalPagado
            FROM dbo.TesObligacionFinancieraCuotas Q
            WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
              AND Q.Estado <> 'ANULADA'
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM @Cuotas C
                  WHERE C.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
              );

            UPDATE Q
            SET NumeroCuota = C.NumeroCuota,
                TotalCuotas = C.TotalCuotas,
                FechaVencimiento = C.FechaVencimiento,
                Importe = C.Importe,
                SaldoPendiente = C.Importe - Q.TotalPagado,
                Observacion = LTRIM(RTRIM(ISNULL(C.Observacion, '')))
            FROM dbo.TesObligacionFinancieraCuotas Q
            INNER JOIN @Cuotas C ON C.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
            WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera;

            INSERT INTO dbo.TesObligacionFinancieraCuotas
            (
                IdObligacionFinanciera,
                NumeroCuota,
                TotalCuotas,
                FechaVencimiento,
                Importe,
                TotalPagado,
                SaldoPendiente,
                Estado,
                Observacion
            )
            SELECT
                @IdObligacionFinanciera,
                C.NumeroCuota,
                C.TotalCuotas,
                C.FechaVencimiento,
                C.Importe,
                0,
                C.Importe,
                'PENDIENTE',
                LTRIM(RTRIM(ISNULL(C.Observacion, '')))
            FROM @Cuotas C
            WHERE C.IdCuotaObligacionFinanciera IS NULL;

            UPDATE Q
            SET TotalPagado = ISNULL(PG.TotalPagado, 0),
                SaldoPendiente = Q.Importe - ISNULL(PG.TotalPagado, 0),
                Estado = CASE
                    WHEN ISNULL(PG.TotalPagado, 0) <= 0 THEN 'PENDIENTE'
                    WHEN Q.Importe - ISNULL(PG.TotalPagado, 0) <= 0 THEN 'CANCELADA'
                    ELSE 'PARCIAL'
                END
            FROM dbo.TesObligacionFinancieraCuotas Q
            OUTER APPLY
            (
                SELECT SUM(P.Monto) AS TotalPagado
                FROM dbo.TesObligacionFinancieraPagos P
                WHERE P.IdCuotaObligacionFinanciera = Q.IdCuotaObligacionFinanciera
                  AND P.Estado = 'ACTIVO'
            ) PG
            WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
              AND Q.Estado <> 'ANULADA';

            INSERT INTO dbo.TesObligacionFinancieraHistorial
            (
                IdObligacionFinanciera,
                Usuario,
                Accion,
                EstadoAnterior,
                EstadoNuevo,
                Descripcion
            )
            VALUES
            (
                @IdObligacionFinanciera,
                @Usuario,
                'EDICION_CRONOGRAMA',
                @EstadoAnterior,
                @EstadoAnterior,
                CONCAT('Cronograma actualizado. Total de cuotas: ', @TotalCuotas, '.')
            );
        END;

        SELECT
            @TotalCronograma = ISNULL(SUM(Q.Importe), 0),
            @TotalPagadoObligacion = ISNULL(SUM(Q.TotalPagado), 0),
            @SaldoObligacion = ISNULL(SUM(Q.SaldoPendiente), 0)
        FROM dbo.TesObligacionFinancieraCuotas Q
        WHERE Q.IdObligacionFinanciera = @IdObligacionFinanciera
          AND Q.Estado <> 'ANULADA';

        SET @EstadoNuevo = CASE
            WHEN @EstadoAnterior = 'CANCELADA' THEN 'CANCELADA'
            WHEN @SaldoObligacion <= 0 AND @TotalCronograma > 0 THEN 'CANCELADA'
            WHEN @TotalPagadoObligacion > 0 THEN 'PARCIAL'
            ELSE 'PENDIENTE'
        END;

        UPDATE dbo.TesObligacionesFinancieras
        SET Estado = @EstadoNuevo
        WHERE IdObligacionFinanciera = @IdObligacionFinanciera;

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Prestamo actualizado correctamente.';

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdObligacionFinanciera AS IdObligacionFinanciera,
            @TotalCronograma AS TotalCronograma,
            @TotalPagadoObligacion AS TotalPagado,
            @SaldoObligacion AS SaldoPendiente,
            @EstadoNuevo AS EstadoObligacionFinanciera;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();

        SELECT
            @Resultado AS Resultado,
            @Mensaje AS Mensaje,
            @IdObligacionFinanciera AS IdObligacionFinanciera,
            CAST(0 AS DECIMAL(18,2)) AS TotalCronograma,
            CAST(0 AS DECIMAL(18,2)) AS TotalPagado,
            CAST(0 AS DECIMAL(18,2)) AS SaldoPendiente,
            '' AS EstadoObligacionFinanciera;
    END CATCH;
END;
GO
