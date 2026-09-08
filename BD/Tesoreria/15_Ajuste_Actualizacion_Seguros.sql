USE CorexProdDB;
GO
/* ETAPA 15.3.1 - Actualizacion persistente y transaccional de seguros. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.USP_TES_SEGURO_ACTUALIZAR
    @IdSeguro INT,
    @IdObligacionFinanciera INT,
    @IdEntidadAseguradora INT,
    @TipoTitular VARCHAR(20), @IdTitular INT = NULL, @NombreTitular VARCHAR(180),
    @IdEmpresaResponsable INT = NULL, @CodigoReferencia VARCHAR(80) = '', @NumeroContrato VARCHAR(80) = '',
    @Concepto VARCHAR(250), @Moneda VARCHAR(10), @MontoOriginal DECIMAL(18,2), @FechaInicio DATE,
    @Observacion VARCHAR(1000) = '', @TipoSeguro VARCHAR(80), @NumeroPoliza VARCHAR(100),
    @FechaInicioCobertura DATE, @FechaFinCobertura DATE, @Periodicidad VARCHAR(20), @Usuario VARCHAR(80),
    @Periodos dbo.TesSeguroPeriodoType READONLY,
    @Resultado BIT OUTPUT, @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @Resultado=0; SET @Mensaje='';
    SET @TipoTitular=UPPER(LTRIM(RTRIM(ISNULL(@TipoTitular,'')))); SET @NombreTitular=LTRIM(RTRIM(ISNULL(@NombreTitular,'')));
    SET @Concepto=LTRIM(RTRIM(ISNULL(@Concepto,''))); SET @Moneda=UPPER(LTRIM(RTRIM(ISNULL(@Moneda,''))));
    SET @TipoSeguro=LTRIM(RTRIM(ISNULL(@TipoSeguro,''))); SET @NumeroPoliza=LTRIM(RTRIM(ISNULL(@NumeroPoliza,'')));
    SET @Periodicidad=UPPER(LTRIM(RTRIM(ISNULL(@Periodicidad,'')))); SET @Usuario=LTRIM(RTRIM(ISNULL(@Usuario,'Sistema')));
    IF @IdSeguro<=0 OR @IdObligacionFinanciera<=0 BEGIN SET @Mensaje='Debe indicar el seguro y la obligacion financiera.'; RETURN; END;
    IF @TipoTitular NOT IN ('EMPRESA','PERSONA','OTRO') OR @NombreTitular='' OR @Concepto='' OR @TipoSeguro='' OR @NumeroPoliza='' BEGIN SET @Mensaje='Los datos obligatorios del seguro no son validos.'; RETURN; END;
    IF @Moneda NOT IN ('PEN','USD') OR @MontoOriginal<=0 BEGIN SET @Mensaje='La moneda o el monto original no son validos.'; RETURN; END;
    IF @FechaInicio IS NULL OR @FechaInicioCobertura IS NULL OR @FechaFinCobertura IS NULL OR @FechaFinCobertura<@FechaInicioCobertura BEGIN SET @Mensaje='Las fechas de cobertura no son validas.'; RETURN; END;
    IF @Periodicidad NOT IN ('UNICO','MENSUAL','BIMESTRAL','TRIMESTRAL','SEMESTRAL','ANUAL') BEGIN SET @Mensaje='La periodicidad no es valida.'; RETURN; END;
    IF NOT EXISTS(SELECT 1 FROM dbo.TesEntidadesFinancieras WHERE IdEntidadFinanciera=@IdEntidadAseguradora AND Activo=1 AND TipoEntidad='ASEGURADORA') BEGIN SET @Mensaje='La entidad seleccionada debe ser una aseguradora activa.'; RETURN; END;
    IF NOT EXISTS(SELECT 1 FROM @Periodos) OR EXISTS(SELECT 1 FROM @Periodos WHERE Importe<=0 OR FechaVencimiento<@FechaInicio) BEGIN SET @Mensaje='Los periodos del seguro no son validos.'; RETURN; END;
    IF EXISTS(SELECT 1 FROM @Periodos WHERE EsCuota=0) AND (SELECT COUNT(1) FROM @Periodos)<>1 BEGIN SET @Mensaje='Un periodo unico no puede coexistir con otros periodos.'; RETURN; END;
    IF EXISTS(SELECT 1 FROM @Periodos WHERE EsCuota=0 AND (NumeroCuota<>1 OR TotalCuotas<>1)) BEGIN SET @Mensaje='El periodo unico debe usar 1/1.'; RETURN; END;
    IF EXISTS(SELECT 1 FROM @Periodos WHERE EsCuota=1 AND (NumeroCuota<=0 OR TotalCuotas<=1 OR NumeroCuota>TotalCuotas)) BEGIN SET @Mensaje='La numeracion de cuotas no es valida.'; RETURN; END;
    IF EXISTS(SELECT NumeroCuota FROM @Periodos GROUP BY NumeroCuota HAVING COUNT(*)>1) OR ROUND((SELECT SUM(Importe) FROM @Periodos),2)<>ROUND(@MontoOriginal,2) BEGIN SET @Mensaje='El cronograma no coincide con el monto original.'; RETURN; END;
    BEGIN TRY
      BEGIN TRANSACTION;
      DECLARE @Estado VARCHAR(30),@TienePagos BIT=0;
      SELECT @Estado=O.Estado FROM dbo.TesSeguros S INNER JOIN dbo.TesObligacionesFinancieras O WITH(UPDLOCK,HOLDLOCK) ON O.IdObligacionFinanciera=S.IdObligacionFinanciera WHERE S.IdSeguro=@IdSeguro AND S.IdObligacionFinanciera=@IdObligacionFinanciera;
      IF @Estado IS NULL BEGIN ROLLBACK; SET @Mensaje='La relacion entre seguro y obligacion no existe.'; RETURN; END;
      IF @Estado='ANULADA' BEGIN ROLLBACK; SET @Mensaje='No se puede editar un seguro anulado.'; RETURN; END;
      IF EXISTS(SELECT 1 FROM dbo.TesObligacionFinancieraPagos P INNER JOIN dbo.TesObligacionFinancieraCuotas Q ON Q.IdCuotaObligacionFinanciera=P.IdCuotaObligacionFinanciera WHERE Q.IdObligacionFinanciera=@IdObligacionFinanciera AND P.Estado='ACTIVO') SET @TienePagos=1;
      IF @TienePagos=1 BEGIN ROLLBACK; SET @Mensaje='No se puede modificar el seguro mientras tenga pagos activos.'; RETURN; END;
      IF EXISTS(SELECT 1 FROM dbo.TesSeguros S INNER JOIN dbo.TesObligacionesFinancieras O ON O.IdObligacionFinanciera=S.IdObligacionFinanciera WHERE O.IdEntidadFinanciera=@IdEntidadAseguradora AND S.NumeroPoliza=@NumeroPoliza AND O.Estado<>'ANULADA' AND S.IdSeguro<>@IdSeguro) BEGIN ROLLBACK; SET @Mensaje='Ya existe una poliza activa con ese numero para la aseguradora.'; RETURN; END;
      UPDATE dbo.TesObligacionesFinancieras SET IdEntidadFinanciera=@IdEntidadAseguradora,TipoTitular=@TipoTitular,IdTitular=@IdTitular,NombreTitular=@NombreTitular,IdEmpresaResponsable=@IdEmpresaResponsable,CodigoReferencia=@CodigoReferencia,NumeroContrato=@NumeroContrato,Concepto=@Concepto,Moneda=@Moneda,MontoOriginal=@MontoOriginal,FechaInicio=@FechaInicio,TotalCuotas=(SELECT MAX(TotalCuotas) FROM @Periodos),Observacion=@Observacion,UsuarioModificacion=@Usuario,FechaModificacion=GETDATE() WHERE IdObligacionFinanciera=@IdObligacionFinanciera;
      UPDATE dbo.TesSeguros SET TipoSeguro=@TipoSeguro,NumeroPoliza=@NumeroPoliza,FechaInicioCobertura=@FechaInicioCobertura,FechaFinCobertura=@FechaFinCobertura,Periodicidad=@Periodicidad WHERE IdSeguro=@IdSeguro;
      UPDATE dbo.TesObligacionFinancieraCuotas SET Estado='ANULADA' WHERE IdObligacionFinanciera=@IdObligacionFinanciera AND Estado<>'ANULADA';
      INSERT dbo.TesObligacionFinancieraCuotas(IdObligacionFinanciera,NumeroCuota,TotalCuotas,EsCuota,FechaVencimiento,Importe,TotalPagado,SaldoPendiente,Estado,Observacion) SELECT @IdObligacionFinanciera,NumeroCuota,TotalCuotas,EsCuota,FechaVencimiento,Importe,0,Importe,'PENDIENTE',ISNULL(Observacion,'') FROM @Periodos;
      INSERT dbo.TesObligacionFinancieraHistorial(IdObligacionFinanciera,Usuario,Accion,EstadoAnterior,EstadoNuevo,Descripcion) VALUES(@IdObligacionFinanciera,@Usuario,'ACTUALIZACION_SEGURO',@Estado,@Estado,CONCAT('Seguro actualizado. Poliza: ',@NumeroPoliza,'.'));
      COMMIT; SET @Resultado=1; SET @Mensaje='Seguro actualizado correctamente.';
    END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; SET @Mensaje=ERROR_MESSAGE(); END CATCH
END;
GO
