using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class DestajoPagosNegocio
    {
        private readonly DestajoPagosDatos _datos = new();

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

            return _datos.GuardarPeriodo(periodo);
        }

        public string CambiarEstadoPeriodo(int idPeriodoPago, string estado, string usuario)
        {
            if (idPeriodoPago <= 0)
                return "Debe seleccionar un periodo.";

            if (string.IsNullOrWhiteSpace(estado))
                return "Debe seleccionar un estado.";

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

            if (movimiento.IdTrabajadorOperativo <= 0)
                return "Debe seleccionar un trabajador.";

            if (movimiento.IdConceptoMovimiento <= 0)
                return "Debe seleccionar un concepto.";

            if (string.IsNullOrWhiteSpace(movimiento.TipoMovimiento))
                return "Debe seleccionar el tipo de movimiento.";

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
            string observacion,
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

            return _datos.RegistrarPagoTrabajador(
                idPeriodoPago,
                idTrabajadorOperativo,
                idLotePagoDetalle,
                medioPago.Trim(),
                montoPagado,
                observacion.Trim(),
                usuario);
        }

        private static decimal CalcularImporte(MovimientoTrabajador movimiento)
        {
            if (movimiento.Cantidad > 0 && movimiento.Tarifa > 0)
                return Math.Round(movimiento.Cantidad * movimiento.Tarifa, 2);

            return Math.Round(movimiento.Importe, 2);
        }
    }
}
