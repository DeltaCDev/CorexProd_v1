SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @IdProduccion INT;
    DECLARE @IdOrdenTrabajo INT;

    SELECT TOP (1) @IdProduccion = IdMenu
    FROM dbo.Menu
    WHERE NombreMenu IN (N'Producción', N'Produccion')
      AND IdMenuPadre IS NULL
    ORDER BY IdMenu;

    SELECT TOP (1) @IdOrdenTrabajo = IdMenu
    FROM dbo.Menu
    WHERE NombreMenu = N'Orden de Trabajo';

    IF @IdProduccion IS NOT NULL AND @IdOrdenTrabajo IS NOT NULL
    BEGIN
        UPDATE dbo.Menu
        SET IdMenuPadre = @IdProduccion,
            Orden = 2,
            Estado = 1
        WHERE IdMenu = @IdOrdenTrabajo;
    END;

    UPDATE dbo.Menu
    SET Estado = 0
    WHERE NombreMenu = N'Seguimiento OT';

    UPDATE dbo.Menu
    SET Orden = 1
    WHERE NombreMenu = N'Áreas de Producción'
      AND IdMenuPadre = @IdProduccion;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
