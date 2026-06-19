USE CorexProdDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.TiposDocumentoNumeracion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TiposDocumentoNumeracion
    (
        CodigoTipoDocumento VARCHAR(50) NOT NULL CONSTRAINT PK_TiposDocumentoNumeracion PRIMARY KEY,
        NombreTipoDocumento VARCHAR(150) NOT NULL,
        Estado BIT NOT NULL CONSTRAINT DF_TiposDocumentoNumeracion_Estado DEFAULT(1),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_TiposDocumentoNumeracion_Fecha DEFAULT(GETDATE())
    );
END;
GO

MERGE dbo.TiposDocumentoNumeracion AS T
USING (VALUES
    ('PROFORMA', 'Proformas'),
    ('INGRESO_PRODUCTOS', 'Ingreso de Stock de Productos'),
    ('INGRESO_INSUMOS', 'Ingreso de Stock de Insumos o Suministros'),
    ('GUIA_SALIDA', 'Guías de Salida')
) AS S(Codigo, Nombre)
ON T.CodigoTipoDocumento = S.Codigo
WHEN MATCHED THEN UPDATE SET NombreTipoDocumento = S.Nombre, Estado = 1
WHEN NOT MATCHED THEN INSERT (CodigoTipoDocumento, NombreTipoDocumento) VALUES (S.Codigo, S.Nombre);
GO

IF OBJECT_ID('dbo.SeriesCorrelativos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SeriesCorrelativos
    (
        IdSerieCorrelativo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesCorrelativos PRIMARY KEY,
        CodigoTipoDocumento VARCHAR(50) NOT NULL,
        Serie VARCHAR(20) NOT NULL,
        UltimoCorrelativo BIGINT NOT NULL CONSTRAINT DF_SeriesCorrelativos_Ultimo DEFAULT(0),
        CantidadDigitos TINYINT NOT NULL CONSTRAINT DF_SeriesCorrelativos_Digitos DEFAULT(6),
        Activa BIT NOT NULL CONSTRAINT DF_SeriesCorrelativos_Activa DEFAULT(1),
        Predeterminada BIT NOT NULL CONSTRAINT DF_SeriesCorrelativos_Pred DEFAULT(0),
        UsuarioModificacion VARCHAR(80) NOT NULL,
        FechaModificacion DATETIME NOT NULL CONSTRAINT DF_SeriesCorrelativos_Fecha DEFAULT(GETDATE()),
        FechaUltimoUso DATETIME NULL,
        CONSTRAINT FK_SeriesCorrelativos_Tipo FOREIGN KEY (CodigoTipoDocumento)
            REFERENCES dbo.TiposDocumentoNumeracion(CodigoTipoDocumento),
        CONSTRAINT UQ_SeriesCorrelativos_TipoSerie UNIQUE (CodigoTipoDocumento, Serie),
        CONSTRAINT CK_SeriesCorrelativos_Digitos CHECK (CantidadDigitos BETWEEN 1 AND 12),
        CONSTRAINT CK_SeriesCorrelativos_Ultimo CHECK (UltimoCorrelativo >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.SeriesCorrelativos') AND name = 'UX_SeriesCorrelativos_Predeterminada')
    CREATE UNIQUE INDEX UX_SeriesCorrelativos_Predeterminada
        ON dbo.SeriesCorrelativos(CodigoTipoDocumento)
        WHERE Predeterminada = 1 AND Activa = 1;
GO

IF OBJECT_ID('dbo.SeriesCorrelativosHistorial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SeriesCorrelativosHistorial
    (
        IdHistorial BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SeriesCorrelativosHistorial PRIMARY KEY,
        IdSerieCorrelativo INT NOT NULL,
        Accion VARCHAR(30) NOT NULL,
        SerieAnterior VARCHAR(20) NULL,
        SerieNueva VARCHAR(20) NULL,
        CorrelativoAnterior BIGINT NULL,
        CorrelativoNuevo BIGINT NULL,
        DigitosAnterior TINYINT NULL,
        DigitosNuevo TINYINT NULL,
        ActivaAnterior BIT NULL,
        ActivaNueva BIT NULL,
        PredeterminadaAnterior BIT NULL,
        PredeterminadaNueva BIT NULL,
        Usuario VARCHAR(80) NOT NULL,
        Fecha DATETIME NOT NULL CONSTRAINT DF_SeriesHistorial_Fecha DEFAULT(GETDATE()),
        CONSTRAINT FK_SeriesHistorial_Serie FOREIGN KEY (IdSerieCorrelativo)
            REFERENCES dbo.SeriesCorrelativos(IdSerieCorrelativo)
    );
END;
GO

DECLARE @SerieProforma VARCHAR(20) = ISNULL((SELECT TOP 1 ValorParametro FROM dbo.Parametros WHERE CodigoParametro='PROFORMA_SERIE'), 'PROF');
DECLARE @SiguienteProforma BIGINT = ISNULL(TRY_CONVERT(BIGINT, (SELECT TOP 1 ValorParametro FROM dbo.Parametros WHERE CodigoParametro='PROFORMA_CORRELATIVO')), 1);
IF NOT EXISTS (SELECT 1 FROM dbo.SeriesCorrelativos WHERE CodigoTipoDocumento='PROFORMA')
    INSERT INTO dbo.SeriesCorrelativos(CodigoTipoDocumento,Serie,UltimoCorrelativo,CantidadDigitos,Activa,Predeterminada,UsuarioModificacion)
    VALUES('PROFORMA', @SerieProforma, CASE WHEN @SiguienteProforma > 0 THEN @SiguienteProforma-1 ELSE 0 END, 6, 1, 1, 'MIGRACION');

IF NOT EXISTS (SELECT 1 FROM dbo.SeriesCorrelativos WHERE CodigoTipoDocumento='INGRESO_PRODUCTOS')
    INSERT INTO dbo.SeriesCorrelativos(CodigoTipoDocumento,Serie,UltimoCorrelativo,CantidadDigitos,Activa,Predeterminada,UsuarioModificacion)
    SELECT TOP 1 'INGRESO_PRODUCTOS', Serie, UltimoNumero, 8, 1, 1, 'MIGRACION' FROM dbo.SerieIngresoManualStock ORDER BY IdSerieIngresoManualStock;

IF NOT EXISTS (SELECT 1 FROM dbo.SeriesCorrelativos WHERE CodigoTipoDocumento='INGRESO_INSUMOS')
    INSERT INTO dbo.SeriesCorrelativos(CodigoTipoDocumento,Serie,UltimoCorrelativo,CantidadDigitos,Activa,Predeterminada,UsuarioModificacion)
    SELECT TOP 1 'INGRESO_INSUMOS', Serie, UltimoNumero, 8, 1, 1, 'MIGRACION' FROM dbo.SerieIngresoManualStockInsumo ORDER BY IdSerieIngresoManualStockInsumo;

IF NOT EXISTS (SELECT 1 FROM dbo.SeriesCorrelativos WHERE CodigoTipoDocumento='GUIA_SALIDA')
    INSERT INTO dbo.SeriesCorrelativos(CodigoTipoDocumento,Serie,UltimoCorrelativo,CantidadDigitos,Activa,Predeterminada,UsuarioModificacion)
    SELECT TOP 1 'GUIA_SALIDA', Serie, UltimoNumero, 8, 1, 1, 'MIGRACION' FROM dbo.SerieGuiaInterna ORDER BY Serie;

IF EXISTS (SELECT 1 FROM dbo.SeriesCorrelativos WHERE CodigoTipoDocumento='PROFORMA')
    DELETE FROM dbo.Parametros
    WHERE CodigoParametro IN ('PROFORMA_SERIE', 'PROFORMA_CORRELATIVO');
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_SERIE_LISTAR
    @CodigoTipoDocumento VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT S.IdSerieCorrelativo, S.CodigoTipoDocumento, T.NombreTipoDocumento, S.Serie,
           S.UltimoCorrelativo, S.CantidadDigitos, S.Activa, S.Predeterminada,
           S.UsuarioModificacion, S.FechaModificacion, S.FechaUltimoUso,
           CONCAT(S.Serie, '-', RIGHT(REPLICATE('0', S.CantidadDigitos) + CONVERT(VARCHAR(20), S.UltimoCorrelativo), S.CantidadDigitos)) AS UltimoNumeroGenerado
    FROM dbo.SeriesCorrelativos S
    INNER JOIN dbo.TiposDocumentoNumeracion T ON T.CodigoTipoDocumento=S.CodigoTipoDocumento
    WHERE @CodigoTipoDocumento IS NULL OR S.CodigoTipoDocumento=@CodigoTipoDocumento
    ORDER BY T.NombreTipoDocumento, S.Predeterminada DESC, S.Serie;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_TIPO_DOCUMENTO_NUMERACION_LISTAR
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CodigoTipoDocumento, NombreTipoDocumento, Estado
    FROM dbo.TiposDocumentoNumeracion WHERE Estado=1 ORDER BY NombreTipoDocumento;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_SERIE_HISTORIAL_LISTAR
    @IdSerieCorrelativo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdHistorial, IdSerieCorrelativo, Accion, SerieAnterior, SerieNueva,
           CorrelativoAnterior, CorrelativoNuevo, DigitosAnterior, DigitosNuevo,
           ActivaAnterior, ActivaNueva, PredeterminadaAnterior, PredeterminadaNueva, Usuario, Fecha
    FROM dbo.SeriesCorrelativosHistorial
    WHERE IdSerieCorrelativo=@IdSerieCorrelativo ORDER BY IdHistorial DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_SERIE_GUARDAR
    @IdSerieCorrelativo INT,
    @CodigoTipoDocumento VARCHAR(50),
    @Serie VARCHAR(20),
    @UltimoCorrelativo BIGINT,
    @CantidadDigitos TINYINT,
    @Activa BIT,
    @Predeterminada BIT,
    @Usuario VARCHAR(80),
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @Serie=UPPER(LTRIM(RTRIM(ISNULL(@Serie,''))));
    IF @Serie='' OR @Serie LIKE '%[^A-Z0-9_-]%' BEGIN SET @Mensaje='La serie solo admite letras, números, guion y guion bajo.'; RETURN; END;
    IF @CantidadDigitos NOT BETWEEN 1 AND 12 BEGIN SET @Mensaje='La cantidad de dígitos debe estar entre 1 y 12.'; RETURN; END;
    IF @UltimoCorrelativo < 0 BEGIN SET @Mensaje='El correlativo no puede ser negativo.'; RETURN; END;
    IF @Predeterminada=1 SET @Activa=1;

    BEGIN TRY
        BEGIN TRANSACTION;
        IF EXISTS(SELECT 1 FROM dbo.SeriesCorrelativos WITH(UPDLOCK,HOLDLOCK) WHERE CodigoTipoDocumento=@CodigoTipoDocumento AND Serie=@Serie AND IdSerieCorrelativo<>@IdSerieCorrelativo)
            THROW 51000, 'Ya existe esa serie para el tipo de documento.', 1;

        IF @Predeterminada=1
            UPDATE dbo.SeriesCorrelativos SET Predeterminada=0, UsuarioModificacion=@Usuario, FechaModificacion=GETDATE()
            WHERE CodigoTipoDocumento=@CodigoTipoDocumento AND IdSerieCorrelativo<>@IdSerieCorrelativo AND Predeterminada=1;

        IF @IdSerieCorrelativo=0
        BEGIN
            INSERT INTO dbo.SeriesCorrelativos(CodigoTipoDocumento,Serie,UltimoCorrelativo,CantidadDigitos,Activa,Predeterminada,UsuarioModificacion)
            VALUES(@CodigoTipoDocumento,@Serie,@UltimoCorrelativo,@CantidadDigitos,@Activa,@Predeterminada,@Usuario);
            SET @IdSerieCorrelativo=SCOPE_IDENTITY();
            INSERT INTO dbo.SeriesCorrelativosHistorial(IdSerieCorrelativo,Accion,SerieNueva,CorrelativoNuevo,DigitosNuevo,ActivaNueva,PredeterminadaNueva,Usuario)
            VALUES(@IdSerieCorrelativo,'CREACION',@Serie,@UltimoCorrelativo,@CantidadDigitos,@Activa,@Predeterminada,@Usuario);
        END
        ELSE
        BEGIN
            DECLARE @SerieA VARCHAR(20),@CorrA BIGINT,@DigA TINYINT,@ActA BIT,@PredA BIT;
            SELECT @SerieA=Serie,@CorrA=UltimoCorrelativo,@DigA=CantidadDigitos,@ActA=Activa,@PredA=Predeterminada
            FROM dbo.SeriesCorrelativos WITH(UPDLOCK,HOLDLOCK) WHERE IdSerieCorrelativo=@IdSerieCorrelativo;
            IF @CorrA IS NULL THROW 51001, 'No se encontró la serie.', 1;
            IF @UltimoCorrelativo < @CorrA THROW 51002, 'El último correlativo no puede disminuir.', 1;
            UPDATE dbo.SeriesCorrelativos SET Serie=@Serie,UltimoCorrelativo=@UltimoCorrelativo,CantidadDigitos=@CantidadDigitos,
                Activa=@Activa,Predeterminada=@Predeterminada,UsuarioModificacion=@Usuario,FechaModificacion=GETDATE()
            WHERE IdSerieCorrelativo=@IdSerieCorrelativo;
            INSERT INTO dbo.SeriesCorrelativosHistorial(IdSerieCorrelativo,Accion,SerieAnterior,SerieNueva,CorrelativoAnterior,CorrelativoNuevo,DigitosAnterior,DigitosNuevo,ActivaAnterior,ActivaNueva,PredeterminadaAnterior,PredeterminadaNueva,Usuario)
            VALUES(@IdSerieCorrelativo,'MODIFICACION',@SerieA,@Serie,@CorrA,@UltimoCorrelativo,@DigA,@CantidadDigitos,@ActA,@Activa,@PredA,@Predeterminada,@Usuario);
        END
        COMMIT; SET @Mensaje='Serie guardada correctamente.';
    END TRY
    BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; SET @Mensaje=ERROR_MESSAGE(); END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_SERIE_OBTENER_PREDETERMINADA
    @CodigoTipoDocumento VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 IdSerieCorrelativo,CodigoTipoDocumento,Serie,UltimoCorrelativo,CantidadDigitos,Activa,Predeterminada
    FROM dbo.SeriesCorrelativos WHERE CodigoTipoDocumento=@CodigoTipoDocumento AND Activa=1 AND Predeterminada=1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_SEG_SERIE_TOMAR_SIGUIENTE
    @CodigoTipoDocumento VARCHAR(50),
    @Serie VARCHAR(20) OUTPUT,
    @Correlativo BIGINT OUTPUT,
    @Numero VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id INT,@Digitos TINYINT;
    SELECT TOP 1 @Id=IdSerieCorrelativo,@Serie=Serie,@Correlativo=UltimoCorrelativo+1,@Digitos=CantidadDigitos
    FROM dbo.SeriesCorrelativos WITH(UPDLOCK,HOLDLOCK)
    WHERE CodigoTipoDocumento=@CodigoTipoDocumento AND Activa=1 AND Predeterminada=1;
    IF @Id IS NULL THROW 51010, 'No existe una serie activa y predeterminada para este documento.', 1;
    IF LEN(CONVERT(VARCHAR(20),@Correlativo))>@Digitos THROW 51011, 'El correlativo excedió la cantidad de dígitos configurada.', 1;
    UPDATE dbo.SeriesCorrelativos SET UltimoCorrelativo=@Correlativo,FechaUltimoUso=GETDATE() WHERE IdSerieCorrelativo=@Id;
    SET @Numero=RIGHT(REPLICATE('0',@Digitos)+CONVERT(VARCHAR(20),@Correlativo),@Digitos);
END;
GO

DECLARE @IdSeguridad INT=(SELECT TOP 1 IdMenu FROM dbo.Menu WHERE NombreMenu='Seguridad' AND IdMenuPadre IS NULL);
IF @IdSeguridad IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Menu WHERE NombreMenu='Series y Correlativos' AND IdMenuPadre=@IdSeguridad)
    INSERT INTO dbo.Menu(NombreMenu,IdMenuPadre,Orden,Estado) VALUES('Series y Correlativos',@IdSeguridad,9,1);
DECLARE @IdMenuSerie INT=(SELECT TOP 1 IdMenu FROM dbo.Menu WHERE NombreMenu='Series y Correlativos' AND IdMenuPadre=@IdSeguridad);
IF @IdMenuSerie IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.PermisosMenu WHERE IdRol=1 AND IdMenu=@IdMenuSerie)
    INSERT INTO dbo.PermisosMenu(IdRol,IdMenu,PuedeVer) VALUES(1,@IdMenuSerie,1);
GO
