USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = 'corex_api')
BEGIN
    CREATE LOGIN [corex_api]
    WITH PASSWORD = 'CorexApi_2026_Local!',
         CHECK_POLICY = OFF,
         CHECK_EXPIRATION = OFF;
END
GO

USE [CorexProdDB];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'corex_api')
BEGIN
    CREATE USER [corex_api] FOR LOGIN [corex_api];
END
GO

IF IS_ROLEMEMBER('db_datareader', 'corex_api') = 0
    ALTER ROLE [db_datareader] ADD MEMBER [corex_api];

IF IS_ROLEMEMBER('db_datawriter', 'corex_api') = 0
    ALTER ROLE [db_datawriter] ADD MEMBER [corex_api];

GRANT EXECUTE TO [corex_api];
GO

PRINT 'Usuario SQL corex_api configurado para CorexProd.Api.';
