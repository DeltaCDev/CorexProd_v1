SET NOCOUNT ON;
GO

DECLARE @IdVentas INT = (
    SELECT TOP (1) IdMenu
    FROM dbo.Menu
    WHERE NombreMenu = 'Ventas'
      AND IdMenuPadre IS NULL
    ORDER BY IdMenu
);

DECLARE @IdProformas INT = (
    SELECT TOP (1) IdMenu
    FROM dbo.Menu
    WHERE NombreMenu = 'Proformas'
      AND IdMenuPadre = @IdVentas
    ORDER BY IdMenu
);

DECLARE @IdOrdenCompra INT = (
    SELECT TOP (1) IdMenu
    FROM dbo.Menu
    WHERE NombreMenu IN ('OCI', 'Orden de Compra')
      AND IdMenuPadre = @IdVentas
    ORDER BY CASE WHEN NombreMenu = 'Orden de Compra' THEN 0 ELSE 1 END, IdMenu
);

IF @IdOrdenCompra IS NOT NULL
BEGIN
    UPDATE dbo.Menu
    SET NombreMenu = 'Orden de Compra',
        Orden = 1,
        Estado = 1
    WHERE IdMenu = @IdOrdenCompra;
END;

IF @IdProformas IS NOT NULL AND @IdOrdenCompra IS NOT NULL
BEGIN
    INSERT INTO dbo.PermisosMenu (IdRol, IdMenu, PuedeVer)
    SELECT pp.IdRol, @IdOrdenCompra, pp.PuedeVer
    FROM dbo.PermisosMenu pp
    WHERE pp.IdMenu = @IdProformas
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.PermisosMenu po
          WHERE po.IdRol = pp.IdRol
            AND po.IdMenu = @IdOrdenCompra
      );

    UPDATE po
    SET PuedeVer = 1
    FROM dbo.PermisosMenu po
    INNER JOIN dbo.PermisosMenu pp
        ON pp.IdRol = po.IdRol
       AND pp.IdMenu = @IdProformas
       AND pp.PuedeVer = 1
    WHERE po.IdMenu = @IdOrdenCompra
      AND po.PuedeVer = 0;

    UPDATE dbo.Menu
    SET Estado = 0,
        Orden = 99
    WHERE IdMenu = @IdProformas;
END;

DECLARE @IdGuiaSalida INT = (
    SELECT TOP (1) IdMenu
    FROM dbo.Menu
    WHERE NombreMenu = 'Guía de Salida'
      AND IdMenuPadre = @IdVentas
    ORDER BY IdMenu
);

IF @IdGuiaSalida IS NOT NULL
BEGIN
    UPDATE dbo.Menu
    SET Orden = 2
    WHERE IdMenu = @IdGuiaSalida;
END;
GO

SELECT IdMenu, NombreMenu, IdMenuPadre, Orden, Estado
FROM dbo.Menu
WHERE NombreMenu IN ('Proformas', 'OCI', 'Orden de Compra', 'Guía de Salida')
ORDER BY IdMenuPadre, Orden, NombreMenu;
GO
