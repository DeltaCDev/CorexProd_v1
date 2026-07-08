SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/*
    Correccion OT Manual - Abastecimiento de Stock
    ------------------------------------------------
    Las OT manuales de abastecimiento ya no deben registrarse con IdCliente = 0.
    Se asigna por defecto el cliente interno de la empresa Delta Confecciones SRLTDA.
*/

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE is_table_type = 1 AND name = 'TipoOTManualPlanificacion')
    EXEC('CREATE TYPE dbo.TipoOTManualPlanificacion AS TABLE(IdProducto INT PRIMARY KEY, CantidadPlanificada DECIMAL(18,2) NOT NULL)');
GO

DECLARE @IdClienteDelta INT;
DECLARE @NombreClienteDelta NVARCHAR(250);

SELECT TOP (1)
    @IdClienteDelta = IdCliente,
    @NombreClienteDelta = NombreRazonSocial
FROM dbo.Clientes
WHERE NumeroDocumento = '20373078078'
  AND Estado = 1
ORDER BY IdCliente;

IF @IdClienteDelta IS NOT NULL
BEGIN
    UPDATE dbo.OrdenTrabajo
    SET IdCliente = @IdClienteDelta,
        NombreCliente = @NombreClienteDelta
    WHERE TipoOT = 'MANUAL'
      AND IdCliente = 0
      AND NombreCliente = 'ABASTECIMIENTO DE STOCK';
END
GO

CREATE OR ALTER PROCEDURE dbo.USP_PRO_OT_MANUAL_CREAR
    @IdUsuario INT,
    @Observacion NVARCHAR(500),
    @Detalles dbo.TipoOTManualPlanificacion READONLY,
    @IdOrdenTrabajo INT OUTPUT,
    @NumeroOT VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE IdUsuario = @IdUsuario AND Estado = 1)
            THROW 51000, 'El usuario de sesion no es valido.', 1;
        IF NOT EXISTS(SELECT 1 FROM @Detalles)
            THROW 51000, 'Seleccione al menos un producto.', 1;
        IF EXISTS(SELECT 1 FROM @Detalles WHERE CantidadPlanificada <= 0)
            THROW 51000, 'Todas las cantidades deben ser mayores que cero.', 1;
        IF EXISTS(SELECT IdProducto FROM @Detalles GROUP BY IdProducto HAVING COUNT(*) > 1)
            THROW 51000, 'No se puede repetir el producto en una OT manual.', 1;
        IF EXISTS(SELECT 1 FROM @Detalles D LEFT JOIN dbo.Productos P ON P.IdProducto = D.IdProducto WHERE P.IdProducto IS NULL OR P.Estado = 0)
            THROW 51000, 'La planificacion contiene productos inexistentes o inactivos.', 1;
        IF NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsInicio = 1)
            OR NOT EXISTS(SELECT 1 FROM dbo.AreaProduccion WHERE Activo = 1 AND EsTermino = 1)
            THROW 51000, 'Configure las areas activas de inicio y termino.', 1;

        DECLARE @IdClienteAbastecimiento INT;
        DECLARE @NombreClienteAbastecimiento NVARCHAR(250);

        SELECT TOP (1)
            @IdClienteAbastecimiento = IdCliente,
            @NombreClienteAbastecimiento = NombreRazonSocial
        FROM dbo.Clientes
        WHERE NumeroDocumento = '20373078078'
          AND Estado = 1
        ORDER BY IdCliente;

        IF @IdClienteAbastecimiento IS NULL
            THROW 51000, 'No se encontro el cliente interno DELTA CONFECCIONES SRLTDA para generar la OT manual de abastecimiento.', 1;

        DECLARE @Correlativo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(NumeroOT, 6))) FROM dbo.OrdenTrabajo WITH(UPDLOCK, HOLDLOCK)), 0) + 1;
        SET @NumeroOT = CONCAT('OT-', RIGHT(CONCAT('000000', @Correlativo), 6));

        INSERT dbo.OrdenTrabajo
        (
            NumeroOT, IdOrdenCompraInterna, IdCliente, NombreCliente, IdUsuarioCreacion,
            Observacion, Estado, TipoOT, IdOrdenTrabajoRelacionada
        )
        VALUES
        (
            @NumeroOT, NULL, @IdClienteAbastecimiento, @NombreClienteAbastecimiento, @IdUsuario,
            ISNULL(@Observacion, N''), 'PENDIENTE', 'MANUAL', NULL
        );

        SET @IdOrdenTrabajo = CONVERT(INT, SCOPE_IDENTITY());

        INSERT dbo.OrdenTrabajoDetalle
        (
            IdOrdenTrabajo, IdOrdenCompraInternaDetalle, IdProducto, CodigoProducto, NombreProducto,
            CantidadRequerida, CantidadPlanificada, CantidadPendiente
        )
        SELECT
            @IdOrdenTrabajo, NULL, P.IdProducto, P.Codigo, P.NombreProducto,
            D.CantidadPlanificada, D.CantidadPlanificada, D.CantidadPlanificada
        FROM @Detalles D
        JOIN dbo.Productos P ON P.IdProducto = D.IdProducto;

        INSERT dbo.OrdenTrabajoDetalleArea
        (
            IdOrdenTrabajo, IdDetalleOT, IdAreaProduccion, CodigoArea, NombreArea,
            OrdenSecuencia, EsInicio, EsTermino, ManejaMerma, ModoEnvio
        )
        SELECT @IdOrdenTrabajo, D.IdDetalleOT, A.IdAreaProduccion, A.CodigoArea, A.NombreArea,
               A.OrdenSecuencia, A.EsInicio, A.EsTermino, A.ManejaMerma, A.ModoEnvio
        FROM dbo.OrdenTrabajoDetalle D
        CROSS JOIN dbo.AreaProduccion A
        WHERE D.IdOrdenTrabajo = @IdOrdenTrabajo
          AND A.Activo = 1;

        UPDATE A
        SET CantidadRecibida = D.CantidadPlanificada,
            Estado = 'PENDIENTE'
        FROM dbo.OrdenTrabajoDetalleArea A
        JOIN dbo.OrdenTrabajoDetalle D ON D.IdDetalleOT = A.IdDetalleOT
        WHERE A.IdOrdenTrabajo = @IdOrdenTrabajo
          AND A.EsInicio = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
