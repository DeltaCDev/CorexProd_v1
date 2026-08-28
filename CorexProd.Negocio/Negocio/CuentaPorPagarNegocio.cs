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
            }

            foreach (CuentaPorPagarCuota cuota in cuenta.Cuotas)
            {
                if (cuota.NumeroCuota <= 0 || cuota.TotalCuotas <= 0 || cuota.NumeroCuota > cuota.TotalCuotas)
                    return "La numeracion de cuotas no es valida.";

                if (cuota.Importe <= 0)
                    return "Los importes de las cuotas deben ser mayores a cero.";

                if (cuota.FechaGiro == default)
                    return "Todas las cuotas deben tener fecha de giro.";

                if (cuota.FechaVencimiento == default)
                    return "Todas las cuotas deben tener fecha de vencimiento.";

                if (cuota.FechaVencimiento.Date < cuota.FechaGiro.Date)
                    return "La fecha de vencimiento no puede ser anterior a la fecha de giro.";
            }

            bool letraDuplicada = cuenta.Cuotas
                .Where(c => !string.IsNullOrWhiteSpace(c.NumeroLetra))
                .GroupBy(c => c.NumeroLetra.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1);

            if (letraDuplicada)
                return "No se puede duplicar el numero de letra dentro de la misma cuenta.";

            decimal sumaCuotas = Math.Round(cuenta.Cuotas.Sum(c => c.Importe), 2);
            decimal importeTotal = Math.Round(cuenta.ImporteTotal, 2);
            if (Math.Abs(sumaCuotas - importeTotal) > ToleranciaImporte)
                return "La suma de cuotas debe ser igual al importe total.";

            return string.Empty;
        }

        private static void NormalizarCuenta(CuentaPorPagar cuenta)
        {
            cuenta.Moneda = string.IsNullOrWhiteSpace(cuenta.Moneda) ? "PEN" : cuenta.Moneda.Trim().ToUpperInvariant();
            cuenta.OrigenTipo = string.IsNullOrWhiteSpace(cuenta.OrigenTipo) ? "MANUAL" : cuenta.OrigenTipo.Trim().ToUpperInvariant();
            cuenta.Observacion = cuenta.Observacion?.Trim() ?? string.Empty;
            cuenta.ImporteTotal = Math.Round(cuenta.ImporteTotal, 2);

            foreach (CuentaPorPagarDocumento documento in cuenta.Documentos)
            {
                documento.Serie = documento.Serie?.Trim() ?? string.Empty;
                documento.Numero = documento.Numero?.Trim() ?? string.Empty;
                documento.NumeroDocumento = string.IsNullOrWhiteSpace(documento.NumeroDocumento)
                    ? UnirNumeroDocumento(documento.Serie, documento.Numero)
                    : documento.NumeroDocumento.Trim();
                documento.Observacion = documento.Observacion?.Trim() ?? string.Empty;
                documento.Importe = Math.Round(documento.Importe, 2);
            }

            foreach (CuentaPorPagarCuota cuota in cuenta.Cuotas)
            {
                cuota.NumeroLetra = cuota.NumeroLetra?.Trim() ?? string.Empty;
                cuota.Observacion = cuota.Observacion?.Trim() ?? string.Empty;
                cuota.Importe = Math.Round(cuota.Importe, 2);
            }
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
