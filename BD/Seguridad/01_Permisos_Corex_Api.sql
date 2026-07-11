USE [CorexProdDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/*
    Permisos requeridos por CorexProd.Api.

    La API se conecta mediante el usuario de base de datos [corex_api].
    db_datareader y db_datawriter no otorgan permiso para ejecutar
    procedimientos almacenados. El panel Android usa, entre otros:

      - dbo.USP_SEG_USUARIO_LOGIN
      - dbo.USP_PRO_OT_LISTAR
      - dbo.USP_VEN_GUIA_INTERNA_LISTAR
      - dbo.USP_PRO_OT_OBTENER
      - procedimientos de OC, OT, guias y reservas

    El permiso sobre el esquema dbo cubre los procedimientos actuales
    y los que se creen posteriormente dentro del mismo esquema.
*/

IF USER_ID(N'corex_api') IS NULL
    THROW 51000, 'No existe el usuario de base de datos corex_api.', 1;
GO

GRANT CONNECT TO [corex_api];
GRANT EXECUTE ON SCHEMA::[dbo] TO [corex_api];
GO

EXECUTE AS USER = N'corex_api';

SELECT
    USER_NAME() AS UsuarioValidado,
    HAS_PERMS_BY_NAME(N'dbo.USP_SEG_USUARIO_LOGIN', N'OBJECT', N'EXECUTE') AS PuedeLogin,
    HAS_PERMS_BY_NAME(N'dbo.USP_PRO_OT_LISTAR', N'OBJECT', N'EXECUTE') AS PuedeListarOT,
    HAS_PERMS_BY_NAME(N'dbo.USP_VEN_GUIA_INTERNA_LISTAR', N'OBJECT', N'EXECUTE') AS PuedeListarGuias,
    HAS_PERMS_BY_NAME(N'dbo.USP_PRO_OT_VALIDAR_INSUMOS', N'OBJECT', N'EXECUTE') AS PuedeValidarOT;

REVERT;
GO

PRINT 'Permisos de ejecucion para CorexProd.Api configurados correctamente.';
GO
