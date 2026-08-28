using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class DestajoPagosNegocio
    {
        private readonly DestajoPagosDatos _datos = new();
        private static readonly HashSet<string> EstadosPeriodoValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Borrador",
            "Abierto",
            "En calculo",
            "Calculado",
            "En pago",
            "Cerrado",
            "Anulado"
        };

        public List<AreaOperativa> ListarAreas()
        {
            return _datos.ListarAreas();
        }

        public string GuardarArea(AreaOperativa area)
        {
            area.NombreArea = area.NombreArea.Trim();
            area.Descripcion = area.Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(area.NombreArea))
                return "El nombre del área es obligatorio.";

            return _datos.GuardarArea(area);
        }

        public string EliminarArea(int idAreaOperativa)
        {
            if (idAreaOperativa <= 0)
                return "Debe seleccionar un área válida.";

            return _datos.EliminarArea(idAreaOperativa);
        }

        public List<ConceptoMovimiento> ListarConceptos()
        {
            return _datos.ListarConceptos();
        }

        public string GuardarConcepto(ConceptoMovimiento concepto)
        {
            concepto.CodigoConcepto = concepto.CodigoConcepto.Trim().ToUpperInvariant();
            concepto.NombreConcepto = concepto.NombreConcepto.Trim();
            concepto.TipoMovimiento = concepto.TipoMovimiento.Trim();
            concepto.CategoriaMovimiento = concepto.CategoriaMovimiento.Trim();
            concepto.TipoCalculo = concepto.TipoCalculo.Trim();

            if (string.IsNullOrWhiteSpace(concepto.CodigoConcepto))
                return "El código del concepto es obligatorio.";

            if (string.IsNullOrWhiteSpace(concepto.NombreConcepto))
                return "El nombre del concepto es obligatorio.";

            if (string.IsNullOrWhiteSpace(concepto.TipoMovimiento))
                return "Debe seleccionar el tipo de movimiento.";

            if (concepto.EsDescuento && concepto.TipoMovimiento == "Ingreso")
                concepto.TipoMovimiento = "Descuento";

            return _datos.GuardarConcepto(concepto);
        }

        public string EliminarConcepto(int idConceptoMovimiento)
        {
            if (idConceptoMovimiento <= 0)
                return "Debe seleccionar un concepto válido.";

            return _datos.EliminarConcepto(idConceptoMovimiento);
        }

        public List<OperacionTextil> ListarOperaciones()
        {
            return _datos.ListarOperaciones();
        }

        public string GuardarOperacion(OperacionTextil operacion)
        {
            operacion.CodigoOperacion = operacion.CodigoOperacion.Trim().ToUpperInvariant();
            operacion.NombreOperacion = operacion.NombreOperacion.Trim();
            operacion.TipoOperacion = operacion.TipoOperacion.Trim();
            operacion.UnidadMedida = operacion.UnidadMedida.Trim();

            if (string.IsNullOrWhiteSpace(operacion.CodigoOperacion))
                return "El código de la operación es obligatorio.";

            if (string.IsNullOrWhiteSpace(operacion.NombreOperacion))
                return "El nombre de la operación es obligatorio.";

            if (string.IsNullOrWhiteSpace(operacion.UnidadMedida))
                return "La unidad de medida es obligatoria.";

            if (operacion.TarifaBase < 0)
                return "La tarifa base no puede ser negativa.";

            if (operacion.FechaInicioVigencia.HasValue
                && operacion.FechaFinVigencia.HasValue
                && operacion.FechaInicioVigencia.Value.Date > operacion.FechaFinVigencia.Value.Date)
                return "La fecha de inicio de vigencia no puede ser mayor que la fecha fin.";

            return _datos.GuardarOperacion(operacion);
        }

        public string EliminarOperacion(int idOperacionTextil)
        {
            if (idOperacionTextil <= 0)
                return "Debe seleccionar una operación válida.";

            return _datos.EliminarOperacion(idOperacionTextil);
        }

        public List<TrabajadorOperativo> ListarTrabajadores()
        {
            return _datos.ListarTrabajadores();
        }

        public string GuardarTrabajador(TrabajadorOperativo trabajador)
        {
            trabajador.TipoTrabajador = trabajador.TipoTrabajador.Trim();
            trabajador.MedioPagoPreferido = trabajador.MedioPagoPreferido.Trim();
            trabajador.NumeroCuenta = trabajador.NumeroCuenta.Trim();
            trabajador.TelefonoPago = trabajador.TelefonoPago.Trim();
            trabajador.Observacion = trabajador.Observacion.Trim();

            if (trabajador.IdEmpleado <= 0)
                return "Debe seleccionar un empleado.";

            if (string.IsNullOrWhiteSpace(trabajador.TipoTrabajador))
                return "Debe seleccionar el tipo de trabajador.";

            if (string.IsNullOrWhiteSpace(trabajador.MedioPagoPreferido))
                return "Debe seleccionar el medio de pago preferido.";

            if (RequiereCuenta(trabajador.MedioPagoPreferido) && string.IsNullOrWhiteSpace(trabajador.NumeroCuenta))
                return "Debe registrar la cuenta bancaria para este medio de pago.";

            if (RequiereTelefono(trabajador.MedioPagoPreferido) && string.IsNullOrWhiteSpace(trabajador.TelefonoPago))
                return "Debe registrar el telefono para Yape o Plin.";

            return _datos.GuardarTrabajador(trabajador);
        }

        public string EliminarTrabajador(int idTrabajadorOperativo)
        {
            if (idTrabajadorOperativo <= 0)
                return "Debe seleccionar un trabajador válido.";

            return _datos.EliminarTrabajador(idTrabajadorOperativo);
        }

        public List<PeriodoPago> ListarPeriodos()
        {
            return _datos.ListarPeriodos();
        }

        public string GuardarPeriodo(PeriodoPago periodo)
        {
            periodo.CodigoPeriodo = periodo.CodigoPeriodo.Trim().ToUpperInvariant();
            periodo.Observacion = periodo.Observacion.Trim();

            if (string.IsNullOrWhiteSpace(periodo.CodigoPeriodo))
                return "El código del periodo es obligatorio.";

            if (periodo.FechaInicio.Date > periodo.FechaFin.Date)
                return "La fecha de inicio no puede ser mayor que la fecha fin.";

            if (string.IsNullOrWhiteSpace(periodo.Estado))
                periodo.Estado = "Borrador";

            if (!EstadosPeriodoValidos.Contains(periodo.Estado))
                return "El estado del periodo no es valido.";

            periodo.NumeroSemana = periodo.NumeroSemana > 0
                ? periodo.NumeroSemana
                : ISOWeek.GetWeekOfYear(periodo.FechaInicio);
            periodo.Anio = periodo.Anio > 0
                ? periodo.Anio
                : ISOWeek.GetYear(periodo.FechaInicio);

            return _datos.GuardarPeriodo(periodo);
        }

        public string CambiarEstadoPeriodo(int idPeriodoPago, string estado, string usuario)
        {
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            if (string.IsNullOrWhiteSpace(estado))
                return "Debe seleccionar un estado.";

            if (!EstadosPeriodoValidos.Contains(estado))
                return "El estado del periodo no es valido.";

            return _datos.CambiarEstadoPeriodo(idPeriodoPago, estado.Trim(), usuario);
        }

        public List<MovimientoTrabajador> ListarMovimientos(int idPeriodoPago)
        {
            return _datos.ListarMovimientos(idPeriodoPago);
        }

        public string GuardarMovimiento(MovimientoTrabajador movimiento)
        {
            movimiento.TipoMovimiento = movimiento.TipoMovimiento.Trim();
            movimiento.CategoriaMovimiento = movimiento.CategoriaMovimiento.Trim();
            movimiento.Descripcion = movimiento.Descripcion.Trim();
            movimiento.UnidadMedida = movimiento.UnidadMedida.Trim();
            movimiento.OrigenMovimiento = string.IsNullOrWhiteSpace(movimiento.OrigenMovimiento)
                ? "Manual"
                : movimiento.OrigenMovimiento.Trim();
            movimiento.Estado = string.IsNullOrWhiteSpace(movimiento.Estado)
                ? "Borrador"
                : movimiento.Estado.Trim();
            movimiento.Observacion = movimiento.Observacion.Trim();
            movimiento.ModificadoPor = movimiento.ModificadoPor.Trim();

            if (movimiento.IdPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            PeriodoPago? periodo = ListarPeriodos().FirstOrDefault(p => p.IdPeriodoPago == movimiento.IdPeriodoPago);

            if (periodo == null)
                return "El periodo seleccionado no existe.";

            if (!periodo.Estado.Equals("Abierto", StringComparison.OrdinalIgnoreCase))
                return "Solo se pueden registrar movimientos en periodos abiertos.";

            if (movimiento.Fecha.Date < periodo.FechaInicio.Date || movimiento.Fecha.Date > periodo.FechaFin.Date)
                return "La fecha del trabajo debe estar dentro del periodo seleccionado.";

            if (movimiento.IdTrabajadorOperativo <= 0)
                return "Debe seleccionar un trabajador.";

            if (movimiento.IdConceptoMovimiento <= 0)
                return "Debe seleccionar un concepto.";

            if (string.IsNullOrWhiteSpace(movimiento.TipoMovimiento))
                return "Debe seleccionar el tipo de movimiento.";

            bool esProduccion = EsCategoriaProduccion(movimiento.CategoriaMovimiento);

            if ((movimiento.EsDescuento || movimiento.TipoMovimiento.Equals("Descuento", StringComparison.OrdinalIgnoreCase)) && esProduccion)
                return "Los descuentos no pueden registrarse como produccion.";

            if (esProduccion && (!movimiento.IdOperacionTextil.HasValue || movimiento.IdOperacionTextil.Value <= 0))
                return "Debe seleccionar una operacion para registrar produccion.";

            if (movimiento.IdOperacionTextil.HasValue && movimiento.IdOperacionTextil.Value > 0)
            {
                OperacionTextil? operacion = ListarOperaciones()
                    .FirstOrDefault(o => o.IdOperacionTextil == movimiento.IdOperacionTextil.Value);

                if (operacion == null)
                    return "La operacion seleccionada no existe.";

                if (!operacion.Estado)
                    return "La operacion seleccionada esta inactiva.";

                if (operacion.FechaInicioVigencia.HasValue && movimiento.Fecha.Date < operacion.FechaInicioVigencia.Value.Date)
                    return "La operacion seleccionada no tiene tarifa vigente para la fecha del trabajo.";

                if (operacion.FechaFinVigencia.HasValue && movimiento.Fecha.Date > operacion.FechaFinVigencia.Value.Date)
                    return "La operacion seleccionada no tiene tarifa vigente para la fecha del trabajo.";

                movimiento.Tarifa = operacion.TarifaBase;
                movimiento.UnidadMedida = operacion.UnidadMedida;

                if (!movimiento.IdAreaOperativa.HasValue && operacion.IdAreaOperativa.HasValue)
                    movimiento.IdAreaOperativa = operacion.IdAreaOperativa;
            }

            if (movimiento.Cantidad < 0)
                return "La cantidad no puede ser negativa.";

            if (movimiento.Tarifa < 0)
                return "La tarifa no puede ser negativa.";

            movimiento.Importe = CalcularImporte(movimiento);

            if (movimiento.Importe <= 0)
                return "El importe debe ser mayor a cero.";

            return _datos.GuardarMovimiento(movimiento);
        }

        public string EliminarMovimiento(int idMovimientoTrabajador, string usuario)
        {
            if (idMovimientoTrabajador <= 0)
                return "Debe seleccionar un movimiento válido.";

            return _datos.EliminarMovimiento(idMovimientoTrabajador, usuario);
        }

        public List<ResumenPagoTrabajador> ListarResumenPeriodo(int idPeriodoPago)
        {
            if (idPeriodoPago <= 0)
                return [];

            return _datos.ListarResumenPeriodo(idPeriodoPago);
        }

        public List<AlertaCalculoPeriodo> ListarAlertasCalculoPeriodo(int idPeriodoPago)
        {
            if (idPeriodoPago <= 0)
                return [];

            return _datos.ListarAlertasCalculoPeriodo(idPeriodoPago);
        }

        public string CalcularPeriodo(int idPeriodoPago, string usuario)
        {
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            return _datos.CalcularPeriodo(idPeriodoPago, null, false, usuario);
        }

        public string RecalcularTrabajador(int idPeriodoPago, int idTrabajadorOperativo, string usuario)
        {
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            if (idTrabajadorOperativo <= 0)
                return "Debe seleccionar un trabajador del resumen.";

            return _datos.CalcularPeriodo(idPeriodoPago, idTrabajadorOperativo, false, usuario);
        }

        public string ConfirmarCalculoPeriodo(int idPeriodoPago, string usuario)
        {
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            return _datos.CalcularPeriodo(idPeriodoPago, null, true, usuario);
        }

        public List<PrestamoTrabajador> ListarPrestamos()
        {
            return _datos.ListarPrestamos();
        }

        public string RegistrarPrestamo(PrestamoTrabajador prestamo, int idConceptoMovimiento, string usuario)
        {
            prestamo.Observacion = prestamo.Observacion.Trim();

            if (prestamo.IdTrabajadorOperativo <= 0)
                return "Debe seleccionar un trabajador.";

            if (prestamo.MontoTotal <= 0)
                return "El monto del préstamo debe ser mayor a cero.";

            if (prestamo.NumeroCuotas <= 0)
                return "El número de cuotas debe ser mayor a cero.";

            if (idConceptoMovimiento <= 0)
                return "Debe seleccionar el concepto de descuento para la cuota.";

            if (prestamo.FechaInicioDescuento == DateTime.MinValue)
                prestamo.FechaInicioDescuento = prestamo.FechaPrestamo;

            if (prestamo.MontoCuota <= 0)
                prestamo.MontoCuota = Math.Round(prestamo.MontoTotal / prestamo.NumeroCuotas, 2);

            return _datos.RegistrarPrestamo(prestamo, idConceptoMovimiento, usuario);
        }

        public List<CuotaProgramadaTrabajador> ListarCuotas(int? idTrabajadorOperativo)
        {
            return _datos.ListarCuotas(idTrabajadorOperativo);
        }

        public string AplicarCuota(int idCuotaProgramada, int idPeriodoPago, string usuario)
        {
            if (idCuotaProgramada <= 0)
                return "Debe seleccionar una cuota.";

            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            return _datos.AplicarCuota(idCuotaProgramada, idPeriodoPago, usuario);
        }

        public string RegistrarPagoExtraordinarioPrestamo(int idPrestamoTrabajador, DateTime fechaPago, decimal montoPago, string observacion, string usuario)
        {
            if (idPrestamoTrabajador <= 0)
                return "Debe seleccionar un prestamo.";

            if (montoPago <= 0)
                return "El pago extraordinario debe ser mayor a cero.";

            return _datos.RegistrarPagoExtraordinarioPrestamo(idPrestamoTrabajador, fechaPago, montoPago, observacion.Trim(), usuario);
        }

        public string SuspenderCuota(int idCuotaProgramada, string observacion, string usuario)
        {
            if (idCuotaProgramada <= 0)
                return "Debe seleccionar una cuota.";

            return _datos.SuspenderCuota(idCuotaProgramada, observacion.Trim(), usuario);
        }

        public string ReprogramarCuota(int idCuotaProgramada, DateTime fechaProgramada, decimal montoCuota, string observacion, string usuario)
        {
            if (idCuotaProgramada <= 0)
                return "Debe seleccionar una cuota.";

            if (fechaProgramada == DateTime.MinValue)
                return "Debe seleccionar la nueva fecha de la cuota.";

            if (montoCuota <= 0)
                return "El monto de la cuota debe ser mayor a cero.";

            return _datos.ReprogramarCuota(idCuotaProgramada, fechaProgramada, montoCuota, observacion.Trim(), usuario);
        }

        public string CancelarPrestamo(int idPrestamoTrabajador, string observacion, string usuario)
        {
            if (idPrestamoTrabajador <= 0)
                return "Debe seleccionar un prestamo.";

            return _datos.CancelarPrestamo(idPrestamoTrabajador, observacion.Trim(), usuario);
        }

        public List<LotePago> ListarLotes(int? idPeriodoPago)
        {
            return _datos.ListarLotes(idPeriodoPago);
        }

        public List<LotePagoDetalle> ListarLoteDetalles(int idLotePago)
        {
            if (idLotePago <= 0)
                return [];

            return _datos.ListarLoteDetalles(idLotePago);
        }

        public List<PagoTrabajador> ListarPagos(int? idPeriodoPago)
        {
            return _datos.ListarPagos(idPeriodoPago);
        }

        public string GenerarLotePago(int idPeriodoPago, string medioPago, string usuario, string observacion)
        {
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            if (string.IsNullOrWhiteSpace(medioPago))
                return "Debe seleccionar un medio de pago.";

            return _datos.GenerarLotePago(idPeriodoPago, medioPago.Trim(), usuario, observacion.Trim());
        }

        public string CambiarEstadoLote(int idLotePago, string estado, string usuario)
        {
            if (idLotePago <= 0)
                return "Debe seleccionar un lote.";

            if (string.IsNullOrWhiteSpace(estado))
                return "Debe seleccionar un estado.";

            return _datos.CambiarEstadoLote(idLotePago, estado.Trim(), usuario);
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
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            if (idTrabajadorOperativo <= 0)
                return "Debe seleccionar un trabajador.";

            if (string.IsNullOrWhiteSpace(medioPago))
                return "Debe seleccionar el medio de pago.";

            if (montoPagado <= 0)
                return "El monto a pagar debe ser mayor a cero.";

            if (fechaPago == DateTime.MinValue)
                return "Debe registrar la fecha de pago.";

            medioPago = medioPago.Trim();
            medioPago2 = medioPago2.Trim();
            numeroOperacion = numeroOperacion.Trim();
            observacion = observacion.Trim();

            if (montoPagado2 > 0 && string.IsNullOrWhiteSpace(medioPago2))
                return "Debe seleccionar el segundo medio del pago mixto.";

            if (montoPagado2 > 0 && medioPago.Equals(medioPago2, StringComparison.OrdinalIgnoreCase))
                return "Los medios del pago mixto deben ser distintos.";

            if (montoPagado2 > 0 && !medioPago.Equals("Mixto", StringComparison.OrdinalIgnoreCase))
                medioPago = "Mixto";

            return _datos.RegistrarPagoTrabajador(
                idPeriodoPago,
                idTrabajadorOperativo,
                idLotePagoDetalle,
                medioPago,
                montoPagado,
                fechaPago,
                numeroOperacion,
                observacion,
                medioPago2,
                montoPagado2,
                usuario);
        }

        public string AnularPagoTrabajador(int idPagoTrabajador, string motivo, string autorizadoPor, string usuario)
        {
            if (idPagoTrabajador <= 0)
                return "Debe seleccionar un pago.";

            motivo = motivo.Trim();
            autorizadoPor = autorizadoPor.Trim();

            if (string.IsNullOrWhiteSpace(motivo))
                return "Debe indicar el motivo de anulación.";

            if (string.IsNullOrWhiteSpace(autorizadoPor))
                return "Debe indicar quien autoriza la anulación.";

            return _datos.AnularPagoTrabajador(idPagoTrabajador, motivo, autorizadoPor, usuario);
        }

        public DashboardDestajoIndicador ObtenerDashboard(int idPeriodoPago)
        {
            if (idPeriodoPago <= 0)
                return new DashboardDestajoIndicador();

            return _datos.ObtenerDashboard(idPeriodoPago);
        }

        public List<DashboardDestajoSerie> ListarDashboardSeries(int idPeriodoPago)
        {
            if (idPeriodoPago <= 0)
                return [];

            return _datos.ListarDashboardSeries(idPeriodoPago);
        }

        public List<AuditoriaDestajo> ListarAuditoriaDestajo(int? idPeriodoPago)
        {
            return _datos.ListarAuditoriaDestajo(idPeriodoPago);
        }

        public string RegistrarBoletasGeneradas(int idPeriodoPago, int cantidad, string usuario)
        {
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            if (cantidad <= 0)
                return "Debe generar al menos una boleta.";

            return _datos.RegistrarBoletasGeneradas(idPeriodoPago, cantidad, usuario);
        }

        public string CerrarPeriodo(int idPeriodoPago, string usuario)
        {
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            return _datos.CerrarPeriodo(idPeriodoPago, usuario);
        }

        private static decimal CalcularImporte(MovimientoTrabajador movimiento)
        {
            if (movimiento.Cantidad > 0 && movimiento.Tarifa > 0)
                return Math.Round(movimiento.Cantidad * movimiento.Tarifa, 2);

            return Math.Round(movimiento.Importe, 2);
        }

        private static bool EsCategoriaProduccion(string categoria)
        {
            return categoria.Equals("Produccion", StringComparison.OrdinalIgnoreCase)
                || categoria.Equals("Produccion por destajo", StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiereCuenta(string medioPago)
        {
            return medioPago.Equals("BCP", StringComparison.OrdinalIgnoreCase)
                || medioPago.Equals("Transferencia", StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiereTelefono(string medioPago)
        {
            return medioPago.Equals("Yape", StringComparison.OrdinalIgnoreCase)
                || medioPago.Equals("Plin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
