USE CorexProdDB;
GO

IF OBJECT_ID('dbo.OrdenesCompraInterna', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenesCompraInterna
    (
        IdOrdenCompraInterna INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenesCompraInterna PRIMARY KEY,
        NumeroOci VARCHAR(40) NOT NULL,
        IdProforma INT NOT NULL,
        FechaEmision DATE NOT NULL,
        OrdenCompraCliente VARCHAR(100) NOT NULL CONSTRAINT DF_OCI_OrdenCompraCliente DEFAULT(''),
        IdCliente INT NOT NULL,
        NombreCliente VARCHAR(250) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCI_Subtotal DEFAULT(0),
        Descuento DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCI_Descuento DEFAULT(0),
        Igv DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCI_Igv DEFAULT(0),
        IgvPorcentaje DECIMAL(9,4) NOT NULL CONSTRAINT DF_OCI_IgvPorcentaje DEFAULT(0),
        CondicionTributaria VARCHAR(50) NOT NULL CONSTRAINT DF_OCI_CondicionTributaria DEFAULT('Exonerado de IGV'),
        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_OCI_Total DEFAULT(0),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_OCI_Estado DEFAULT('Emitida'),
        UsuarioGenerador VARCHAR(80) NOT NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OCI_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT UQ_OCI_Numero UNIQUE (NumeroOci),
        CONSTRAINT UQ_OCI_Proforma UNIQUE (IdProforma),
        CONSTRAINT FK_OCI_Proforma FOREIGN KEY (IdProforma) REFERENCES dbo.Proformas(IdProforma),
        CONSTRAINT FK_OCI_Cliente FOREIGN KEY (IdCliente) REFERENCES dbo.Clientes(IdCliente)
    );
END;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'IgvPorcentaje') IS NULL
    ALTER TABLE dbo.OrdenesCompraInterna ADD IgvPorcentaje DECIMAL(9,4) NOT NULL
        CONSTRAINT DF_OCI_IgvPorcentaje DEFAULT(0) WITH VALUES;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'CondicionTributaria') IS NULL
    ALTER TABLE dbo.OrdenesCompraInterna ADD CondicionTributaria VARCHAR(50) NOT NULL
        CONSTRAINT DF_OCI_CondicionTributaria DEFAULT('Exonerado de IGV') WITH VALUES;
GO

UPDATE dbo.OrdenesCompraInterna
SET IgvPorcentaje = CASE WHEN Subtotal <> 0 THEN ROUND(Igv * 100 / Subtotal, 4) ELSE IgvPorcentaje END,
    CondicionTributaria = 'Gravado con IGV'
WHERE Igv > 0 AND IgvPorcentaje = 0;
GO

/* MODULO DE GUIAS INTERNAS: ORIGEN OCI Y MANUAL */
IF OBJECT_ID('dbo.GuiasInternas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuiasInternas
    (
        IdGuiaInterna INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GuiasInternas PRIMARY KEY,
        NumeroGuia VARCHAR(30) NOT NULL,
        Origen VARCHAR(20) NOT NULL CONSTRAINT DF_GuiasInternas_Origen DEFAULT('OCI'),
        IdOrdenCompraInterna INT NULL,
        IdCliente INT NULL,
        IdAlmacen INT NOT NULL,
        FechaEmision DATE NOT NULL,
        RucEmisor VARCHAR(20) NOT NULL,
        EmpresaEmisora VARCHAR(250) NOT NULL,
        RucDestino VARCHAR(20) NOT NULL,
        EmpresaDestino VARCHAR(250) NOT NULL,
        UsuarioEmisor VARCHAR(80) NOT NULL,
        UsuarioAutorizador VARCHAR(80) NOT NULL,
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_GuiaInterna_Observacion DEFAULT(''),
        MotivoEmisionManual VARCHAR(500) NOT NULL CONSTRAINT DF_GuiasInternas_MotivoManual DEFAULT(''),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_GuiaInterna_Estado DEFAULT('Emitida'),
        UsuarioAnulacion VARCHAR(80) NULL,
        FechaAnulacion DATETIME NULL,
        MotivoAnulacion VARCHAR(500) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_GuiaInterna_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT UQ_GuiasInternas_Numero UNIQUE(NumeroGuia),
        CONSTRAINT FK_GuiasInternas_OCI FOREIGN KEY(IdOrdenCompraInterna) REFERENCES dbo.OrdenesCompraInterna(IdOrdenCompraInterna),
        CONSTRAINT FK_GuiasInternas_Cliente FOREIGN KEY(IdCliente) REFERENCES dbo.Clientes(IdCliente),
        CONSTRAINT FK_GuiasInternas_Almacen FOREIGN KEY(IdAlmacen) REFERENCES dbo.Almacenes(IdAlmacen)
    );
END;
GO

IF OBJECT_ID('dbo.GuiaInternaDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuiaInternaDetalle
    (
        IdGuiaInternaDetalle INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GuiaInternaDetalle PRIMARY KEY,
        IdGuiaInterna INT NOT NULL, IdOrdenCompraInternaDetalle INT NULL, IdProducto INT NOT NULL,
        CodigoProducto VARCHAR(100) NOT NULL, NombreProducto VARCHAR(500) NOT NULL,
        IdUnidadMedida INT NOT NULL, NombreUnidad VARCHAR(100) NOT NULL,
        CantidadRequerida DECIMAL(18,2) NOT NULL, CantidadDespachada DECIMAL(18,2) NOT NULL,
        StockAnterior DECIMAL(18,2) NOT NULL, PrecioUnitario DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_GuiaInternaDetalle_Observacion DEFAULT(''),
        CONSTRAINT FK_GuiaInternaDetalle_Guia FOREIGN KEY(IdGuiaInterna) REFERENCES dbo.GuiasInternas(IdGuiaInterna),
        CONSTRAINT FK_GuiaInternaDetalle_Producto FOREIGN KEY(IdProducto) REFERENCES dbo.Productos(IdProducto),
        CONSTRAINT FK_GuiaInternaDetalle_Unidad FOREIGN KEY(IdUnidadMedida) REFERENCES dbo.UnidadesMedida(IdUnidadMedida)
    );
END;
GO

IF COL_LENGTH('dbo.KardexProductos', 'IdGuiaInterna') IS NULL
    ALTER TABLE dbo.KardexProductos ADD IdGuiaInterna INT NULL;
GO

IF COL_LENGTH('dbo.GuiasInternas', 'Origen') IS NULL
    ALTER TABLE dbo.GuiasInternas ADD Origen VARCHAR(20) NOT NULL CONSTRAINT DF_GuiasInternas_Origen DEFAULT('OCI') WITH VALUES;
GO
IF COL_LENGTH('dbo.GuiasInternas', 'MotivoEmisionManual') IS NULL
    ALTER TABLE dbo.GuiasInternas ADD MotivoEmisionManual VARCHAR(500) NOT NULL CONSTRAINT DF_GuiasInternas_MotivoManual DEFAULT('') WITH VALUES;
GO
IF COL_LENGTH('dbo.GuiasInternas', 'IdCliente') IS NULL
    ALTER TABLE dbo.GuiasInternas ADD IdCliente INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_GuiasInternas_Cliente')
    ALTER TABLE dbo.GuiasInternas ADD CONSTRAINT FK_GuiasInternas_Cliente FOREIGN KEY(IdCliente) REFERENCES dbo.Clientes(IdCliente);
GO
IF COL_LENGTH('dbo.GuiasInternas', 'UsuarioAnulacion') IS NULL
    ALTER TABLE dbo.GuiasInternas ADD UsuarioAnulacion VARCHAR(80) NULL;
GO
IF COL_LENGTH('dbo.GuiasInternas', 'FechaAnulacion') IS NULL
    ALTER TABLE dbo.GuiasInternas ADD FechaAnulacion DATETIME NULL;
GO
IF COL_LENGTH('dbo.GuiasInternas', 'MotivoAnulacion') IS NULL
    ALTER TABLE dbo.GuiasInternas ADD MotivoAnulacion VARCHAR(500) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.GuiasInternas') AND name='IdOrdenCompraInterna' AND is_nullable=0)
    ALTER TABLE dbo.GuiasInternas ALTER COLUMN IdOrdenCompraInterna INT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.GuiaInternaDetalle') AND name='IdOrdenCompraInternaDetalle' AND is_nullable=0)
    ALTER TABLE dbo.GuiaInternaDetalle ALTER COLUMN IdOrdenCompraInternaDetalle INT NULL;
GO

IF TYPE_ID('dbo.GuiaInternaManualDetalleType') IS NULL
    EXEC('CREATE TYPE dbo.GuiaInternaManualDetalleType AS TABLE
    (
        IdProducto INT NOT NULL,
        CantidadDespachar DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NULL
    )');
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_LISTAR
    @FechaDesde DATE=NULL,
    @FechaHasta DATE=NULL,
    @IdAlmacen INT=NULL,
    @Estado VARCHAR(20)=NULL,
    @Origen VARCHAR(20)=NULL,
    @Texto VARCHAR(100)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT G.IdGuiaInterna,G.NumeroGuia,G.Origen,ISNULL(G.IdOrdenCompraInterna,0) IdOrdenCompraInterna,G.IdCliente,
           ISNULL(O.NumeroOci,'') NumeroOci,ISNULL(P.SerieNumero,'') NumeroProforma,ISNULL(O.OrdenCompraCliente,'') OrdenCompraCliente,
           G.FechaEmision,G.IdAlmacen,A.NombreAlmacen,G.RucEmisor,G.EmpresaEmisora,G.RucDestino,G.EmpresaDestino,
           G.UsuarioEmisor,G.UsuarioAutorizador,G.Observacion,G.MotivoEmisionManual,G.Estado,
           ISNULL(G.UsuarioAnulacion,'') UsuarioAnulacion,G.FechaAnulacion,
           ISNULL(G.MotivoAnulacion,'') MotivoAnulacion,G.FechaRegistro
    FROM dbo.GuiasInternas G
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen=G.IdAlmacen
    LEFT JOIN dbo.OrdenesCompraInterna O ON O.IdOrdenCompraInterna=G.IdOrdenCompraInterna
    LEFT JOIN dbo.Proformas P ON P.IdProforma=O.IdProforma
    WHERE (@FechaDesde IS NULL OR G.FechaEmision>=@FechaDesde)
      AND (@FechaHasta IS NULL OR G.FechaEmision<=@FechaHasta)
      AND (@IdAlmacen IS NULL OR G.IdAlmacen=@IdAlmacen)
      AND (@Estado IS NULL OR G.Estado=@Estado)
      AND (@Origen IS NULL OR G.Origen=@Origen)
      AND (@Texto IS NULL OR G.NumeroGuia LIKE '%'+@Texto+'%' OR ISNULL(O.NumeroOci,'') LIKE '%'+@Texto+'%'
           OR G.EmpresaDestino LIKE '%'+@Texto+'%' OR G.MotivoEmisionManual LIKE '%'+@Texto+'%')
    ORDER BY G.FechaEmision DESC,G.IdGuiaInterna DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_OBTENER
    @IdGuiaInterna INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT G.IdGuiaInterna,G.NumeroGuia,G.Origen,ISNULL(G.IdOrdenCompraInterna,0) IdOrdenCompraInterna,G.IdCliente,
           ISNULL(O.NumeroOci,'') NumeroOci,ISNULL(P.SerieNumero,'') NumeroProforma,ISNULL(O.OrdenCompraCliente,'') OrdenCompraCliente,
           G.FechaEmision,G.IdAlmacen,A.NombreAlmacen,G.RucEmisor,G.EmpresaEmisora,G.RucDestino,G.EmpresaDestino,
           G.UsuarioEmisor,G.UsuarioAutorizador,G.Observacion,G.MotivoEmisionManual,G.Estado,
           ISNULL(G.UsuarioAnulacion,'') UsuarioAnulacion,G.FechaAnulacion,
           ISNULL(G.MotivoAnulacion,'') MotivoAnulacion,G.FechaRegistro
    FROM dbo.GuiasInternas G
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen=G.IdAlmacen
    LEFT JOIN dbo.OrdenesCompraInterna O ON O.IdOrdenCompraInterna=G.IdOrdenCompraInterna
    LEFT JOIN dbo.Proformas P ON P.IdProforma=O.IdProforma
    WHERE G.IdGuiaInterna=@IdGuiaInterna;

    SELECT D.IdGuiaInternaDetalle,ISNULL(D.IdOrdenCompraInternaDetalle,0) IdOrdenCompraInternaDetalle,
           D.IdProducto,D.CodigoProducto,D.NombreProducto,D.IdUnidadMedida,D.NombreUnidad,
           D.CantidadRequerida,
           ISNULL(OD.CantidadDespachada,D.CantidadDespachada) CantidadEntregada,
           CASE WHEN OD.IdOrdenCompraInternaDetalle IS NULL THEN CAST(0 AS DECIMAL(18,2))
                WHEN OD.Cantidad>OD.CantidadDespachada THEN OD.Cantidad-OD.CantidadDespachada
                ELSE CAST(0 AS DECIMAL(18,2)) END CantidadPendiente,
           D.StockAnterior StockActual,D.PrecioUnitario,D.CantidadDespachada CantidadSugerida,D.Observacion
    FROM dbo.GuiaInternaDetalle D
    LEFT JOIN dbo.OrdenCompraInternaDetalle OD ON OD.IdOrdenCompraInternaDetalle=D.IdOrdenCompraInternaDetalle
    WHERE D.IdGuiaInterna=@IdGuiaInterna ORDER BY D.IdGuiaInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_MANUAL_PREPARAR
    @IdAlmacen INT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @IdAlmacen IS NULL SELECT TOP(1) @IdAlmacen=IdAlmacen FROM dbo.Almacenes WHERE Estado=1 ORDER BY IdAlmacen;
    SELECT A.IdAlmacen,A.NombreAlmacen,ISNULL(E.Ruc,'') RucEmisor,ISNULL(E.Nombre,'') EmpresaEmisora
    FROM dbo.Almacenes A
    OUTER APPLY(SELECT TOP(1) Ruc,Nombre FROM dbo.Empresas WHERE Estado=1 ORDER BY EsPredeterminada DESC,IdEmpresa) E
    WHERE A.IdAlmacen=@IdAlmacen AND A.Estado=1;

    SELECT P.IdProducto,P.Codigo CodigoProducto,P.NombreProducto,P.IdUnidadMedida,U.NombreUnidad,
           CAST(S.StockActual AS DECIMAL(18,2)) StockActual
    FROM dbo.StockProductosAlmacen S
    INNER JOIN dbo.Productos P ON P.IdProducto=S.IdProducto AND P.Estado=1
    INNER JOIN dbo.UnidadesMedida U ON U.IdUnidadMedida=P.IdUnidadMedida
    WHERE S.IdAlmacen=@IdAlmacen AND S.StockActual>0 ORDER BY P.NombreProducto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_MANUAL_EMITIR
    @IdAlmacen INT,@FechaEmision DATE,@UsuarioEmisor VARCHAR(80),@UsuarioAutorizador VARCHAR(80),
    @IdCliente INT=NULL,@MotivoEmisionManual VARCHAR(500),@Observacion VARCHAR(500),
    @Detalles dbo.GuiaInternaManualDetalleType READONLY,@NumeroGuia VARCHAR(30) OUTPUT,@Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; SET @NumeroGuia='';
    SET @MotivoEmisionManual=LTRIM(RTRIM(ISNULL(@MotivoEmisionManual,'')));
    IF @MotivoEmisionManual='' BEGIN SET @Mensaje='Debe seleccionar el motivo de salida o destino.'; RETURN; END;
    IF @MotivoEmisionManual='Entrega a cliente' AND @IdCliente IS NULL BEGIN SET @Mensaje='Debe seleccionar un cliente para una entrega a cliente.'; RETURN; END;
    IF @IdCliente IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Clientes WHERE IdCliente=@IdCliente AND Estado=1) BEGIN SET @Mensaje='El cliente seleccionado no existe o está inactivo.'; RETURN; END;
    IF NOT EXISTS(SELECT 1 FROM @Detalles WHERE CantidadDespachar>0) BEGIN SET @Mensaje='Debe indicar al menos un producto para despachar.'; RETURN; END;
    IF EXISTS(SELECT IdProducto FROM @Detalles GROUP BY IdProducto HAVING COUNT(*)>1) BEGIN SET @Mensaje='No se permiten productos repetidos.'; RETURN; END;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF EXISTS
        (
            SELECT 1 FROM @Detalles D
            LEFT JOIN dbo.StockProductosAlmacen S WITH(UPDLOCK,HOLDLOCK) ON S.IdProducto=D.IdProducto AND S.IdAlmacen=@IdAlmacen
            WHERE D.CantidadDespachar<=0 OR D.CantidadDespachar>ISNULL(S.StockActual,0)
        ) THROW 51100,'La cantidad supera el stock disponible. Actualice la lista.',1;

        DECLARE @Serie VARCHAR(20),@Correlativo BIGINT,@Numero VARCHAR(30);
        EXEC dbo.USP_SEG_SERIE_TOMAR_SIGUIENTE 'GUIA_SALIDA',@Serie OUTPUT,@Correlativo OUTPUT,@Numero OUTPUT;
        SET @NumeroGuia=CONCAT(@Serie,'-',@Numero);

        INSERT INTO dbo.GuiasInternas(NumeroGuia,Origen,IdOrdenCompraInterna,IdCliente,IdAlmacen,FechaEmision,RucEmisor,EmpresaEmisora,
            RucDestino,EmpresaDestino,UsuarioEmisor,UsuarioAutorizador,Observacion,MotivoEmisionManual,Estado)
        SELECT @NumeroGuia,'Manual',NULL,@IdCliente,@IdAlmacen,@FechaEmision,ISNULL(E.Ruc,''),ISNULL(E.Nombre,''),ISNULL(C.NumeroDocumento,''),ISNULL(C.NombreRazonSocial,'No especificado'),
               @UsuarioEmisor,@UsuarioAutorizador,ISNULL(@Observacion,''),@MotivoEmisionManual,'Emitida'
        FROM (SELECT 1 X) B OUTER APPLY(SELECT TOP(1) Ruc,Nombre FROM dbo.Empresas WHERE Estado=1 ORDER BY EsPredeterminada DESC,IdEmpresa) E
        LEFT JOIN dbo.Clientes C ON C.IdCliente=@IdCliente;
        DECLARE @IdGuia INT=SCOPE_IDENTITY();

        INSERT INTO dbo.GuiaInternaDetalle(IdGuiaInterna,IdOrdenCompraInternaDetalle,IdProducto,CodigoProducto,NombreProducto,
            IdUnidadMedida,NombreUnidad,CantidadRequerida,CantidadDespachada,StockAnterior,PrecioUnitario,Observacion)
        SELECT @IdGuia,NULL,P.IdProducto,P.Codigo,P.NombreProducto,P.IdUnidadMedida,U.NombreUnidad,D.CantidadDespachar,
               D.CantidadDespachar,S.StockActual,0,ISNULL(D.Observacion,'')
        FROM @Detalles D INNER JOIN dbo.Productos P ON P.IdProducto=D.IdProducto
        INNER JOIN dbo.UnidadesMedida U ON U.IdUnidadMedida=P.IdUnidadMedida
        INNER JOIN dbo.StockProductosAlmacen S ON S.IdProducto=D.IdProducto AND S.IdAlmacen=@IdAlmacen;

        DECLARE @IdProducto INT,@Cantidad DECIMAL(18,2),@Anterior DECIMAL(18,2);
        DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT D.IdProducto,D.CantidadDespachar,S.StockActual FROM @Detalles D
            INNER JOIN dbo.StockProductosAlmacen S ON S.IdProducto=D.IdProducto AND S.IdAlmacen=@IdAlmacen;
        OPEN c; FETCH NEXT FROM c INTO @IdProducto,@Cantidad,@Anterior;
        WHILE @@FETCH_STATUS=0
        BEGIN
            UPDATE dbo.StockProductosAlmacen SET StockActual=StockActual-@Cantidad,FechaActualizacion=GETDATE() WHERE IdProducto=@IdProducto AND IdAlmacen=@IdAlmacen;
            UPDATE dbo.StockProductos SET StockActual=StockActual-@Cantidad,FechaActualizacion=GETDATE() WHERE IdProducto=@IdProducto;
            INSERT dbo.KardexProductos(TipoMovimiento,IdIngresoManualStock,IdGuiaInterna,IdProducto,IdAlmacen,StockAnterior,Cantidad,StockResultante,UsuarioResponsable,FechaMovimiento,Observacion)
            VALUES('GUIA_INTERNA_MANUAL',NULL,@IdGuia,@IdProducto,@IdAlmacen,@Anterior,@Cantidad,@Anterior-@Cantidad,@UsuarioEmisor,GETDATE(),CONCAT('Salida manual ',@NumeroGuia,': ',@MotivoEmisionManual));
            FETCH NEXT FROM c INTO @IdProducto,@Cantidad,@Anterior;
        END
        CLOSE c; DEALLOCATE c; COMMIT;
        SET @Mensaje=CONCAT('Guía interna manual ',@NumeroGuia,' emitida correctamente.');
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local','c')>=0 CLOSE c; IF CURSOR_STATUS('local','c')>=-1 DEALLOCATE c;
        IF @@TRANCOUNT>0 ROLLBACK; SET @Mensaje=ERROR_MESSAGE(); SET @NumeroGuia='';
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_ANULAR
    @IdGuiaInterna INT,@Usuario VARCHAR(80),@Motivo VARCHAR(500),@Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; SET @Motivo=LTRIM(RTRIM(ISNULL(@Motivo,'')));
    IF @Motivo='' BEGIN SET @Mensaje='Debe ingresar el motivo de anulación.'; RETURN; END;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Estado VARCHAR(20),@IdAlmacen INT,@IdOci INT,@NumeroGuia VARCHAR(30);
        SELECT @Estado=Estado,@IdAlmacen=IdAlmacen,@IdOci=IdOrdenCompraInterna,@NumeroGuia=NumeroGuia
        FROM dbo.GuiasInternas WITH(UPDLOCK,HOLDLOCK) WHERE IdGuiaInterna=@IdGuiaInterna;
        IF @Estado IS NULL THROW 51200,'No se encontró la guía interna.',1;
        IF @Estado='Anulada' THROW 51201,'La guía interna ya se encuentra anulada.',1;

        DECLARE @IdProducto INT,@Cantidad DECIMAL(18,2),@IdDetalleOci INT,@Anterior DECIMAL(18,2);
        DECLARE c CURSOR LOCAL FAST_FORWARD FOR
            SELECT D.IdProducto,D.CantidadDespachada,D.IdOrdenCompraInternaDetalle,S.StockActual
            FROM dbo.GuiaInternaDetalle D
            INNER JOIN dbo.StockProductosAlmacen S WITH(UPDLOCK,HOLDLOCK) ON S.IdProducto=D.IdProducto AND S.IdAlmacen=@IdAlmacen
            WHERE D.IdGuiaInterna=@IdGuiaInterna;
        OPEN c; FETCH NEXT FROM c INTO @IdProducto,@Cantidad,@IdDetalleOci,@Anterior;
        WHILE @@FETCH_STATUS=0
        BEGIN
            UPDATE dbo.StockProductosAlmacen SET StockActual=StockActual+@Cantidad,FechaActualizacion=GETDATE() WHERE IdProducto=@IdProducto AND IdAlmacen=@IdAlmacen;
            UPDATE dbo.StockProductos SET StockActual=StockActual+@Cantidad,FechaActualizacion=GETDATE() WHERE IdProducto=@IdProducto;
            IF @IdDetalleOci IS NOT NULL UPDATE dbo.OrdenCompraInternaDetalle SET CantidadDespachada=CantidadDespachada-@Cantidad WHERE IdOrdenCompraInternaDetalle=@IdDetalleOci;
            INSERT dbo.KardexProductos(TipoMovimiento,IdIngresoManualStock,IdGuiaInterna,IdProducto,IdAlmacen,StockAnterior,Cantidad,StockResultante,UsuarioResponsable,FechaMovimiento,Observacion)
            VALUES('ANULACION_GUIA_INTERNA',NULL,@IdGuiaInterna,@IdProducto,@IdAlmacen,@Anterior,@Cantidad,@Anterior+@Cantidad,@Usuario,GETDATE(),CONCAT('Anulación de ',@NumeroGuia,': ',@Motivo));
            FETCH NEXT FROM c INTO @IdProducto,@Cantidad,@IdDetalleOci,@Anterior;
        END
        CLOSE c; DEALLOCATE c;

        UPDATE dbo.GuiasInternas SET Estado='Anulada',UsuarioAnulacion=@Usuario,FechaAnulacion=GETDATE(),MotivoAnulacion=@Motivo WHERE IdGuiaInterna=@IdGuiaInterna;
        IF @IdOci IS NOT NULL
            UPDATE dbo.OrdenesCompraInterna SET
                TieneGuiaSalida=CASE WHEN EXISTS(SELECT 1 FROM dbo.GuiasInternas WHERE IdOrdenCompraInterna=@IdOci AND Estado='Emitida') THEN 1 ELSE 0 END,
                Estado=CASE
                    WHEN Estado='Anulado' THEN 'Anulado'
                    WHEN NOT EXISTS(SELECT 1 FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOci AND CantidadDespachada<Cantidad) THEN 'Entregado'
                    WHEN EXISTS(SELECT 1 FROM dbo.OrdenCompraInternaDetalle WHERE IdOrdenCompraInterna=@IdOci AND CantidadDespachada>0) THEN 'Parcial'
                    WHEN TieneOrdenTrabajo=1 THEN 'En proceso'
                    ELSE 'Emitida'
                    END
            WHERE IdOrdenCompraInterna=@IdOci;
        COMMIT; SET @Mensaje='Guía interna anulada correctamente. El stock fue restituido.';
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local','c')>=0 CLOSE c; IF CURSOR_STATUS('local','c')>=-1 DEALLOCATE c;
        IF @@TRANCOUNT>0 ROLLBACK; SET @Mensaje=ERROR_MESSAGE();
    END CATCH;
END;
GO

/* GUIAS INTERNAS DE SALIDA */
IF OBJECT_ID('dbo.GuiasInternas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuiasInternas
    (
        IdGuiaInterna INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GuiasInternas PRIMARY KEY,
        NumeroGuia VARCHAR(30) NOT NULL,
        IdOrdenCompraInterna INT NOT NULL,
        IdAlmacen INT NOT NULL,
        FechaEmision DATE NOT NULL,
        RucEmisor VARCHAR(20) NOT NULL,
        EmpresaEmisora VARCHAR(250) NOT NULL,
        RucDestino VARCHAR(20) NOT NULL,
        EmpresaDestino VARCHAR(250) NOT NULL,
        UsuarioEmisor VARCHAR(80) NOT NULL,
        UsuarioAutorizador VARCHAR(80) NOT NULL,
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_GuiaInterna_Observacion DEFAULT(''),
        Estado VARCHAR(20) NOT NULL CONSTRAINT DF_GuiaInterna_Estado DEFAULT('Emitida'),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_GuiaInterna_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT UQ_GuiasInternas_Numero UNIQUE (NumeroGuia),
        CONSTRAINT FK_GuiasInternas_OCI FOREIGN KEY (IdOrdenCompraInterna) REFERENCES dbo.OrdenesCompraInterna(IdOrdenCompraInterna),
        CONSTRAINT FK_GuiasInternas_Almacen FOREIGN KEY (IdAlmacen) REFERENCES dbo.Almacenes(IdAlmacen)
    );
END;
GO

IF OBJECT_ID('dbo.GuiaInternaDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuiaInternaDetalle
    (
        IdGuiaInternaDetalle INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GuiaInternaDetalle PRIMARY KEY,
        IdGuiaInterna INT NOT NULL,
        IdOrdenCompraInternaDetalle INT NOT NULL,
        IdProducto INT NOT NULL,
        CodigoProducto VARCHAR(100) NOT NULL,
        NombreProducto VARCHAR(500) NOT NULL,
        IdUnidadMedida INT NOT NULL,
        NombreUnidad VARCHAR(100) NOT NULL,
        CantidadRequerida DECIMAL(18,2) NOT NULL,
        CantidadDespachada DECIMAL(18,2) NOT NULL,
        StockAnterior DECIMAL(18,2) NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_GuiaInternaDetalle_Observacion DEFAULT(''),
        CONSTRAINT FK_GuiaInternaDetalle_Guia FOREIGN KEY (IdGuiaInterna) REFERENCES dbo.GuiasInternas(IdGuiaInterna),
        CONSTRAINT FK_GuiaInternaDetalle_OCIDetalle FOREIGN KEY (IdOrdenCompraInternaDetalle) REFERENCES dbo.OrdenCompraInternaDetalle(IdOrdenCompraInternaDetalle),
        CONSTRAINT FK_GuiaInternaDetalle_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos(IdProducto),
        CONSTRAINT FK_GuiaInternaDetalle_Unidad FOREIGN KEY (IdUnidadMedida) REFERENCES dbo.UnidadesMedida(IdUnidadMedida)
    );
END;
GO

IF OBJECT_ID('dbo.SerieGuiaInterna', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SerieGuiaInterna
    (
        Serie VARCHAR(10) NOT NULL CONSTRAINT PK_SerieGuiaInterna PRIMARY KEY,
        UltimoNumero INT NOT NULL
    );
    INSERT INTO dbo.SerieGuiaInterna (Serie, UltimoNumero) VALUES ('GI01', 0);
END;
GO

IF COL_LENGTH('dbo.KardexProductos', 'IdGuiaInterna') IS NULL
    ALTER TABLE dbo.KardexProductos ADD IdGuiaInterna INT NULL;
GO

IF TYPE_ID('dbo.GuiaInternaDetalleType') IS NULL
    EXEC('CREATE TYPE dbo.GuiaInternaDetalleType AS TABLE
    (
        IdOrdenCompraInternaDetalle INT NOT NULL,
        CantidadDespachar DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NULL
    )');
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_PREPARAR
    @IdOrdenCompraInterna INT,
    @IdAlmacen INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @IdAlmacen IS NULL
        SELECT TOP (1) @IdAlmacen = IdAlmacen FROM dbo.Almacenes WHERE Estado = 1 ORDER BY IdAlmacen;

    SELECT
        O.IdOrdenCompraInterna, O.NumeroOci, O.OrdenCompraCliente,
        A.IdAlmacen, A.NombreAlmacen,
        ISNULL(E.Ruc, '') AS RucEmisor,
        ISNULL(E.Nombre, '') AS EmpresaEmisora,
        ISNULL(C.NumeroDocumento, '') AS RucDestino,
        O.NombreCliente AS EmpresaDestino
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN dbo.Almacenes A ON A.IdAlmacen = @IdAlmacen AND A.Estado = 1
    INNER JOIN dbo.Clientes C ON C.IdCliente = O.IdCliente
    OUTER APPLY
    (
        SELECT TOP (1) Ruc, Nombre
        FROM dbo.Empresas
        WHERE Estado = 1
        ORDER BY EsPredeterminada DESC, IdEmpresa
    ) E
    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna
      AND O.Estado <> 'Anulado';

    SELECT
        D.IdOrdenCompraInternaDetalle, D.IdProducto, D.CodigoProducto, D.NombreProducto,
        P.IdUnidadMedida, UM.NombreUnidad,
        D.Cantidad AS CantidadRequerida,
        D.CantidadDespachada AS CantidadEntregada,
        D.Cantidad - D.CantidadDespachada AS CantidadPendiente,
        CAST(ISNULL(S.StockActual, 0) AS DECIMAL(18,2)) AS StockActual,
        D.PrecioUnitario,
        CAST(CASE
            WHEN ISNULL(S.StockActual, 0) <= 0 THEN 0
            WHEN S.StockActual < D.Cantidad - D.CantidadDespachada THEN S.StockActual
            ELSE D.Cantidad - D.CantidadDespachada
        END AS DECIMAL(18,2)) AS CantidadSugerida,
        D.Observacion
    FROM dbo.OrdenCompraInternaDetalle D
    INNER JOIN dbo.Productos P ON P.IdProducto = D.IdProducto
    INNER JOIN dbo.UnidadesMedida UM ON UM.IdUnidadMedida = P.IdUnidadMedida
    LEFT JOIN dbo.StockProductosAlmacen S ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen
    WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
      AND D.Cantidad > D.CantidadDespachada
    ORDER BY D.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_EMITIR
    @IdOrdenCompraInterna INT,
    @IdAlmacen INT,
    @FechaEmision DATE,
    @UsuarioEmisor VARCHAR(80),
    @UsuarioAutorizador VARCHAR(80),
    @Observacion VARCHAR(500),
    @Detalles dbo.GuiaInternaDetalleType READONLY,
    @NumeroGuia VARCHAR(30) OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @NumeroGuia = '';

    IF NOT EXISTS (SELECT 1 FROM @Detalles WHERE CantidadDespachar > 0)
    BEGIN
        SET @Mensaje = 'Debe indicar al menos un producto para despachar.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.OrdenesCompraInterna WITH (UPDLOCK, HOLDLOCK)
            WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna AND Estado <> 'Anulado'
        )
            THROW 51000, 'La OCI no existe o se encuentra anulada.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM @Detalles T
            LEFT JOIN dbo.OrdenCompraInternaDetalle D WITH (UPDLOCK, HOLDLOCK)
                ON D.IdOrdenCompraInternaDetalle = T.IdOrdenCompraInternaDetalle
               AND D.IdOrdenCompraInterna = @IdOrdenCompraInterna
            WHERE D.IdOrdenCompraInternaDetalle IS NULL
               OR T.CantidadDespachar <= 0
        )
            THROW 51001, 'Uno o más detalles de la guía no son válidos.', 1;

        DECLARE @CodigoProductoInvalido VARCHAR(100), @CantidadMaxima DECIMAL(18,2), @MensajeValidacion VARCHAR(500);
        SELECT TOP (1)
            @CodigoProductoInvalido = D.CodigoProducto,
            @CantidadMaxima = CASE
                WHEN D.Cantidad - D.CantidadDespachada < ISNULL(S.StockActual, 0)
                    THEN D.Cantidad - D.CantidadDespachada
                ELSE ISNULL(S.StockActual, 0)
            END
        FROM @Detalles T
        INNER JOIN dbo.OrdenCompraInternaDetalle D WITH (UPDLOCK, HOLDLOCK)
            ON D.IdOrdenCompraInternaDetalle = T.IdOrdenCompraInternaDetalle
           AND D.IdOrdenCompraInterna = @IdOrdenCompraInterna
        LEFT JOIN dbo.StockProductosAlmacen S WITH (UPDLOCK, HOLDLOCK)
            ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen
        WHERE T.CantidadDespachar > CASE
            WHEN D.Cantidad - D.CantidadDespachada < ISNULL(S.StockActual, 0)
                THEN D.Cantidad - D.CantidadDespachada
            ELSE ISNULL(S.StockActual, 0)
        END
        ORDER BY T.IdOrdenCompraInternaDetalle;

        IF @CodigoProductoInvalido IS NOT NULL
        BEGIN
            SET @MensajeValidacion = CONCAT(
                'La cantidad máxima permitida para ', @CodigoProductoInvalido,
                ' es ', CONVERT(VARCHAR(30), CAST(@CantidadMaxima AS DECIMAL(18,2))), '.');
            THROW 51001, @MensajeValidacion, 1;
        END;

        DECLARE @SerieGuia VARCHAR(20), @Correlativo BIGINT, @NumeroCorrelativo VARCHAR(30);
        EXEC dbo.USP_SEG_SERIE_TOMAR_SIGUIENTE
            @CodigoTipoDocumento='GUIA_SALIDA', @Serie=@SerieGuia OUTPUT,
            @Correlativo=@Correlativo OUTPUT, @Numero=@NumeroCorrelativo OUTPUT;
        SET @NumeroGuia = CONCAT(@SerieGuia, '-', @NumeroCorrelativo);

        INSERT INTO dbo.GuiasInternas
        (
            NumeroGuia, IdOrdenCompraInterna, IdAlmacen, FechaEmision,
            RucEmisor, EmpresaEmisora, RucDestino, EmpresaDestino,
            UsuarioEmisor, UsuarioAutorizador, Observacion, Estado
        )
        SELECT
            @NumeroGuia, O.IdOrdenCompraInterna, @IdAlmacen, @FechaEmision,
            ISNULL(E.Ruc, ''), ISNULL(E.Nombre, ''), ISNULL(C.NumeroDocumento, ''), O.NombreCliente,
            @UsuarioEmisor, @UsuarioAutorizador, ISNULL(@Observacion, ''), 'Emitida'
        FROM dbo.OrdenesCompraInterna O
        INNER JOIN dbo.Clientes C ON C.IdCliente = O.IdCliente
        OUTER APPLY
        (
            SELECT TOP (1) Ruc, Nombre FROM dbo.Empresas
            WHERE Estado = 1 ORDER BY EsPredeterminada DESC, IdEmpresa
        ) E
        WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

        DECLARE @IdGuiaInterna INT = SCOPE_IDENTITY();

        INSERT INTO dbo.GuiaInternaDetalle
        (
            IdGuiaInterna, IdOrdenCompraInternaDetalle, IdProducto, CodigoProducto, NombreProducto,
            IdUnidadMedida, NombreUnidad, CantidadRequerida, CantidadDespachada,
            StockAnterior, PrecioUnitario, Observacion
        )
        SELECT
            @IdGuiaInterna, D.IdOrdenCompraInternaDetalle, D.IdProducto, D.CodigoProducto, D.NombreProducto,
            P.IdUnidadMedida, U.NombreUnidad, D.Cantidad, T.CantidadDespachar,
            S.StockActual, D.PrecioUnitario, ISNULL(T.Observacion, '')
        FROM @Detalles T
        INNER JOIN dbo.OrdenCompraInternaDetalle D ON D.IdOrdenCompraInternaDetalle = T.IdOrdenCompraInternaDetalle
        INNER JOIN dbo.Productos P ON P.IdProducto = D.IdProducto
        INNER JOIN dbo.UnidadesMedida U ON U.IdUnidadMedida = P.IdUnidadMedida
        INNER JOIN dbo.StockProductosAlmacen S ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen;

        DECLARE @IdDetalle INT, @IdProducto INT, @Cantidad DECIMAL(18,2), @StockAnterior DECIMAL(18,2);
        DECLARE detalle_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT T.IdOrdenCompraInternaDetalle, D.IdProducto, T.CantidadDespachar, S.StockActual
            FROM @Detalles T
            INNER JOIN dbo.OrdenCompraInternaDetalle D ON D.IdOrdenCompraInternaDetalle = T.IdOrdenCompraInternaDetalle
            INNER JOIN dbo.StockProductosAlmacen S ON S.IdProducto = D.IdProducto AND S.IdAlmacen = @IdAlmacen;
        OPEN detalle_cursor;
        FETCH NEXT FROM detalle_cursor INTO @IdDetalle, @IdProducto, @Cantidad, @StockAnterior;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            UPDATE dbo.StockProductosAlmacen
            SET StockActual = StockActual - @Cantidad, FechaActualizacion = GETDATE()
            WHERE IdProducto = @IdProducto AND IdAlmacen = @IdAlmacen;

            UPDATE dbo.StockProductos
            SET StockActual = StockActual - @Cantidad, FechaActualizacion = GETDATE()
            WHERE IdProducto = @IdProducto;

            UPDATE dbo.OrdenCompraInternaDetalle
            SET CantidadDespachada = CantidadDespachada + @Cantidad
            WHERE IdOrdenCompraInternaDetalle = @IdDetalle;

            INSERT INTO dbo.KardexProductos
            (
                TipoMovimiento, IdIngresoManualStock, IdGuiaInterna, IdProducto, IdAlmacen,
                StockAnterior, Cantidad, StockResultante, UsuarioResponsable, FechaMovimiento, Observacion
            )
            VALUES
            (
                'GUIA_INTERNA_SALIDA', NULL, @IdGuiaInterna, @IdProducto, @IdAlmacen,
                @StockAnterior, @Cantidad, @StockAnterior - @Cantidad, @UsuarioEmisor, GETDATE(),
                CONCAT('Salida por ', @NumeroGuia, ' - OCI ', @IdOrdenCompraInterna)
            );

            FETCH NEXT FROM detalle_cursor INTO @IdDetalle, @IdProducto, @Cantidad, @StockAnterior;
        END;
        CLOSE detalle_cursor;
        DEALLOCATE detalle_cursor;

        UPDATE dbo.OrdenesCompraInterna
        SET TieneGuiaSalida = 1,
            Estado = CASE WHEN EXISTS
            (
                SELECT 1 FROM dbo.OrdenCompraInternaDetalle
                WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna AND CantidadDespachada < Cantidad
            ) THEN 'Parcial' ELSE 'Entregado' END
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna;

        COMMIT TRANSACTION;
        SET @Mensaje = CONCAT('Guia interna ', @NumeroGuia, ' emitida correctamente.');
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'detalle_cursor') >= 0 CLOSE detalle_cursor;
        IF CURSOR_STATUS('local', 'detalle_cursor') >= -1 DEALLOCATE detalle_cursor;
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
        SET @NumeroGuia = '';
    END CATCH;
END;
GO

IF OBJECT_ID('dbo.OrdenCompraInternaDetalle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrdenCompraInternaDetalle
    (
        IdOrdenCompraInternaDetalle INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenCompraInternaDetalle PRIMARY KEY,
        IdOrdenCompraInterna INT NOT NULL,
        IdProducto INT NOT NULL,
        CodigoProducto VARCHAR(100) NOT NULL,
        NombreProducto VARCHAR(500) NOT NULL,
        Cantidad DECIMAL(18,2) NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        Descuento DECIMAL(18,2) NOT NULL,
        Importe DECIMAL(18,2) NOT NULL,
        Observacion VARCHAR(500) NOT NULL CONSTRAINT DF_OCID_Observacion DEFAULT(''),
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_OCID_FechaRegistro DEFAULT(GETDATE()),
        CONSTRAINT FK_OCID_OCI FOREIGN KEY (IdOrdenCompraInterna)
            REFERENCES dbo.OrdenesCompraInterna(IdOrdenCompraInterna),
        CONSTRAINT FK_OCID_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Productos(IdProducto)
    );
END;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'TieneGuiaSalida') IS NULL
    ALTER TABLE dbo.OrdenesCompraInterna ADD TieneGuiaSalida BIT NOT NULL
        CONSTRAINT DF_OCI_TieneGuiaSalida DEFAULT(0) WITH VALUES;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'TieneOrdenTrabajo') IS NULL
    ALTER TABLE dbo.OrdenesCompraInterna ADD TieneOrdenTrabajo BIT NOT NULL
        CONSTRAINT DF_OCI_TieneOrdenTrabajo DEFAULT(0) WITH VALUES;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'UsuarioAnulacion') IS NULL
    ALTER TABLE dbo.OrdenesCompraInterna ADD UsuarioAnulacion VARCHAR(80) NULL;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'FechaAnulacion') IS NULL
    ALTER TABLE dbo.OrdenesCompraInterna ADD FechaAnulacion DATETIME NULL;
GO

IF COL_LENGTH('dbo.OrdenesCompraInterna', 'MotivoAnulacion') IS NULL
    ALTER TABLE dbo.OrdenesCompraInterna ADD MotivoAnulacion VARCHAR(500) NULL;
GO

UPDATE dbo.OrdenesCompraInterna
SET Estado = CASE
        WHEN Estado IN ('Anulada', 'Anulado') THEN 'Anulado'
        WHEN NOT EXISTS
        (
            SELECT 1 FROM dbo.OrdenCompraInternaDetalle D
            WHERE D.IdOrdenCompraInterna = OrdenesCompraInterna.IdOrdenCompraInterna
              AND D.CantidadDespachada < D.Cantidad
        ) THEN 'Entregado'
        WHEN EXISTS
        (
            SELECT 1 FROM dbo.OrdenCompraInternaDetalle D
            WHERE D.IdOrdenCompraInterna = OrdenesCompraInterna.IdOrdenCompraInterna
              AND D.CantidadDespachada > 0
        ) THEN 'Parcial'
        WHEN TieneOrdenTrabajo = 1 THEN 'En proceso'
        ELSE 'Emitida'
    END;
GO

UPDATE dbo.OrdenesCompraInterna
SET MotivoAnulacion = 'No registrado (anulacion anterior)'
WHERE Estado = 'Anulado'
  AND NULLIF(LTRIM(RTRIM(MotivoAnulacion)), '') IS NULL;
GO

CREATE OR ALTER TRIGGER dbo.TRG_OCI_ESTADO_ORDEN_TRABAJO
ON dbo.OrdenesCompraInterna
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(TieneOrdenTrabajo) RETURN;

    UPDATE O
    SET Estado = 'En proceso'
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN inserted I ON I.IdOrdenCompraInterna = O.IdOrdenCompraInterna
    INNER JOIN deleted D ON D.IdOrdenCompraInterna = I.IdOrdenCompraInterna
    WHERE D.TieneOrdenTrabajo = 0
      AND I.TieneOrdenTrabajo = 1
      AND O.Estado = 'Emitida';
END;
GO

IF COL_LENGTH('dbo.OrdenCompraInternaDetalle', 'CantidadDespachada') IS NULL
    ALTER TABLE dbo.OrdenCompraInternaDetalle ADD CantidadDespachada DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_OCID_CantidadDespachada DEFAULT(0) WITH VALUES;
GO

UPDATE P
SET Estado = CASE
    WHEN P.Estado = 'Anulado' THEN 'Anulado'
    WHEN P.TieneOrdenCompraInterna = 1
         OR EXISTS (SELECT 1 FROM dbo.OrdenesCompraInterna O WHERE O.IdProforma = P.IdProforma)
        THEN 'Registrado'
    ELSE 'Emitido'
END
FROM dbo.Proformas P;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_LISTAR
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        O.IdProforma,
        P.SerieNumero AS NumeroProforma,
        O.FechaEmision,
        O.OrdenCompraCliente,
        O.IdCliente,
        O.NombreCliente,
        O.Subtotal,
        O.Descuento,
        O.Igv,
        O.IgvPorcentaje,
        O.CondicionTributaria,
        O.Total,
        O.Estado,
        O.UsuarioGenerador,
        O.FechaRegistro,
        O.MotivoAnulacion,
        O.UsuarioAnulacion,
        O.FechaAnulacion,
        O.TieneGuiaSalida,
        O.TieneOrdenTrabajo,
        CAST(CASE WHEN O.Estado <> 'Anulado'
             AND NOT EXISTS
             (
                 SELECT 1
                 FROM dbo.OrdenTrabajo OT
                 WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                   AND OT.Estado IN ('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL')
             )
             AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            OUTER APPLY
            (
                SELECT SUM(OD.CantidadAplicada) CantidadAplicada
                FROM dbo.OrdenTrabajoDetalle OD
                JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
                  AND OT.Estado <> 'ANULADA'
                  AND OD.Estado <> 'ANULADO'
            ) PROD
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - ISNULL(PROD.CantidadAplicada, 0) > 0
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarOt,
        CAST(CASE WHEN O.Estado <> 'Anulado' AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - D.CantidadDespachada > 0
              AND ISNULL(S.StockActual, 0) > 0
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarGuiaSalida
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    ORDER BY O.FechaEmision DESC, O.IdOrdenCompraInterna DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_OBTENER
    @IdOrdenCompraInterna INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        O.IdOrdenCompraInterna,
        O.NumeroOci,
        O.IdProforma,
        P.SerieNumero AS NumeroProforma,
        O.FechaEmision,
        O.OrdenCompraCliente,
        O.IdCliente,
        O.NombreCliente,
        O.Subtotal,
        O.Descuento,
        O.Igv,
        O.IgvPorcentaje,
        O.CondicionTributaria,
        O.Total,
        O.Estado,
        O.UsuarioGenerador,
        O.FechaRegistro,
        O.MotivoAnulacion,
        O.UsuarioAnulacion,
        O.FechaAnulacion,
        O.TieneGuiaSalida,
        O.TieneOrdenTrabajo,
        CAST(CASE WHEN O.Estado <> 'Anulado'
             AND NOT EXISTS
             (
                 SELECT 1
                 FROM dbo.OrdenTrabajo OT
                 WHERE OT.IdOrdenCompraInterna = O.IdOrdenCompraInterna
                   AND OT.Estado IN ('PENDIENTE','EMITIDA','EN_PROCESO','PARCIAL')
             )
             AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            OUTER APPLY
            (
                SELECT SUM(OD.CantidadAplicada) CantidadAplicada
                FROM dbo.OrdenTrabajoDetalle OD
                JOIN dbo.OrdenTrabajo OT ON OT.IdOrdenTrabajo = OD.IdOrdenTrabajo
                WHERE OD.IdOrdenCompraInternaDetalle = D.IdOrdenCompraInternaDetalle
                  AND OT.Estado <> 'ANULADA'
                  AND OD.Estado <> 'ANULADO'
            ) PROD
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - ISNULL(PROD.CantidadAplicada, 0) > 0
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarOt,
        CAST(CASE WHEN O.Estado <> 'Anulado' AND EXISTS
        (
            SELECT 1
            FROM dbo.OrdenCompraInternaDetalle D
            LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
            WHERE D.IdOrdenCompraInterna = O.IdOrdenCompraInterna
              AND D.Cantidad - D.CantidadDespachada > 0
              AND ISNULL(S.StockActual, 0) > 0
        ) THEN 1 ELSE 0 END AS BIT) AS PuedeGenerarGuiaSalida
    FROM dbo.OrdenesCompraInterna O
    INNER JOIN dbo.Proformas P ON P.IdProforma = O.IdProforma
    WHERE O.IdOrdenCompraInterna = @IdOrdenCompraInterna;

    SELECT
        D.IdOrdenCompraInternaDetalle,
        D.IdOrdenCompraInterna,
        D.IdProducto,
        D.CodigoProducto,
        D.NombreProducto,
        D.Cantidad,
        CAST(ISNULL(S.StockActual, 0) AS DECIMAL(18,2)) AS StockActual,
        D.CantidadDespachada,
        D.PrecioUnitario,
        D.Descuento,
        D.Importe,
        D.Observacion
    FROM dbo.OrdenCompraInternaDetalle D
    LEFT JOIN dbo.StockProductos S ON S.IdProducto = D.IdProducto
    WHERE D.IdOrdenCompraInterna = @IdOrdenCompraInterna
    ORDER BY D.IdOrdenCompraInternaDetalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_ANULAR
    @IdOrdenCompraInterna INT,
    @MotivoAnulacion VARCHAR(500),
    @UsuarioAnulacion VARCHAR(80),
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @MotivoAnulacion = LTRIM(RTRIM(ISNULL(@MotivoAnulacion, '')));
    SET @UsuarioAnulacion = LTRIM(RTRIM(ISNULL(@UsuarioAnulacion, '')));

    IF @MotivoAnulacion = ''
    BEGIN
        SET @Mensaje = 'Debe ingresar el motivo de anulacion.';
        RETURN;
    END;

    IF @UsuarioAnulacion = ''
    BEGIN
        SET @Mensaje = 'No se pudo identificar al usuario que anula la OCI.';
        RETURN;
    END;

    UPDATE dbo.OrdenesCompraInterna WITH (UPDLOCK)
    SET Estado = 'Anulado',
        MotivoAnulacion = @MotivoAnulacion,
        UsuarioAnulacion = @UsuarioAnulacion,
        FechaAnulacion = GETDATE()
    WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna
      AND Estado <> 'Anulado'
      AND TieneOrdenTrabajo = 0;

    IF @@ROWCOUNT = 1
    BEGIN
        SET @Mensaje = 'OCI anulada correctamente.';
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraInterna WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna)
        SET @Mensaje = 'No se encontró la OCI seleccionada.';
    ELSE IF EXISTS
    (
        SELECT 1 FROM dbo.OrdenesCompraInterna
        WHERE IdOrdenCompraInterna = @IdOrdenCompraInterna AND Estado = 'Anulado'
    )
        SET @Mensaje = 'La OCI ya se encuentra anulada.';
    ELSE
        SET @Mensaje = 'No se puede anular la OCI porque tiene una Orden de Trabajo emitida.';
END;
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_OCI_GENERAR
    @IdProforma INT,
    @UsuarioGenerador VARCHAR(80),
    @IdGenerado INT OUTPUT,
    @NumeroOci VARCHAR(40) OUTPUT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @IdGenerado = 0;
    SET @NumeroOci = '';
    SET @Resultado = 0;

    IF NOT EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma)
    BEGIN
        SET @Mensaje = 'No se encontró la proforma seleccionada.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma AND Estado = 'Anulado')
    BEGIN
        SET @Mensaje = 'No se puede generar una OCI desde una proforma anulada.';
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.OrdenesCompraInterna WHERE IdProforma = @IdProforma)
       OR EXISTS (SELECT 1 FROM dbo.Proformas WHERE IdProforma = @IdProforma AND TieneOrdenCompraInterna = 1)
    BEGIN
        SET @Mensaje = 'La proforma ya tiene una orden de compra interna.';
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.OrdenesCompraInterna
        (
            NumeroOci, IdProforma, FechaEmision, OrdenCompraCliente, IdCliente,
            NombreCliente, Subtotal, Descuento, Igv, IgvPorcentaje, CondicionTributaria,
            Total, Estado, UsuarioGenerador
        )
        SELECT
            '', P.IdProforma, CAST(GETDATE() AS DATE), P.OrdenCompraCliente, P.IdCliente,
            C.NombreRazonSocial, P.Subtotal, P.Descuento, P.Igv, P.IgvPorcentaje, P.CondicionTributaria,
            P.Total, 'Emitida', @UsuarioGenerador
        FROM dbo.Proformas P
        INNER JOIN dbo.Clientes C ON C.IdCliente = P.IdCliente
        WHERE P.IdProforma = @IdProforma;

        SET @IdGenerado = SCOPE_IDENTITY();
        SET @NumeroOci = CONCAT('OCI-', RIGHT(CONCAT('000000', @IdGenerado), 6));

        UPDATE dbo.OrdenesCompraInterna
        SET NumeroOci = @NumeroOci
        WHERE IdOrdenCompraInterna = @IdGenerado;

        INSERT INTO dbo.OrdenCompraInternaDetalle
        (
            IdOrdenCompraInterna, IdProducto, CodigoProducto, NombreProducto,
            Cantidad, PrecioUnitario, Descuento, Importe, Observacion
        )
        SELECT
            @IdGenerado, D.IdProducto, PR.Codigo, PR.NombreProducto,
            D.Cantidad, D.PrecioUnitario, D.Descuento, D.Importe, D.Observacion
        FROM dbo.ProformaDetalle D
        INNER JOIN dbo.Productos PR ON PR.IdProducto = D.IdProducto
        WHERE D.IdProforma = @IdProforma;

        UPDATE dbo.Proformas
        SET TieneOrdenCompraInterna = 1,
            Estado = 'Registrado'
        WHERE IdProforma = @IdProforma;

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = CONCAT('Orden de compra interna ', @NumeroOci, ' generada correctamente.');
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH;
END;
GO
