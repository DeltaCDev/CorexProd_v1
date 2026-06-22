SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Usuario INT = (SELECT TOP (1) IdUsuario FROM dbo.Usuarios ORDER BY IdUsuario);
IF @Usuario IS NULL THROW 51000, 'Se requiere al menos un usuario para ejecutar las pruebas.', 1;

BEGIN TRY
    DECLARE @Mensaje NVARCHAR(500), @IdCorte INT, @IdEmpaque INT;

    DELETE dbo.AreaProduccion WHERE CodigoArea LIKE 'TST[_]%';
    DELETE dbo.Auditoria WHERE Modulo = N'Áreas de Producción' AND Descripcion LIKE N'%TST[_]%';

    EXEC dbo.USP_PRO_AREA_PRODUCCION_GUARDAR 0, 'TST_CORTE', N'Corte prueba', N'', 10, 1, 1, 0, 'PARCIAL', 1, @Usuario, @Mensaje OUTPUT;
    IF @Mensaje NOT LIKE N'OK|%' THROW 51000, 'Falló creación de área de inicio.', 1;
    SET @IdCorte = SCOPE_IDENTITY();

    EXEC dbo.USP_PRO_AREA_PRODUCCION_GUARDAR 0, 'TST_EMPAQUE', N'Empaque prueba', N'', 20, 0, 0, 1, 'UNICO', 1, @Usuario, @Mensaje OUTPUT;
    IF @Mensaje NOT LIKE N'OK|%' THROW 51000, 'Falló creación de área de término.', 1;

    EXEC dbo.USP_PRO_AREA_PRODUCCION_GUARDAR 0, 'TST_CORTE', N'Duplicado', N'', 30, 0, 0, 0, 'UNICO', 1, @Usuario, @Mensaje OUTPUT;
    IF @Mensaje NOT LIKE N'Ya existe un área registrada%' THROW 51000, 'No se rechazó el código duplicado.', 1;

    EXEC dbo.USP_PRO_AREA_PRODUCCION_GUARDAR 0, 'TST_DUP_ORDEN', N'Duplicado orden', N'', 20, 0, 0, 0, 'UNICO', 1, @Usuario, @Mensaje OUTPUT;
    IF @Mensaje NOT LIKE N'Ya existe un área activa con el orden%' THROW 51000, 'No se rechazó la secuencia duplicada.', 1;

    EXEC dbo.USP_PRO_AREA_PRODUCCION_GUARDAR 0, 'TST_INICIO2', N'Inicio duplicado', N'', 5, 1, 0, 0, 'UNICO', 1, @Usuario, @Mensaje OUTPUT;
    IF @Mensaje NOT LIKE N'Solo puede existir un área de inicio%' THROW 51000, 'No se rechazó un segundo inicio.', 1;

    SET @IdCorte = (SELECT IdAreaProduccion FROM dbo.AreaProduccion WHERE CodigoArea = 'TST_CORTE');
    EXEC dbo.USP_PRO_AREA_PRODUCCION_CAMBIAR_ESTADO @IdCorte, 0, @Usuario, @Mensaje OUTPUT;
    IF @Mensaje NOT LIKE N'No se puede desactivar el área de inicio%' THROW 51000, 'No se protegió la desactivación del inicio.', 1;

    EXEC dbo.USP_PRO_AREA_PRODUCCION_GUARDAR @IdCorte, 'TST_CORTE', N'Corte editado', N'Edición correcta', 10, 1, 0, 0, 'UNICO', 1, @Usuario, @Mensaje OUTPUT;
    IF @Mensaje NOT LIKE N'OK|%' OR NOT EXISTS (SELECT 1 FROM dbo.AreaProduccion WHERE IdAreaProduccion = @IdCorte AND NombreArea = N'Corte editado')
        THROW 51000, 'Falló la edición.', 1;

    DELETE dbo.AreaProduccion WHERE CodigoArea LIKE 'TST[_]%';
    DELETE dbo.Auditoria WHERE Modulo = N'Áreas de Producción' AND Descripcion LIKE N'%TST[_]%';
    PRINT 'OK: creación, edición, duplicados, extremos y desactivación validados.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DELETE dbo.AreaProduccion WHERE CodigoArea LIKE 'TST[_]%';
    DELETE dbo.Auditoria WHERE Modulo = N'Áreas de Producción' AND Descripcion LIKE N'%TST[_]%';
    THROW;
END CATCH;
