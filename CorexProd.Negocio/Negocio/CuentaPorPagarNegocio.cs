using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class CuentaPorPagarNegocio
    {
        private readonly CuentaPorPagarDatos _datos = new();
        private const decimal ToleranciaImporte = 0.01m;

        public CuentaPorPagarResultado Guardar(CuentaPorPagar cuenta, string usuario)
        {
            string validacion = ValidarCuenta(cuenta);
            if (!string.IsNullOrWhiteSpace(validacion))
            {
                return new CuentaPorPagarResultado
                {
                    IdCuentaPorPagar = cuenta?.IdCuentaPorPagar ?? 0,
                    Resultado = false,
                    Mensaje = validacion
                };
            }

            NormalizarCuenta(cuenta);
            return _datos.Guardar(cuenta, Usuario(usuario));
        }

        public List<CuentaPorPagarListado> Listar(DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? idProveedor = null, string? estado = null, string? texto = null)
        {
            return _datos.Listar(fechaDesde, fechaHasta, idProveedor, estado, texto);
        }

        public CuentaPorPagar? Obtener(int idCuentaPorPagar)
        {
            return idCuentaPorPagar <= 0 ? null : _datos.Obtener(idCuentaPorPagar);
        }

        public List<TipoObligacion> ListarTiposObligacion(bool soloActivos = true)
        {
            return _datos.ListarTiposObligacion(soloActivos);
        }

        public List<TipoDocumentoStock> ListarTiposDocumento()
        {
            return _datos.ListarTiposDocumento();
        }

        public List<BancoTesoreria> ListarBancos(bool soloActivos = true)
        {
            return _datos.ListarBancos(soloActivos);
        }

        public List<CuentaBancariaTesoreria> ListarCuentasBancarias(int? idBanco = null, bool soloActivas = true)
        {
            return _datos.ListarCuentasBancarias(idBanco, soloActivas);
        }

        public List<CuentaPorPagarProgramacion> ObtenerProgramacion(DateTime fechaDesde, DateTime fechaHasta, int? idProveedor = null, string? estado = null)
        {
            if (fechaDesde == default || fechaHasta == default || fechaHasta.Date < fechaDesde.Date)
                return [];

            return _datos.ObtenerProgramacion(fechaDesde, fechaHasta, idProveedor, estado);
        }

        public CuentaPorPagarResultado Anular(int idCuentaPorPagar, string usuario, string motivo)
        {
            if (idCuentaPorPagar <= 0)
            {
                return new CuentaPorPagarResultado
                {
                    IdCuentaPorPagar = idCuentaPorPagar,
                    Resultado = false,
                    Mensaje = "Debe seleccionar una cuenta por pagar valida."
                };
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return new CuentaPorPagarResultado
                {
                    IdCuentaPorPagar = idCuentaPorPagar,
                    Resultado = false,
                    Mensaje = "Debe ingresar el motivo de anulacion."
                };
            }

            return _datos.Anular(idCuentaPorPagar, Usuario(usuario), motivo.Trim());
        }

        public CuentaPorPagarPagoResultado RegistrarPago(CuentaPorPagarPago pago, string usuario)
        {
            string validacion = ValidarPago(pago);
            if (!string.IsNullOrWhiteSpace(validacion))
            {
                return new CuentaPorPagarPagoResultado
                {
                    IdPago = pago?.IdPago ?? 0,
                    Resultado = false,
                    Mensaje = validacion
                };
            }

            pago.FechaPago = pago.FechaPago.Date;
            pago.Importe = Math.Round(pago.Importe, 2);
            pago.NumeroOperacion = pago.NumeroOperacion?.Trim() ?? string.Empty;
            pago.Observacion = pago.Observacion?.Trim() ?? string.Empty;

            return _datos.RegistrarPago(pago, Usuario(usuario));
        }

        public CuentaPorPagarPagoResultado AnularPago(int idPago, string motivo, string usuario)
        {
            if (idPago <= 0)
            {
                return new CuentaPorPagarPagoResultado
                {
                    IdPago = idPago,
                    Resultado = false,
                    Mensaje = "Debe seleccionar un pago valido."
                };
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return new CuentaPorPagarPagoResultado
                {
                    IdPago = idPago,
                    Resultado = false,
                    Mensaje = "Debe ingresar el motivo de anulacion del pago."
                };
            }

            return _datos.AnularPago(idPago, motivo.Trim(), Usuario(usuario));
        }

        private static string ValidarPago(CuentaPorPagarPago? pago)
        {
            if (pago == null)
                return "Debe ingresar los datos del pago.";

            if (pago.IdCuota <= 0)
                return "Debe seleccionar una cuota valida.";

            if (pago.FechaPago == default)
                return "Debe ingresar la fecha de pago.";

            if (pago.Importe <= 0)
                return "El importe del pago debe ser mayor a cero.";

            if (pago.IdCuentaBancaria.HasValue && pago.IdCuentaBancaria.Value <= 0)
                return "La cuenta bancaria seleccionada no es valida.";

            return string.Empty;
        }

        private static string ValidarCuenta(CuentaPorPagar? cuenta)
        {
            if (cuenta == null)
                return "Debe ingresar los datos de la cuenta por pagar.";

            if (cuenta.IdProveedor <= 0)
                return "Debe seleccionar un proveedor.";

            if (cuenta.IdTipoObligacion <= 0)
                return "Debe seleccionar un tipo de obligacion.";

            if (cuenta.FechaDocumento == default)
                return "Debe ingresar la fecha del documento.";

            string moneda = cuenta.Moneda?.Trim().ToUpperInvariant() ?? string.Empty;
            if (moneda is not ("PEN" or "USD" or "EUR"))
                return "La moneda debe ser PEN, USD o EUR.";

            if (cuenta.ImporteTotal <= 0)
                return "El importe total debe ser mayor a cero.";

            if (cuenta.Documentos == null || cuenta.Documentos.Count == 0)
                return "Debe registrar al menos un documento.";

            if (cuenta.Cuotas == null || cuenta.Cuotas.Count == 0)
                return "Debe registrar al menos una cuota.";

            foreach (CuentaPorPagarDocumento documento in cuenta.Documentos)
            {
                if (documento.IdTipoDocumento <= 0)
                    return "Todos los documentos deben tener un tipo de documento.";

                if (documento.FechaEmision == default)
                    return "Todos los documentos deben tener fecha de emision.";

                if (documento.Importe <= 0)
                    return "Los importes de los documentos deben ser mayores a cero.";

                if (documento.FactorEfecto is not (1 or -1))
                    return "El efecto de los documentos debe ser positivo o nota de credito.";
            }

            decimal totalDocumentosPositivos = Math.Round(cuenta.Documentos.Where(d => d.FactorEfecto == 1).Sum(d => d.Importe), 2);
            decimal totalNotasCredito = Math.Round(cuenta.Documentos.Where(d => d.FactorEfecto == -1).Sum(d => d.Importe), 2);
            decimal totalNeto = Math.Round(totalDocumentosPositivos - totalNotasCredito, 2);

            if (totalDocumentosPositivos <= 0)
                return "Debe registrar al menos una factura o documento positivo.";

            if (totalNotasCredito > totalDocumentosPositivos)
                return "El total de notas de credito no puede ser mayor al total de facturas.";

            if (totalNeto <= 0)
                return "El total neto por pagar debe ser mayor a cero.";

            if (Math.Abs(Math.Round(cuenta.ImporteTotal, 2) - totalNeto) > ToleranciaImporte)
                return "El importe total debe ser igual al total neto documental.";

            bool esFacturaCredito = EsFacturaCredito(cuenta);

            foreach (CuentaPorPagarCuota cuota in cuenta.Cuotas)
            {
                if (cuota.NumeroCuota <= 0 || cuota.TotalCuotas <= 0 || cuota.NumeroCuota > cuota.TotalCuotas)
                    return "La numeracion de cuotas no es valida.";

                if (cuota.Importe <= 0)
                    return "Los importes de las cuotas deben ser mayores a cero.";

                if (!esFacturaCredito && string.IsNullOrWhiteSpace(cuota.NumeroLetra))
                    return "El numero de letra es obligatorio para Letras por Pagar.";

                if (!esFacturaCredito && cuota.FechaGiro == null)
                    return "Todas las cuotas deben tener fecha de giro.";

                if (cuota.FechaVencimiento == default)
                    return "Todas las cuotas deben tener fecha de vencimiento.";

                if (cuota.FechaGiro.HasValue && cuota.FechaVencimiento.Date < cuota.FechaGiro.Value.Date)
                    return "La fecha de vencimiento no puede ser anterior a la fecha de giro.";
            }

            if (!esFacturaCredito)
            {
                bool letraDuplicada = cuenta.Cuotas
                    .Where(c => !string.IsNullOrWhiteSpace(c.NumeroLetra))
                    .GroupBy(c => c.NumeroLetra.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);

                if (letraDuplicada)
                    return "No se puede duplicar el numero de letra dentro de la misma cuenta.";
            }

            decimal sumaCuotas = Math.Round(cuenta.Cuotas.Sum(c => c.Importe), 2);
            if (Math.Abs(sumaCuotas - totalNeto) > ToleranciaImporte)
                return "La suma de cuotas debe ser igual al total neto por pagar.";

            return string.Empty;
        }

        private static void NormalizarCuenta(CuentaPorPagar cuenta)
        {
            cuenta.Moneda = string.IsNullOrWhiteSpace(cuenta.Moneda) ? "PEN" : cuenta.Moneda.Trim().ToUpperInvariant();
            cuenta.OrigenTipo = string.IsNullOrWhiteSpace(cuenta.OrigenTipo) ? "MANUAL" : cuenta.OrigenTipo.Trim().ToUpperInvariant();
            cuenta.Observacion = cuenta.Observacion?.Trim() ?? string.Empty;
            cuenta.ImporteTotal = Math.Round(cuenta.TotalNetoDocumental, 2);
            bool esFacturaCredito = EsFacturaCredito(cuenta);

            foreach (CuentaPorPagarDocumento documento in cuenta.Documentos)
            {
                documento.Serie = documento.Serie?.Trim() ?? string.Empty;
                documento.Numero = documento.Numero?.Trim() ?? string.Empty;
                documento.NumeroDocumento = string.IsNullOrWhiteSpace(documento.NumeroDocumento)
                    ? UnirNumeroDocumento(documento.Serie, documento.Numero)
                    : documento.NumeroDocumento.Trim();
                documento.Observacion = documento.Observacion?.Trim() ?? string.Empty;
                documento.Importe = Math.Round(documento.Importe, 2);
                documento.FactorEfecto = documento.FactorEfecto == -1 ? (short)-1 : (short)1;
            }

            foreach (CuentaPorPagarCuota cuota in cuenta.Cuotas)
            {
                cuota.NumeroLetra = cuota.NumeroLetra?.Trim() ?? string.Empty;
                cuota.TipoCuota = esFacturaCredito ? "CUOTA_FACTURA" : "LETRA";
                if (!cuota.FechaGiro.HasValue)
                    cuota.FechaGiro = cuota.FechaVencimiento;
                cuota.Observacion = cuota.Observacion?.Trim() ?? string.Empty;
                cuota.Importe = Math.Round(cuota.Importe, 2);
            }
        }

        private static bool EsFacturaCredito(CuentaPorPagar cuenta)
        {
            return cuenta.CodigoTipoObligacion.Equals("FACTURA_CREDITO", StringComparison.OrdinalIgnoreCase)
                || cuenta.TipoObligacion.Equals("Factura a credito", StringComparison.OrdinalIgnoreCase)
                || cuenta.TipoObligacion.Equals("Factura a crédito", StringComparison.OrdinalIgnoreCase);
        }

        private static string UnirNumeroDocumento(string serie, string numero)
        {
            serie = serie?.Trim() ?? string.Empty;
            numero = numero?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(serie))
                return numero;
            if (string.IsNullOrWhiteSpace(numero))
                return serie;
            return $"{serie}-{numero}";
        }

        private static string Usuario(string usuario) => string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim();
    }
}
