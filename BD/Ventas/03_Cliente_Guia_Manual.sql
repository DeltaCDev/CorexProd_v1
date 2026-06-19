SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.GuiasInternas', 'IdCliente') IS NULL
    ALTER TABLE dbo.GuiasInternas ADD IdCliente INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuiasInternas_Cliente')
    ALTER TABLE dbo.GuiasInternas ADD CONSTRAINT FK_GuiasInternas_Cliente
        FOREIGN KEY (IdCliente) REFERENCES dbo.Clientes(IdCliente);
GO

CREATE OR ALTER PROCEDURE dbo.USP_VEN_GUIA_INTERNA_LISTAR
    @FechaDesde DATE=NULL, @FechaHasta DATE=NULL, @IdAlmacen INT=NULL,
    @Estado VARCHAR(20)=NULL, @Origen VARCHAR(20)=NULL, @Texto VARCHAR(100)=NULL
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
           OR G.EmpresaDestino LIKE '%'+@Texto+'%' OR G.RucDestino LIKE '%'+@Texto+'%' OR G.MotivoEmisionManual LIKE '%'+@Texto+'%')
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
           D.CantidadRequerida,ISNULL(OD.CantidadDespachada,D.CantidadDespachada) CantidadEntregada,
           CASE WHEN OD.IdOrdenCompraInternaDetalle IS NULL THEN CAST(0 AS DECIMAL(18,2))
                WHEN OD.Cantidad>OD.CantidadDespachada THEN OD.Cantidad-OD.CantidadDespachada
                ELSE CAST(0 AS DECIMAL(18,2)) END CantidadPendiente,
           D.StockAnterior StockActual,D.PrecioUnitario,D.CantidadDespachada CantidadSugerida,D.Observacion
    FROM dbo.GuiaInternaDetalle D
    LEFT JOIN dbo.OrdenCompraInternaDetalle OD ON OD.IdOrdenCompraInternaDetalle=D.IdOrdenCompraInternaDetalle
    WHERE D.IdGuiaInterna=@IdGuiaInterna ORDER BY D.IdGuiaInternaDetalle;
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
    IF @IdCliente IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Clientes WHERE IdCliente=@IdCliente AND Estado=1)
        BEGIN SET @Mensaje='El cliente seleccionado no existe o está inactivo.'; RETURN; END;
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
        SELECT @NumeroGuia,'Manual',NULL,@IdCliente,@IdAlmacen,@FechaEmision,ISNULL(E.Ruc,''),ISNULL(E.Nombre,''),
               ISNULL(C.NumeroDocumento,''),ISNULL(C.NombreRazonSocial,'No especificado'),
               @UsuarioEmisor,@UsuarioAutorizador,ISNULL(@Observacion,''),@MotivoEmisionManual,'Emitida'
        FROM (SELECT 1 X) B
        OUTER APPLY(SELECT TOP(1) Ruc,Nombre FROM dbo.Empresas WHERE Estado=1 ORDER BY EsPredeterminada DESC,IdEmpresa) E
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
