using CorexProd.Entidad.Entidades;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CorexProd.Datos.Datos
{
    public class DestajoPagosDatos
    {
        public List<AreaOperativa> ListarAreas()
        {
            List<AreaOperativa> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_AREA_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new AreaOperativa
                {
                    IdAreaOperativa = Entero(dr["IdAreaOperativa"]),
                    NombreArea = Texto(dr["NombreArea"]),
                    Descripcion = Texto(dr["Descripcion"]),
                    Estado = Booleano(dr["Estado"]),
                    FechaRegistro = Fecha(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string GuardarArea(AreaOperativa area)
        {
            return EjecutarConMensaje("USP_DES_AREA_GUARDAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdAreaOperativa", area.IdAreaOperativa);
                cmd.Parameters.AddWithValue("@NombreArea", area.NombreArea);
                cmd.Parameters.AddWithValue("@Descripcion", area.Descripcion);
                cmd.Parameters.AddWithValue("@Estado", area.Estado);
            });
        }

        public string EliminarArea(int idAreaOperativa)
        {
            return EjecutarConMensaje("USP_DES_AREA_ELIMINAR_LOGICO", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdAreaOperativa", idAreaOperativa);
            });
        }

        public List<ConceptoMovimiento> ListarConceptos()
        {
            List<ConceptoMovimiento> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_CONCEPTO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new ConceptoMovimiento
                {
                    IdConceptoMovimiento = Entero(dr["IdConceptoMovimiento"]),
                    CodigoConcepto = Texto(dr["CodigoConcepto"]),
                    NombreConcepto = Texto(dr["NombreConcepto"]),
                    TipoMovimiento = Texto(dr["TipoMovimiento"]),
                    CategoriaMovimiento = Texto(dr["CategoriaMovimiento"]),
                    TipoCalculo = Texto(dr["TipoCalculo"]),
                    EsDescuento = Booleano(dr["EsDescuento"]),
                    Estado = Booleano(dr["Estado"]),
                    FechaRegistro = Fecha(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string GuardarConcepto(ConceptoMovimiento concepto)
        {
            return EjecutarConMensaje("USP_DES_CONCEPTO_GUARDAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdConceptoMovimiento", concepto.IdConceptoMovimiento);
                cmd.Parameters.AddWithValue("@CodigoConcepto", concepto.CodigoConcepto);
                cmd.Parameters.AddWithValue("@NombreConcepto", concepto.NombreConcepto);
                cmd.Parameters.AddWithValue("@TipoMovimiento", concepto.TipoMovimiento);
                cmd.Parameters.AddWithValue("@CategoriaMovimiento", concepto.CategoriaMovimiento);
                cmd.Parameters.AddWithValue("@TipoCalculo", concepto.TipoCalculo);
                cmd.Parameters.AddWithValue("@EsDescuento", concepto.EsDescuento);
                cmd.Parameters.AddWithValue("@Estado", concepto.Estado);
            });
        }

        public string EliminarConcepto(int idConceptoMovimiento)
        {
            return EjecutarConMensaje("USP_DES_CONCEPTO_ELIMINAR_LOGICO", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdConceptoMovimiento", idConceptoMovimiento);
            });
        }

        public List<OperacionTextil> ListarOperaciones()
        {
            List<OperacionTextil> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_OPERACION_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new OperacionTextil
                {
                    IdOperacionTextil = Entero(dr["IdOperacionTextil"]),
                    CodigoOperacion = Texto(dr["CodigoOperacion"]),
                    NombreOperacion = Texto(dr["NombreOperacion"]),
                    IdAreaOperativa = EnteroNullable(dr["IdAreaOperativa"]),
                    NombreArea = Texto(dr["NombreArea"]),
                    TipoOperacion = Texto(dr["TipoOperacion"]),
                    UnidadMedida = Texto(dr["UnidadMedida"]),
                    TarifaBase = Decimal(dr["TarifaBase"]),
                    FechaInicioVigencia = FechaNullable(dr["FechaInicioVigencia"]),
                    FechaFinVigencia = FechaNullable(dr["FechaFinVigencia"]),
                    Estado = Booleano(dr["Estado"]),
                    FechaRegistro = Fecha(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string GuardarOperacion(OperacionTextil operacion)
        {
            return EjecutarConMensaje("USP_DES_OPERACION_GUARDAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdOperacionTextil", operacion.IdOperacionTextil);
                cmd.Parameters.AddWithValue("@CodigoOperacion", operacion.CodigoOperacion);
                cmd.Parameters.AddWithValue("@NombreOperacion", operacion.NombreOperacion);
                cmd.Parameters.AddWithValue("@IdAreaOperativa", ValorDb(operacion.IdAreaOperativa));
                cmd.Parameters.AddWithValue("@TipoOperacion", operacion.TipoOperacion);
                cmd.Parameters.AddWithValue("@UnidadMedida", operacion.UnidadMedida);
                cmd.Parameters.AddWithValue("@TarifaBase", operacion.TarifaBase);
                cmd.Parameters.AddWithValue("@FechaInicioVigencia", ValorDb(operacion.FechaInicioVigencia));
                cmd.Parameters.AddWithValue("@FechaFinVigencia", ValorDb(operacion.FechaFinVigencia));
                cmd.Parameters.AddWithValue("@Estado", operacion.Estado);
            });
        }

        public string EliminarOperacion(int idOperacionTextil)
        {
            return EjecutarConMensaje("USP_DES_OPERACION_ELIMINAR_LOGICO", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdOperacionTextil", idOperacionTextil);
            });
        }

        public List<TrabajadorOperativo> ListarTrabajadores()
        {
            List<TrabajadorOperativo> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_TRABAJADOR_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new TrabajadorOperativo
                {
                    IdTrabajadorOperativo = Entero(dr["IdTrabajadorOperativo"]),
                    IdEmpleado = Entero(dr["IdEmpleado"]),
                    NombreTrabajador = Texto(dr["NombreTrabajador"]),
                    Documento = Texto(dr["Documento"]),
                    TipoTrabajador = Texto(dr["TipoTrabajador"]),
                    MedioPagoPreferido = Texto(dr["MedioPagoPreferido"]),
                    NumeroCuenta = Texto(dr["NumeroCuenta"]),
                    TelefonoPago = Texto(dr["TelefonoPago"]),
                    Observacion = Texto(dr["Observacion"]),
                    Estado = Booleano(dr["Estado"]),
                    FechaRegistro = Fecha(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string GuardarTrabajador(TrabajadorOperativo trabajador)
        {
            return EjecutarConMensaje("USP_DES_TRABAJADOR_GUARDAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdTrabajadorOperativo", trabajador.IdTrabajadorOperativo);
                cmd.Parameters.AddWithValue("@IdEmpleado", trabajador.IdEmpleado);
                cmd.Parameters.AddWithValue("@TipoTrabajador", trabajador.TipoTrabajador);
                cmd.Parameters.AddWithValue("@MedioPagoPreferido", trabajador.MedioPagoPreferido);
                cmd.Parameters.AddWithValue("@NumeroCuenta", trabajador.NumeroCuenta);
                cmd.Parameters.AddWithValue("@TelefonoPago", trabajador.TelefonoPago);
                cmd.Parameters.AddWithValue("@Observacion", trabajador.Observacion);
                cmd.Parameters.AddWithValue("@Estado", trabajador.Estado);
            });
        }

        public string EliminarTrabajador(int idTrabajadorOperativo)
        {
            return EjecutarConMensaje("USP_DES_TRABAJADOR_ELIMINAR_LOGICO", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdTrabajadorOperativo", idTrabajadorOperativo);
            });
        }

        public List<PeriodoPago> ListarPeriodos()
        {
            List<PeriodoPago> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_PERIODO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new PeriodoPago
                {
                    IdPeriodoPago = Entero(dr["IdPeriodoPago"]),
                    CodigoPeriodo = Texto(dr["CodigoPeriodo"]),
                    NumeroSemana = Entero(dr["NumeroSemana"]),
                    Anio = Entero(dr["Anio"]),
                    FechaInicio = Fecha(dr["FechaInicio"]),
                    FechaFin = Fecha(dr["FechaFin"]),
                    Estado = Texto(dr["Estado"]),
                    Observacion = Texto(dr["Observacion"]),
                    TotalIngresos = Decimal(dr["TotalIngresos"]),
                    TotalDescuentos = Decimal(dr["TotalDescuentos"]),
                    NetoCalculado = Decimal(dr["NetoCalculado"]),
                    TotalPagado = Decimal(dr["TotalPagado"]),
                    SaldoPendiente = Decimal(dr["SaldoPendiente"]),
                    FechaRegistro = Fecha(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string GuardarPeriodo(PeriodoPago periodo)
        {
            return EjecutarConMensaje("USP_DES_PERIODO_GUARDAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPeriodoPago", periodo.IdPeriodoPago);
                cmd.Parameters.AddWithValue("@CodigoPeriodo", periodo.CodigoPeriodo);
                cmd.Parameters.AddWithValue("@NumeroSemana", periodo.NumeroSemana);
                cmd.Parameters.AddWithValue("@Anio", periodo.Anio);
                cmd.Parameters.AddWithValue("@FechaInicio", periodo.FechaInicio.Date);
                cmd.Parameters.AddWithValue("@FechaFin", periodo.FechaFin.Date);
                cmd.Parameters.AddWithValue("@Estado", periodo.Estado);
                cmd.Parameters.AddWithValue("@Observacion", periodo.Observacion);
            });
        }

        public string CambiarEstadoPeriodo(int idPeriodoPago, string estado, string usuario)
        {
            return EjecutarConMensaje("USP_DES_PERIODO_CAMBIAR_ESTADO", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);
                cmd.Parameters.AddWithValue("@Estado", estado);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public List<MovimientoTrabajador> ListarMovimientos(int idPeriodoPago)
        {
            List<MovimientoTrabajador> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_MOVIMIENTO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago <= 0 ? DBNull.Value : idPeriodoPago);

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new MovimientoTrabajador
                {
                    IdMovimientoTrabajador = Entero(dr["IdMovimientoTrabajador"]),
                    IdPeriodoPago = Entero(dr["IdPeriodoPago"]),
                    CodigoPeriodo = Texto(dr["CodigoPeriodo"]),
                    IdTrabajadorOperativo = Entero(dr["IdTrabajadorOperativo"]),
                    NombreTrabajador = Texto(dr["NombreTrabajador"]),
                    Fecha = Fecha(dr["Fecha"]),
                    TipoMovimiento = Texto(dr["TipoMovimiento"]),
                    CategoriaMovimiento = Texto(dr["CategoriaMovimiento"]),
                    IdConceptoMovimiento = Entero(dr["IdConceptoMovimiento"]),
                    NombreConcepto = Texto(dr["NombreConcepto"]),
                    Descripcion = Texto(dr["Descripcion"]),
                    IdAreaOperativa = EnteroNullable(dr["IdAreaOperativa"]),
                    NombreArea = Texto(dr["NombreArea"]),
                    IdOperacionTextil = EnteroNullable(dr["IdOperacionTextil"]),
                    NombreOperacion = Texto(dr["NombreOperacion"]),
                    Cantidad = Decimal(dr["Cantidad"]),
                    UnidadMedida = Texto(dr["UnidadMedida"]),
                    Tarifa = Decimal(dr["Tarifa"]),
                    Importe = Decimal(dr["Importe"]),
                    EsDescuento = Booleano(dr["EsDescuento"]),
                    EsAutomatico = Booleano(dr["EsAutomatico"]),
                    OrigenMovimiento = Texto(dr["OrigenMovimiento"]),
                    ReferenciaId = EnteroNullable(dr["ReferenciaId"]),
                    Estado = Texto(dr["Estado"]),
                    Observacion = Texto(dr["Observacion"]),
                    CreadoPor = Texto(dr["CreadoPor"]),
                    FechaCreacion = Fecha(dr["FechaCreacion"]),
                    ModificadoPor = Texto(dr["ModificadoPor"]),
                    FechaModificacion = FechaNullable(dr["FechaModificacion"])
                });
            }

            return lista;
        }

        public string GuardarMovimiento(MovimientoTrabajador movimiento)
        {
            return EjecutarConMensaje("USP_DES_MOVIMIENTO_GUARDAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdMovimientoTrabajador", movimiento.IdMovimientoTrabajador);
                cmd.Parameters.AddWithValue("@IdPeriodoPago", movimiento.IdPeriodoPago);
                cmd.Parameters.AddWithValue("@IdTrabajadorOperativo", movimiento.IdTrabajadorOperativo);
                cmd.Parameters.AddWithValue("@Fecha", movimiento.Fecha.Date);
                cmd.Parameters.AddWithValue("@TipoMovimiento", movimiento.TipoMovimiento);
                cmd.Parameters.AddWithValue("@CategoriaMovimiento", movimiento.CategoriaMovimiento);
                cmd.Parameters.AddWithValue("@IdConceptoMovimiento", movimiento.IdConceptoMovimiento);
                cmd.Parameters.AddWithValue("@Descripcion", movimiento.Descripcion);
                cmd.Parameters.AddWithValue("@IdAreaOperativa", ValorDb(movimiento.IdAreaOperativa));
                cmd.Parameters.AddWithValue("@IdOperacionTextil", ValorDb(movimiento.IdOperacionTextil));
                cmd.Parameters.AddWithValue("@Cantidad", movimiento.Cantidad);
                cmd.Parameters.AddWithValue("@UnidadMedida", movimiento.UnidadMedida);
                cmd.Parameters.AddWithValue("@Tarifa", movimiento.Tarifa);
                cmd.Parameters.AddWithValue("@Importe", movimiento.Importe);
                cmd.Parameters.AddWithValue("@EsDescuento", movimiento.EsDescuento);
                cmd.Parameters.AddWithValue("@EsAutomatico", movimiento.EsAutomatico);
                cmd.Parameters.AddWithValue("@OrigenMovimiento", movimiento.OrigenMovimiento);
                cmd.Parameters.AddWithValue("@ReferenciaId", ValorDb(movimiento.ReferenciaId));
                cmd.Parameters.AddWithValue("@Estado", movimiento.Estado);
                cmd.Parameters.AddWithValue("@Observacion", movimiento.Observacion);
                cmd.Parameters.AddWithValue("@Usuario", movimiento.ModificadoPor);
            });
        }

        public string EliminarMovimiento(int idMovimientoTrabajador, string usuario)
        {
            return EjecutarConMensaje("USP_DES_MOVIMIENTO_ELIMINAR_LOGICO", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdMovimientoTrabajador", idMovimientoTrabajador);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public List<ResumenPagoTrabajador> ListarResumenPeriodo(int idPeriodoPago)
        {
            List<ResumenPagoTrabajador> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_RESUMEN_PERIODO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new ResumenPagoTrabajador
                {
                    IdPeriodoPago = Entero(dr["IdPeriodoPago"]),
                    IdTrabajadorOperativo = Entero(dr["IdTrabajadorOperativo"]),
                    NombreTrabajador = Texto(dr["NombreTrabajador"]),
                    Documento = Texto(dr["Documento"]),
                    TipoTrabajador = Texto(dr["TipoTrabajador"]),
                    MedioPagoPreferido = Texto(dr["MedioPagoPreferido"]),
                    SaldoAnterior = Decimal(dr["SaldoAnterior"]),
                    TotalIngresos = Decimal(dr["TotalIngresos"]),
                    TotalDescuentos = Decimal(dr["TotalDescuentos"]),
                    NetoCalculado = Decimal(dr["NetoCalculado"]),
                    TotalPagado = Decimal(dr["TotalPagado"]),
                    TotalPorPagar = Decimal(dr["TotalPorPagar"]),
                    SaldoPendiente = Decimal(dr["SaldoPendiente"]),
                    EstadoPeriodo = Texto(dr["EstadoPeriodo"]),
                    EstadoCalculo = Texto(dr["EstadoCalculo"]),
                    FechaCalculo = FechaNullable(dr["FechaCalculo"]),
                    UsuarioCalculo = Texto(dr["UsuarioCalculo"])
                });
            }

            return lista;
        }

        public List<AlertaCalculoPeriodo> ListarAlertasCalculoPeriodo(int idPeriodoPago)
        {
            List<AlertaCalculoPeriodo> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_CALCULO_PERIODO_ALERTAS_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new AlertaCalculoPeriodo
                {
                    IdCalculoPeriodoAlerta = Entero(dr["IdCalculoPeriodoAlerta"]),
                    IdPeriodoPago = Entero(dr["IdPeriodoPago"]),
                    IdTrabajadorOperativo = EnteroNullable(dr["IdTrabajadorOperativo"]),
                    NombreTrabajador = Texto(dr["NombreTrabajador"]),
                    IdMovimientoTrabajador = EnteroNullable(dr["IdMovimientoTrabajador"]),
                    IdCuotaProgramada = EnteroNullable(dr["IdCuotaProgramada"]),
                    TipoAlerta = Texto(dr["TipoAlerta"]),
                    Severidad = Texto(dr["Severidad"]),
                    Mensaje = Texto(dr["Mensaje"]),
                    FechaRegistro = Fecha(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string CalcularPeriodo(int idPeriodoPago, int? idTrabajadorOperativo, bool confirmar, string usuario)
        {
            return EjecutarConMensaje("USP_DES_CALCULO_PERIODO_CALCULAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);
                cmd.Parameters.AddWithValue("@IdTrabajadorOperativo", ValorDb(idTrabajadorOperativo));
                cmd.Parameters.AddWithValue("@Confirmar", confirmar);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public List<PrestamoTrabajador> ListarPrestamos()
        {
            List<PrestamoTrabajador> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_PRESTAMO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new PrestamoTrabajador
                {
                    IdPrestamoTrabajador = Entero(dr["IdPrestamoTrabajador"]),
                    IdTrabajadorOperativo = Entero(dr["IdTrabajadorOperativo"]),
                    NombreTrabajador = Texto(dr["NombreTrabajador"]),
                    FechaPrestamo = Fecha(dr["FechaPrestamo"]),
                    FechaInicioDescuento = Fecha(dr["FechaInicioDescuento"]),
                    IdConceptoMovimiento = EnteroNullable(dr["IdConceptoMovimiento"]),
                    NombreConcepto = Texto(dr["NombreConcepto"]),
                    MontoTotal = Decimal(dr["MontoTotal"]),
                    NumeroCuotas = Entero(dr["NumeroCuotas"]),
                    MontoCuota = Decimal(dr["MontoCuota"]),
                    SaldoPendiente = Decimal(dr["SaldoPendiente"]),
                    Estado = Texto(dr["Estado"]),
                    Observacion = Texto(dr["Observacion"]),
                    FechaRegistro = Fecha(dr["FechaRegistro"])
                });
            }

            return lista;
        }

        public string RegistrarPrestamo(PrestamoTrabajador prestamo, int idConceptoMovimiento, string usuario)
        {
            return EjecutarConMensaje("USP_DES_PRESTAMO_REGISTRAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdTrabajadorOperativo", prestamo.IdTrabajadorOperativo);
                cmd.Parameters.AddWithValue("@FechaPrestamo", prestamo.FechaPrestamo.Date);
                cmd.Parameters.AddWithValue("@FechaInicioDescuento", prestamo.FechaInicioDescuento.Date);
                cmd.Parameters.AddWithValue("@MontoTotal", prestamo.MontoTotal);
                cmd.Parameters.AddWithValue("@NumeroCuotas", prestamo.NumeroCuotas);
                cmd.Parameters.AddWithValue("@MontoCuota", prestamo.MontoCuota);
                cmd.Parameters.AddWithValue("@Observacion", prestamo.Observacion);
                cmd.Parameters.AddWithValue("@IdConceptoMovimiento", idConceptoMovimiento);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public List<CuotaProgramadaTrabajador> ListarCuotas(int? idTrabajadorOperativo)
        {
            List<CuotaProgramadaTrabajador> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_CUOTA_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdTrabajadorOperativo", ValorDb(idTrabajadorOperativo));

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new CuotaProgramadaTrabajador
                {
                    IdCuotaProgramada = Entero(dr["IdCuotaProgramada"]),
                    TipoOrigen = Texto(dr["TipoOrigen"]),
                    ReferenciaId = Entero(dr["ReferenciaId"]),
                    IdTrabajadorOperativo = Entero(dr["IdTrabajadorOperativo"]),
                    NombreTrabajador = Texto(dr["NombreTrabajador"]),
                    IdConceptoMovimiento = Entero(dr["IdConceptoMovimiento"]),
                    NombreConcepto = Texto(dr["NombreConcepto"]),
                    NumeroCuota = Entero(dr["NumeroCuota"]),
                    TotalCuotas = Entero(dr["TotalCuotas"]),
                    MontoCuota = Decimal(dr["MontoCuota"]),
                    FechaProgramada = Fecha(dr["FechaProgramada"]),
                    IdPeriodoAplicado = EnteroNullable(dr["IdPeriodoAplicado"]),
                    IdMovimientoTrabajador = EnteroNullable(dr["IdMovimientoTrabajador"]),
                    FechaAplicacion = FechaNullable(dr["FechaAplicacion"]),
                    CodigoPeriodoAplicado = Texto(dr["CodigoPeriodoAplicado"]),
                    Estado = Texto(dr["Estado"]),
                    Observacion = Texto(dr["Observacion"])
                });
            }

            return lista;
        }

        public string AplicarCuota(int idCuotaProgramada, int idPeriodoPago, string usuario)
        {
            return EjecutarConMensaje("USP_DES_CUOTA_APLICAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdCuotaProgramada", idCuotaProgramada);
                cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public string RegistrarPagoExtraordinarioPrestamo(int idPrestamoTrabajador, DateTime fechaPago, decimal montoPago, string observacion, string usuario)
        {
            return EjecutarConMensaje("USP_DES_PRESTAMO_PAGO_EXTRA_REGISTRAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPrestamoTrabajador", idPrestamoTrabajador);
                cmd.Parameters.AddWithValue("@FechaPago", fechaPago.Date);
                cmd.Parameters.AddWithValue("@MontoPago", montoPago);
                cmd.Parameters.AddWithValue("@Observacion", observacion);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public string SuspenderCuota(int idCuotaProgramada, string observacion, string usuario)
        {
            return EjecutarConMensaje("USP_DES_CUOTA_SUSPENDER", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdCuotaProgramada", idCuotaProgramada);
                cmd.Parameters.AddWithValue("@Observacion", observacion);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public string ReprogramarCuota(int idCuotaProgramada, DateTime fechaProgramada, decimal montoCuota, string observacion, string usuario)
        {
            return EjecutarConMensaje("USP_DES_CUOTA_REPROGRAMAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdCuotaProgramada", idCuotaProgramada);
                cmd.Parameters.AddWithValue("@FechaProgramada", fechaProgramada.Date);
                cmd.Parameters.AddWithValue("@MontoCuota", montoCuota);
                cmd.Parameters.AddWithValue("@Observacion", observacion);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public string CancelarPrestamo(int idPrestamoTrabajador, string observacion, string usuario)
        {
            return EjecutarConMensaje("USP_DES_PRESTAMO_CANCELAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPrestamoTrabajador", idPrestamoTrabajador);
                cmd.Parameters.AddWithValue("@Observacion", observacion);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public List<LotePago> ListarLotes(int? idPeriodoPago)
        {
            List<LotePago> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_LOTE_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdPeriodoPago", ValorDb(idPeriodoPago));

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new LotePago
                {
                    IdLotePago = Entero(dr["IdLotePago"]),
                    IdPeriodoPago = Entero(dr["IdPeriodoPago"]),
                    CodigoPeriodo = Texto(dr["CodigoPeriodo"]),
                    MedioPago = Texto(dr["MedioPago"]),
                    FechaGeneracion = Fecha(dr["FechaGeneracion"]),
                    UsuarioGenerador = Texto(dr["UsuarioGenerador"]),
                    Estado = Texto(dr["Estado"]),
                    TotalLote = Decimal(dr["TotalLote"]),
                    Observacion = Texto(dr["Observacion"])
                });
            }

            return lista;
        }

        public List<LotePagoDetalle> ListarLoteDetalles(int idLotePago)
        {
            List<LotePagoDetalle> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_LOTE_DETALLE_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdLotePago", idLotePago);

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new LotePagoDetalle
                {
                    IdLotePagoDetalle = Entero(dr["IdLotePagoDetalle"]),
                    IdLotePago = Entero(dr["IdLotePago"]),
                    IdTrabajadorOperativo = Entero(dr["IdTrabajadorOperativo"]),
                    NombreTrabajador = Texto(dr["NombreTrabajador"]),
                    MontoPago = Decimal(dr["MontoPago"]),
                    MedioPago = Texto(dr["MedioPago"]),
                    Estado = Texto(dr["Estado"]),
                    NumeroCuenta = Texto(dr["NumeroCuenta"]),
                    TelefonoPago = Texto(dr["TelefonoPago"])
                });
            }

            return lista;
        }

        public List<PagoTrabajador> ListarPagos(int? idPeriodoPago)
        {
            List<PagoTrabajador> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_PAGO_TRABAJADOR_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdPeriodoPago", ValorDb(idPeriodoPago));

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new PagoTrabajador
                {
                    IdPagoTrabajador = Entero(dr["IdPagoTrabajador"]),
                    IdPeriodoPago = Entero(dr["IdPeriodoPago"]),
                    CodigoPeriodo = Texto(dr["CodigoPeriodo"]),
                    IdTrabajadorOperativo = Entero(dr["IdTrabajadorOperativo"]),
                    NombreTrabajador = Texto(dr["NombreTrabajador"]),
                    IdLotePagoDetalle = EnteroNullable(dr["IdLotePagoDetalle"]),
                    FechaPago = Fecha(dr["FechaPago"]),
                    MedioPago = Texto(dr["MedioPago"]),
                    NumeroOperacion = Texto(dr["NumeroOperacion"]),
                    MontoPagado = Decimal(dr["MontoPagado"]),
                    Estado = Texto(dr["Estado"]),
                    Observacion = Texto(dr["Observacion"]),
                    UsuarioRegistro = Texto(dr["UsuarioRegistro"]),
                    MotivoAnulacion = Texto(dr["MotivoAnulacion"]),
                    UsuarioAnulacion = Texto(dr["UsuarioAnulacion"]),
                    AutorizadoPor = Texto(dr["AutorizadoPor"])
                });
            }

            return lista;
        }

        public string GenerarLotePago(int idPeriodoPago, string medioPago, string usuario, string observacion)
        {
            return EjecutarConMensaje("USP_DES_LOTE_GENERAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);
                cmd.Parameters.AddWithValue("@MedioPago", medioPago);
                cmd.Parameters.AddWithValue("@UsuarioGenerador", usuario);
                cmd.Parameters.AddWithValue("@Observacion", observacion);
            });
        }

        public string CambiarEstadoLote(int idLotePago, string estado, string usuario)
        {
            return EjecutarConMensaje("USP_DES_LOTE_CAMBIAR_ESTADO", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdLotePago", idLotePago);
                cmd.Parameters.AddWithValue("@Estado", estado);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public string RegistrarPagoTrabajador(
            int idPeriodoPago,
            int idTrabajadorOperativo,
            int? idLotePagoDetalle,
            string medioPago,
            decimal montoPagado,
            DateTime fechaPago,
            string numeroOperacion,
            string observacion,
            string medioPago2,
            decimal montoPagado2,
            string usuario)
        {
            return EjecutarConMensaje("USP_DES_PAGO_TRABAJADOR_REGISTRAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);
                cmd.Parameters.AddWithValue("@IdTrabajadorOperativo", idTrabajadorOperativo);
                cmd.Parameters.AddWithValue("@IdLotePagoDetalle", ValorDb(idLotePagoDetalle));
                cmd.Parameters.AddWithValue("@MedioPago", medioPago);
                cmd.Parameters.AddWithValue("@MontoPagado", montoPagado);
                cmd.Parameters.AddWithValue("@FechaPago", fechaPago);
                cmd.Parameters.AddWithValue("@NumeroOperacion", numeroOperacion);
                cmd.Parameters.AddWithValue("@Observacion", observacion);
                cmd.Parameters.AddWithValue("@MedioPago2", medioPago2);
                cmd.Parameters.AddWithValue("@MontoPagado2", montoPagado2);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public string AnularPagoTrabajador(int idPagoTrabajador, string motivo, string autorizadoPor, string usuario)
        {
            return EjecutarConMensaje("USP_DES_PAGO_TRABAJADOR_ANULAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPagoTrabajador", idPagoTrabajador);
                cmd.Parameters.AddWithValue("@MotivoAnulacion", motivo);
                cmd.Parameters.AddWithValue("@AutorizadoPor", autorizadoPor);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public DashboardDestajoIndicador ObtenerDashboard(int idPeriodoPago)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_DASHBOARD_INDICADORES", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read())
                return new DashboardDestajoIndicador();

            return new DashboardDestajoIndicador
            {
                TrabajadoresActivos = Entero(dr["TrabajadoresActivos"]),
                TrabajadoresConMovimientos = Entero(dr["TrabajadoresConMovimientos"]),
                TotalProducido = Decimal(dr["TotalProducido"]),
                TotalIngresos = Decimal(dr["TotalIngresos"]),
                TotalDescuentos = Decimal(dr["TotalDescuentos"]),
                NetoPeriodo = Decimal(dr["NetoPeriodo"]),
                TotalPagado = Decimal(dr["TotalPagado"]),
                SaldoPendiente = Decimal(dr["SaldoPendiente"]),
                PrestamosActivos = Entero(dr["PrestamosActivos"]),
                CuotasAplicadas = Entero(dr["CuotasAplicadas"]),
                PeriodosAbiertos = Entero(dr["PeriodosAbiertos"]),
                PeriodosPendientesCierre = Entero(dr["PeriodosPendientesCierre"])
            };
        }

        public List<DashboardDestajoSerie> ListarDashboardSeries(int idPeriodoPago)
        {
            List<DashboardDestajoSerie> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_DASHBOARD_SERIES", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new DashboardDestajoSerie
                {
                    Categoria = Texto(dr["Categoria"]),
                    Etiqueta = Texto(dr["Etiqueta"]),
                    Valor = Decimal(dr["Valor"]),
                    Importe = Decimal(dr["Importe"])
                });
            }

            return lista;
        }

        public List<AuditoriaDestajo> ListarAuditoriaDestajo(int? idPeriodoPago)
        {
            List<AuditoriaDestajo> lista = [];

            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new("USP_DES_AUDITORIA_DESTAJO_LISTAR", conexion);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdPeriodoPago", ValorDb(idPeriodoPago));

            conexion.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new AuditoriaDestajo
                {
                    IdAuditoria = Entero(dr["IdAuditoria"]),
                    Usuario = Texto(dr["Usuario"]),
                    Fecha = Fecha(dr["Fecha"]),
                    Modulo = Texto(dr["Modulo"]),
                    Accion = Texto(dr["Accion"]),
                    RegistroAfectado = Texto(dr["RegistroAfectado"]),
                    ValorAnterior = Texto(dr["ValorAnterior"]),
                    ValorNuevo = Texto(dr["ValorNuevo"]),
                    Motivo = Texto(dr["Motivo"]),
                    Equipo = Texto(dr["Equipo"])
                });
            }

            return lista;
        }

        public string RegistrarBoletasGeneradas(int idPeriodoPago, int cantidad, string usuario)
        {
            return EjecutarConMensaje("USP_DES_PERIODO_BOLETAS_GENERADAS_REGISTRAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);
                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        public string CerrarPeriodo(int idPeriodoPago, string usuario)
        {
            return EjecutarConMensaje("USP_DES_PERIODO_CERRAR", cmd =>
            {
                cmd.Parameters.AddWithValue("@IdPeriodoPago", idPeriodoPago);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
            });
        }

        private static string EjecutarConMensaje(string procedimiento, Action<SqlCommand> configurar)
        {
            using SqlConnection conexion = Conexion.ObtenerConexion();
            using SqlCommand cmd = new(procedimiento, conexion);
            cmd.CommandType = CommandType.StoredProcedure;

            configurar(cmd);

            SqlParameter resultado = new("@Resultado", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };

            SqlParameter mensaje = new("@Mensaje", SqlDbType.VarChar, 500)
            {
                Direction = ParameterDirection.Output
            };

            cmd.Parameters.Add(resultado);
            cmd.Parameters.Add(mensaje);

            conexion.Open();
            cmd.ExecuteNonQuery();

            return mensaje.Value?.ToString() ?? string.Empty;
        }

        private static object ValorDb(int? valor)
        {
            return valor.HasValue && valor.Value > 0 ? valor.Value : DBNull.Value;
        }

        private static object ValorDb(DateTime? valor)
        {
            return valor.HasValue ? valor.Value.Date : DBNull.Value;
        }

        private static int Entero(object valor)
        {
            return valor == DBNull.Value ? 0 : Convert.ToInt32(valor);
        }

        private static int? EnteroNullable(object valor)
        {
            return valor == DBNull.Value ? null : Convert.ToInt32(valor);
        }

        private static decimal Decimal(object valor)
        {
            return valor == DBNull.Value ? 0 : Convert.ToDecimal(valor);
        }

        private static bool Booleano(object valor)
        {
            return valor != DBNull.Value && Convert.ToBoolean(valor);
        }

        private static DateTime Fecha(object valor)
        {
            return valor == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(valor);
        }

        private static DateTime? FechaNullable(object valor)
        {
            return valor == DBNull.Value ? null : Convert.ToDateTime(valor);
        }

        private static string Texto(object valor)
        {
            return valor == DBNull.Value ? string.Empty : valor.ToString() ?? string.Empty;
        }
    }
}
