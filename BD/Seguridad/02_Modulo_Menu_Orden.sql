SET NOCOUNT ON;

UPDATE dbo.Menu SET Orden = 1 WHERE NombreMenu = 'Inicio' AND IdMenuPadre IS NULL;
UPDATE dbo.Menu SET Orden = 2 WHERE NombreMenu = 'Ventas' AND IdMenuPadre IS NULL;
UPDATE dbo.Menu SET Orden = 3 WHERE NombreMenu LIKE 'Producci%' AND IdMenuPadre IS NULL;
UPDATE dbo.Menu SET Orden = 4 WHERE NombreMenu = 'Reportes' AND IdMenuPadre IS NULL;
UPDATE dbo.Menu SET Orden = 5 WHERE NombreMenu LIKE 'Almac%' AND IdMenuPadre IS NULL;
UPDATE dbo.Menu SET Orden = 6 WHERE NombreMenu = 'Productos' AND IdMenuPadre IS NULL;
UPDATE dbo.Menu SET Orden = 7 WHERE NombreMenu = 'Destajo y Pagos' AND IdMenuPadre IS NULL;
UPDATE dbo.Menu SET Orden = 8 WHERE NombreMenu = 'Seguridad' AND IdMenuPadre IS NULL;

DECLARE @IdSeguridad INT;
DECLARE @NombreMenuAdministracion NVARCHAR(80) = N'Men' + NCHAR(250);

SELECT @IdSeguridad = IdMenu
FROM dbo.Menu
WHERE NombreMenu = 'Seguridad'
  AND IdMenuPadre IS NULL;

IF @IdSeguridad IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM dbo.Menu
       WHERE NombreMenu = @NombreMenuAdministracion
         AND IdMenuPadre = @IdSeguridad
   )
BEGIN
    INSERT INTO dbo.Menu (NombreMenu, IdMenuPadre, Orden, Estado)
    VALUES (@NombreMenuAdministracion, @IdSeguridad, 10, 1);
END;

UPDATE dbo.Menu
SET NombreMenu = @NombreMenuAdministracion
WHERE IdMenuPadre = @IdSeguridad
  AND (NombreMenu LIKE 'Men%' OR NombreMenu = 'Menu');

;WITH MenuDuplicado AS
(
    SELECT
        IdMenu,
        ROW_NUMBER() OVER (ORDER BY IdMenu) AS rn
    FROM dbo.Menu
    WHERE IdMenuPadre = @IdSeguridad
      AND NombreMenu = @NombreMenuAdministracion
)
DELETE pm
FROM dbo.PermisosMenu pm
INNER JOIN MenuDuplicado md ON md.IdMenu = pm.IdMenu
WHERE md.rn > 1;

;WITH MenuDuplicado AS
(
    SELECT
        IdMenu,
        ROW_NUMBER() OVER (ORDER BY IdMenu) AS rn
    FROM dbo.Menu
    WHERE IdMenuPadre = @IdSeguridad
      AND NombreMenu = @NombreMenuAdministracion
)
DELETE m
FROM dbo.Menu m
INNER JOIN MenuDuplicado md ON md.IdMenu = m.IdMenu
WHERE md.rn > 1;

DECLARE @IdMenuAdministracion INT;
SELECT @IdMenuAdministracion = IdMenu
FROM dbo.Menu
WHERE NombreMenu = @NombreMenuAdministracion
  AND IdMenuPadre = @IdSeguridad;

IF @IdMenuAdministracion IS NOT NULL
BEGIN
    INSERT INTO dbo.PermisosMenu (IdRol, IdMenu, PuedeVer)
    SELECT pm.IdRol, @IdMenuAdministracion, pm.PuedeVer
    FROM dbo.PermisosMenu pm
    WHERE pm.IdMenu = @IdSeguridad
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.PermisosMenu existe
          WHERE existe.IdRol = pm.IdRol
            AND existe.IdMenu = @IdMenuAdministracion
      );
END;

SELECT IdMenu, NombreMenu, IdMenuPadre, Orden, Estado
FROM dbo.Menu
WHERE IdMenuPadre IS NULL
   OR IdMenuPadre = @IdSeguridad
ORDER BY
    CASE WHEN IdMenuPadre IS NULL THEN Orden ELSE 99 END,
    IdMenuPadre,
    Orden,
    NombreMenu;
