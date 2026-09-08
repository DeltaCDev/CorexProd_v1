using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class ObligacionFinancieraNegocio
    {
        private readonly ObligacionFinancieraDatos _datos = new();

        public ObligacionFinancieraResultado GuardarPrestamo(ObligacionFinanciera obligacion, string usuario)
        {
            string validacion = ValidarPrestamo(obligacion, requiereCronograma: true);
            if (!string.IsNullOrWhiteSpace(validacion))
                return Resultado(obligacion?.IdObligacionFinanciera ?? 0, validacion);

            NormalizarPrestamo(obligacion, normalizarCronograma: true);
            return _datos.GuardarPrestamo(obligacion, Usuario(usuario));
        }

        public ObligacionFinancieraResultado ActualizarPrestamo(ObligacionFinanciera obligacion, string usuario, bool actualizarCronograma = true)
        {
            if (obligacion == null)
                return Resultado(0, "Debe ingresar los datos del prestamo.");

            if (obligacion.IdObligacionFinanciera <= 0)
                return Resultado(obligacion.IdObligacionFinanciera, "Debe seleccionar un prestamo valido.");

            string validacion = ValidarPrestamo(obligacion, requiereCronograma: actualizarCronograma);
            if (!string.IsNullOrWhiteSpace(validacion))
                return Resultado(obligacion.IdObligacionFinanciera, validacion);

            NormalizarPrestamo(obligacion, normalizarCronograma: actualizarCronograma);
            if (!actualizarCronograma)
                obligacion.Cuotas = [];

            return _datos.ActualizarPrestamo(obligacion, Usuario(usuario));
        }

        public List<ObligacionFinancieraResumen> Listar(string? texto = null, string? estado = null, int? idTipoObligacionFinanciera = null, int? idEntidadFinanciera = null, string? tipoTitular = null, int? idEmpresaResponsable = null)
        {
            return _datos.Listar(texto, estado, idTipoObligacionFinanciera, idEntidadFinanciera, tipoTitular, idEmpresaResponsable);
        }

        public ObligacionFinanciera? Obtener(int idObligacionFinanciera)
        {
            return idObligacionFinanciera <= 0 ? null : _datos.Obtener(idObligacionFinanciera);
        }

        public ObligacionFinancieraDetalle? ObtenerDetalle(int idObligacionFinanciera)
        {
            return idObligacionFinanciera <= 0 ? null : _datos.ObtenerDetalle(idObligacionFinanciera);
        }

        public ObligacionFinancieraResultado Anular(int idObligacionFinanciera, string usuario, string motivo)
        {
            if (idObligacionFinanciera <= 0)
                return Resultado(idObligacionFinanciera, "Debe seleccionar una obligacion financiera valida.");

            if (string.IsNullOrWhiteSpace(motivo))
                return Resultado(idObligacionFinanciera, "Debe ingresar el motivo de anulacion.");

            return _datos.Anular(idObligacionFinanciera, Usuario(usuario), motivo.Trim());
        }

        public ObligacionFinancieraPagoResultado RegistrarPago(ObligacionFinancieraPago pago, string usuario)
        {
            string validacion = ValidarPago(pago);
            if (!string.IsNullOrWhiteSpace(validacion))
            {
                return new ObligacionFinancieraPagoResultado
                {
                    IdPago = pago?.IdPago ?? 0,
                    Resultado = false,
                    Mensaje = validacion
                };
            }

            pago.FechaPago = pago.FechaPago.Date;
            pago.Monto = Math.Round(pago.Monto, 2);
            pago.NumeroOperacion = pago.NumeroOperacion?.Trim() ?? string.Empty;
            pago.Observacion = pago.Observacion?.Trim() ?? string.Empty;

            return _datos.RegistrarPago(pago, Usuario(usuario));
        }

        public ObligacionFinancieraPagoResultado AnularPago(int idPago, string motivo, string usuario)
        {
            if (idPago <= 0)
            {
                return new ObligacionFinancieraPagoResultado
                {
                    IdPago = idPago,
                    Resultado = false,
                    Mensaje = "Debe seleccionar un pago valido."
                };
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return new ObligacionFinancieraPagoResultado
                {
                    IdPago = idPago,
                    Resultado = false,
                    Mensaje = "Debe ingresar el motivo de anulacion del pago."
                };
            }

            return _datos.AnularPago(idPago, motivo.Trim(), Usuario(usuario));
        }

        public List<EntidadFinanciera> ListarEntidadesFinancieras(string? tipoEntidad = null)
        {
            return _datos.ListarEntidadesFinancieras(tipoEntidad);
        }

        public List<TipoObligacionFinanciera> ListarTiposObligacionFinanciera()
        {
            return _datos.ListarTiposObligacionFinanciera();
        }

        public List<BancoTesoreria> ListarBancos(bool soloActivos = true)
        {
            return _datos.ListarBancos(soloActivos);
        }

        public List<CuentaBancariaTesoreria> ListarCuentasBancarias(int? idBanco = null, bool soloActivas = true)
        {
            return _datos.ListarCuentasBancarias(idBanco, soloActivas);
        }

        private static string ValidarPrestamo(ObligacionFinanciera? obligacion, bool requiereCronograma)
        {
            if (obligacion == null)
                return "Debe ingresar los datos del prestamo.";

            if (obligacion.IdEntidadFinanciera <= 0)
                return "Debe seleccionar una entidad financiera.";

            if (!string.IsNullOrWhiteSpace(obligacion.CodigoTipoObligacionFinanciera)
                && !obligacion.CodigoTipoObligacionFinanciera.Equals("PRESTAMO", StringComparison.OrdinalIgnoreCase))
                return "El tipo de obligacion debe ser PRESTAMO.";

            string tipoTitular = obligacion.TipoTitular?.Trim().ToUpperInvariant() ?? string.Empty;
            if (tipoTitular is not ("EMPRESA" or "PERSONA" or "OTRO"))
                return "El tipo de titular debe ser EMPRESA, PERSONA u OTRO.";

            if (tipoTitular == "EMPRESA" && obligacion.IdTitular.HasValue && obligacion.IdTitular.Value <= 0)
                return "La empresa titular seleccionada no es valida.";

            if (obligacion.IdEmpresaResponsable.HasValue && obligacion.IdEmpresaResponsable.Value <= 0)
                return "La empresa responsable seleccionada no es valida.";

            if (string.IsNullOrWhiteSpace(obligacion.NombreTitular) && !(tipoTitular == "EMPRESA" && obligacion.IdTitular.HasValue))
                return "Debe indicar el titular del prestamo.";

            if (string.IsNullOrWhiteSpace(obligacion.Concepto))
                return "Debe ingresar el concepto del prestamo.";

            string moneda = obligacion.Moneda?.Trim().ToUpperInvariant() ?? string.Empty;
            if (moneda is not ("PEN" or "USD"))
                return "La moneda debe ser PEN o USD.";

            if (obligacion.MontoOriginal.HasValue && obligacion.MontoOriginal.Value <= 0)
                return "El monto original debe ser mayor a cero.";

            if (obligacion.FechaInicio == default)
                return "Debe ingresar la fecha de inicio.";

            if (obligacion.TotalCuotas <= 0)
                return "El total de cuotas debe ser mayor a cero.";

            if (!requiereCronograma)
                return string.Empty;

            if (obligacion.Cuotas == null || obligacion.Cuotas.Count == 0)
                return "Debe registrar el cronograma de cuotas.";

            if (obligacion.Cuotas.Count != obligacion.TotalCuotas)
                return "La cantidad de cuotas debe coincidir con el total de cuotas.";

            foreach (ObligacionFinancieraCuota cuota in obligacion.Cuotas)
            {
                if (cuota.NumeroCuota <= 0 || cuota.TotalCuotas <= 0 || cuota.NumeroCuota > cuota.TotalCuotas || cuota.TotalCuotas != obligacion.TotalCuotas)
                    return "La numeracion de cuotas no es valida.";

                if (cuota.FechaVencimiento == default)
                    return "Todas las cuotas deben tener fecha de vencimiento.";

                if (cuota.FechaVencimiento.Date < obligacion.FechaInicio.Date)
                    return "La fecha de vencimiento no puede ser anterior a la fecha de inicio.";

                if (cuota.Importe <= 0)
                    return "Los importes de las cuotas deben ser mayores a cero.";
            }

            bool numeroDuplicado = obligacion.Cuotas
                .GroupBy(c => c.NumeroCuota)
                .Any(g => g.Count() > 1);

            if (numeroDuplicado)
                return "No se puede duplicar el numero de cuota dentro del prestamo.";

            return string.Empty;
        }

        private static string ValidarPago(ObligacionFinancieraPago? pago)
        {
            if (pago == null)
                return "Debe ingresar los datos del pago.";

            if (pago.IdCuota <= 0)
                return "Debe seleccionar una cuota valida.";

            if (pago.FechaPago == default)
                return "Debe ingresar la fecha de pago.";

            if (pago.Monto <= 0)
                return "El monto del pago debe ser mayor a cero.";

            if (pago.IdBanco.HasValue && pago.IdBanco.Value <= 0)
                return "El banco seleccionado no es valido.";

            if (pago.IdCuentaBancaria.HasValue && pago.IdCuentaBancaria.Value <= 0)
                return "La cuenta bancaria seleccionada no es valida.";

            return string.Empty;
        }

        private static void NormalizarPrestamo(ObligacionFinanciera obligacion, bool normalizarCronograma)
        {
            obligacion.CodigoTipoObligacionFinanciera = "PRESTAMO";
            obligacion.TipoTitular = string.IsNullOrWhiteSpace(obligacion.TipoTitular) ? "EMPRESA" : obligacion.TipoTitular.Trim().ToUpperInvariant();
            obligacion.NombreTitular = obligacion.NombreTitular?.Trim() ?? string.Empty;
            obligacion.CodigoReferencia = obligacion.CodigoReferencia?.Trim() ?? string.Empty;
            obligacion.NumeroContrato = obligacion.NumeroContrato?.Trim() ?? string.Empty;
            obligacion.NumeroOperacion = obligacion.NumeroOperacion?.Trim() ?? string.Empty;
            obligacion.Concepto = obligacion.Concepto?.Trim() ?? string.Empty;
            obligacion.Moneda = string.IsNullOrWhiteSpace(obligacion.Moneda) ? "PEN" : obligacion.Moneda.Trim().ToUpperInvariant();
            obligacion.MontoOriginal = obligacion.MontoOriginal.HasValue ? Math.Round(obligacion.MontoOriginal.Value, 2) : null;
            obligacion.FechaInicio = obligacion.FechaInicio.Date;
            obligacion.Observacion = obligacion.Observacion?.Trim() ?? string.Empty;

            if (!normalizarCronograma)
                return;

            foreach (ObligacionFinancieraCuota cuota in obligacion.Cuotas)
            {
                cuota.FechaVencimiento = cuota.FechaVencimiento.Date;
                cuota.Importe = Math.Round(cuota.Importe, 2);
                cuota.TotalCuotas = obligacion.TotalCuotas;
                cuota.Observacion = cuota.Observacion?.Trim() ?? string.Empty;
            }
        }

        private static ObligacionFinancieraResultado Resultado(int id, string mensaje) => new()
        {
            IdObligacionFinanciera = id,
            Resultado = false,
            Mensaje = mensaje
        };

        private static string Usuario(string usuario) => string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim();
    }
}
