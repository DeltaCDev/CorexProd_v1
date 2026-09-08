using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class TarjetaCreditoNegocio
    {
        private readonly TarjetaCreditoDatos _datos = new();

        public TarjetaCreditoResultado GuardarTarjeta(TarjetaCredito? tarjeta, string usuario)
        {
            if (tarjeta == null)
                return ResultadoTarjeta(0, "Debe ingresar los datos de la tarjeta.");

            List<string> monedas = NormalizarMonedas(tarjeta?.MonedasHabilitadas, out string validacionMonedas);
            if (!string.IsNullOrWhiteSpace(validacionMonedas))
                return ResultadoTarjeta(tarjeta?.IdTarjetaCredito ?? 0, validacionMonedas);

            string validacion = ValidarTarjeta(tarjeta);
            if (!string.IsNullOrWhiteSpace(validacion))
                return ResultadoTarjeta(tarjeta?.IdTarjetaCredito ?? 0, validacion);

            if (tarjeta!.IdTarjetaCredito > 0)
            {
                TarjetaCredito? existente = ObtenerTarjeta(tarjeta.IdTarjetaCredito);
                if (existente != null)
                {
                    foreach (string monedaRetirada in existente.MonedasHabilitadas.Where(moneda => !monedas.Contains(moneda)))
                    {
                        bool tienePeriodos = ListarPeriodos(tarjeta.IdTarjetaCredito).Any(periodo =>
                            monedaRetirada == "PEN" ? periodo.ImportePEN.HasValue : periodo.ImporteUSD.HasValue);
                        if (tienePeriodos)
                            return ResultadoTarjeta(tarjeta.IdTarjetaCredito, $"No se puede retirar la moneda {monedaRetirada} porque tiene periodos asociados.");
                    }
                }
            }

            tarjeta.MonedasHabilitadas = monedas;
            tarjeta.Moneda = monedas.Contains("PEN") ? "PEN" : "USD";
            NormalizarTarjeta(tarjeta);
            return _datos.GuardarTarjeta(tarjeta, Usuario(usuario), monedas);
        }

        public List<TarjetaCredito> ListarTarjetas(string? texto = null, string? estado = null, int? idEntidadFinanciera = null, string? moneda = null)
        {
            return _datos.ListarTarjetas(texto, estado, idEntidadFinanciera, moneda);
        }

        public TarjetaCredito? ObtenerTarjeta(int idTarjetaCredito)
        {
            return idTarjetaCredito <= 0 ? null : _datos.ObtenerTarjeta(idTarjetaCredito);
        }

        public TarjetaCreditoResultado AnularTarjeta(int idTarjetaCredito, string usuario, string motivo)
        {
            if (idTarjetaCredito <= 0)
                return ResultadoTarjeta(idTarjetaCredito, "Debe seleccionar una tarjeta de credito valida.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoTarjeta(idTarjetaCredito, "Debe ingresar el motivo de anulacion.");

            return _datos.AnularTarjeta(idTarjetaCredito, Usuario(usuario), motivo.Trim());
        }

        public List<TarjetaCreditoPeriodo> ListarPeriodos(int? idTarjetaCredito = null, short? anio = null, string? estado = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            return _datos.ListarPeriodos(idTarjetaCredito, anio, estado, fechaDesde, fechaHasta);
        }

        public TarjetaCreditoPeriodo? ObtenerPeriodo(int idTarjetaPeriodo)
        {
            return idTarjetaPeriodo <= 0 ? null : _datos.ObtenerPeriodo(idTarjetaPeriodo);
        }

        public TarjetaCreditoPeriodoResultado GuardarPeriodo(TarjetaCreditoPeriodo periodo, string usuario)
        {
            string validacion = ValidarPeriodo(periodo);
            if (!string.IsNullOrWhiteSpace(validacion))
                return ResultadoPeriodo(periodo?.IdTarjetaPeriodo ?? 0, validacion);

            TarjetaCredito? tarjeta = ObtenerTarjeta(periodo.IdTarjetaCredito);
            if (tarjeta == null)
                return ResultadoPeriodo(periodo.IdTarjetaPeriodo, "La tarjeta de credito no existe.");

            if (tarjeta.EstaAnulada)
                return ResultadoPeriodo(periodo.IdTarjetaPeriodo, "La tarjeta se encuentra anulada y no permite operaciones.");

            if (periodo.IdTarjetaPeriodo <= 0 && tarjeta.EstaInactiva)
                return ResultadoPeriodo(0, "La tarjeta se encuentra inactiva y no permite registrar nuevos periodos.");

            if (periodo.ImportePEN.HasValue && !tarjeta.MonedasHabilitadas.Contains("PEN"))
                return ResultadoPeriodo(periodo.IdTarjetaPeriodo, "La tarjeta no tiene PEN habilitado.");

            if (periodo.ImporteUSD.HasValue && !tarjeta.MonedasHabilitadas.Contains("USD"))
                return ResultadoPeriodo(periodo.IdTarjetaPeriodo, "La tarjeta no tiene USD habilitado.");

            NormalizarPeriodo(periodo);
            return _datos.GuardarPeriodo(periodo, Usuario(usuario));
        }

        public TarjetaCreditoPeriodoResultado AnularPeriodo(int idTarjetaPeriodo, string usuario, string motivo)
        {
            if (idTarjetaPeriodo <= 0)
                return ResultadoPeriodo(idTarjetaPeriodo, "Debe seleccionar un periodo de tarjeta valido.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoPeriodo(idTarjetaPeriodo, "Debe ingresar el motivo de anulacion del periodo.");

            return _datos.AnularPeriodo(idTarjetaPeriodo, Usuario(usuario), motivo.Trim());
        }

        public TarjetaCreditoPagoResultado RegistrarPago(TarjetaCreditoPago pago, string usuario)
        {
            string validacion = ValidarPago(pago);
            if (!string.IsNullOrWhiteSpace(validacion))
                return ResultadoPago(pago?.IdPago ?? 0, validacion);

            TarjetaCreditoPeriodo? periodo = ObtenerPeriodo(pago.IdTarjetaPeriodo);
            if (periodo == null)
                return ResultadoPago(pago.IdPago, "El periodo de tarjeta no existe.");

            if (periodo.EstaAbierto)
                return ResultadoPago(pago.IdPago, "No se puede registrar pagos sobre un periodo abierto.");

            if (periodo.EstaAnulado)
                return ResultadoPago(pago.IdPago, "No se puede registrar pagos sobre un periodo anulado.");

            pago.Moneda = pago.Moneda?.Trim().ToUpperInvariant();
            if (pago.Moneda is not ("PEN" or "USD"))
                return ResultadoPago(pago.IdPago, "La moneda del pago debe ser PEN o USD.");

            decimal? saldoMoneda = pago.Moneda == "PEN" ? periodo.SaldoPendientePEN : periodo.SaldoPendienteUSD;
            decimal? importeMoneda = pago.Moneda == "PEN" ? periodo.ImportePEN : periodo.ImporteUSD;
            if (!importeMoneda.HasValue || !saldoMoneda.HasValue || saldoMoneda.Value <= 0)
                return ResultadoPago(pago.IdPago, "El periodo no tiene saldo pendiente para la moneda indicada.");

            if (pago.Importe > saldoMoneda.Value)
                return ResultadoPago(pago.IdPago, "El importe del pago no puede superar el saldo pendiente de la moneda.");

            pago.FechaPago = pago.FechaPago.Date;
            pago.Importe = Math.Round(pago.Importe, 2);
            pago.NumeroOperacion = pago.NumeroOperacion?.Trim() ?? string.Empty;
            pago.Observacion = pago.Observacion?.Trim() ?? string.Empty;

            return _datos.RegistrarPago(pago, Usuario(usuario));
        }

        public TarjetaCreditoPagoResultado AnularPago(int idPago, string usuario, string motivo)
        {
            if (idPago <= 0)
                return ResultadoPago(idPago, "Debe seleccionar un pago valido.");

            if (string.IsNullOrWhiteSpace(motivo))
                return ResultadoPago(idPago, "Debe ingresar el motivo de anulacion del pago.");

            return _datos.AnularPago(idPago, Usuario(usuario), motivo.Trim());
        }

        public List<TarjetaCreditoHistorial> ListarHistorial(int idTarjetaCredito)
        {
            return idTarjetaCredito <= 0 ? [] : _datos.ListarHistorial(idTarjetaCredito);
        }

        public List<EntidadFinanciera> ListarEntidadesFinancieras(string? tipoEntidad = null)
        {
            return _datos.ListarEntidadesFinancieras(tipoEntidad);
        }

        public List<BancoTesoreria> ListarBancos(bool soloActivos = true)
        {
            return _datos.ListarBancos(soloActivos);
        }

        public List<CuentaBancariaTesoreria> ListarCuentasBancarias(int? idBanco = null, bool soloActivas = true)
        {
            return _datos.ListarCuentasBancarias(idBanco, soloActivas);
        }

        public List<Empresa> ListarEmpresas()
        {
            return _datos.ListarEmpresas();
        }

        private static string ValidarTarjeta(TarjetaCredito? tarjeta)
        {
            if (tarjeta == null)
                return "Debe ingresar los datos de la tarjeta.";

            if (tarjeta.IdEntidadFinanciera <= 0)
                return "Debe seleccionar una entidad financiera.";

            if (string.IsNullOrWhiteSpace(tarjeta.Titular))
                return "Debe indicar el titular de la tarjeta.";

            if (tarjeta.IdEmpresaResponsable.HasValue && tarjeta.IdEmpresaResponsable.Value <= 0)
                return "La empresa responsable seleccionada no es valida.";

            if (string.IsNullOrWhiteSpace(tarjeta.EmpresaResponsable) && !tarjeta.IdEmpresaResponsable.HasValue)
                return "Debe indicar la empresa responsable del pago.";

            if (string.IsNullOrWhiteSpace(tarjeta.Alias))
                return "Debe indicar el alias de la tarjeta.";

            string ultimos4 = tarjeta.Ultimos4?.Trim() ?? string.Empty;
            if (ultimos4.Length > 0 && (ultimos4.Length != 4 || !SoloDigitos(ultimos4)))
                return "Ultimos4 debe contener exactamente cuatro digitos.";

            if (tarjeta.DiaCorte.HasValue && (tarjeta.DiaCorte.Value < 1 || tarjeta.DiaCorte.Value > 31))
                return "El dia de corte debe estar entre 1 y 31.";

            if (tarjeta.DiaPago.HasValue && (tarjeta.DiaPago.Value < 1 || tarjeta.DiaPago.Value > 31))
                return "El dia de pago debe estar entre 1 y 31.";

            string estado = tarjeta.Estado?.Trim().ToUpperInvariant() ?? string.Empty;
            if (estado is not ("ACTIVA" or "INACTIVA" or "ANULADA"))
                return "El estado de la tarjeta debe ser ACTIVA, INACTIVA o ANULADA.";

            if (tarjeta.IdTarjetaCredito <= 0 && estado == "ANULADA")
                return "No se puede crear una tarjeta directamente en estado ANULADA.";

            return string.Empty;
        }

        private static string ValidarPeriodo(TarjetaCreditoPeriodo? periodo)
        {
            if (periodo == null)
                return "Debe ingresar los datos del periodo.";

            if (periodo.IdTarjetaCredito <= 0)
                return "Debe seleccionar una tarjeta de credito.";

            if (periodo.PeriodoAnio < 2000 || periodo.PeriodoAnio > 2100 || periodo.PeriodoMes < 1 || periodo.PeriodoMes > 12)
                return "El periodo de tarjeta no es valido.";

            if (periodo.FechaCorte == default)
                return "Debe ingresar la fecha de corte.";

            if (periodo.FechaVencimiento == default)
                return "Debe ingresar la fecha de vencimiento.";

            if (periodo.FechaVencimiento.Date < periodo.FechaCorte.Date)
                return "La fecha de vencimiento debe ser mayor o igual a la fecha de corte.";

            string estado = periodo.Estado?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(estado))
                estado = periodo.ImportePEN.HasValue || periodo.ImporteUSD.HasValue ? "PENDIENTE" : "ABIERTO";

            if (estado is not ("ABIERTO" or "PENDIENTE" or "PARCIAL" or "CANCELADA"))
                return "El estado del periodo debe ser ABIERTO, PENDIENTE, PARCIAL o CANCELADA.";

            if (periodo.IdTarjetaPeriodo <= 0 && estado is "PARCIAL" or "CANCELADA")
                return "Un periodo nuevo debe iniciar como ABIERTO o PENDIENTE.";

            if (periodo.ImportePEN.HasValue && periodo.ImportePEN.Value < 0 || periodo.ImporteUSD.HasValue && periodo.ImporteUSD.Value < 0)
                return "Los importes por moneda no pueden ser negativos.";

            if (estado == "ABIERTO" && (periodo.ImportePEN.HasValue || periodo.ImporteUSD.HasValue))
                return "Un periodo ABIERTO no debe tener monto definitivo.";

            if (estado is "PENDIENTE" or "PARCIAL" or "CANCELADA")
            {
                if (!periodo.ImportePEN.HasValue && !periodo.ImporteUSD.HasValue)
                    return "Un periodo exigible debe tener al menos un importe.";
            }

            return string.Empty;
        }

        private static string ValidarPago(TarjetaCreditoPago? pago)
        {
            if (pago == null)
                return "Debe ingresar los datos del pago.";

            if (pago.IdTarjetaPeriodo <= 0)
                return "Debe seleccionar un periodo de tarjeta valido.";

            if (pago.FechaPago == default)
                return "Debe ingresar la fecha de pago.";

            if (pago.Importe <= 0)
                return "El importe del pago debe ser mayor a cero.";

            if (pago.IdBanco.HasValue && pago.IdBanco.Value <= 0)
                return "El banco seleccionado no es valido.";

            if (pago.IdCuentaBancaria.HasValue && pago.IdCuentaBancaria.Value <= 0)
                return "La cuenta bancaria seleccionada no es valida.";

            return string.Empty;
        }

        private static void NormalizarTarjeta(TarjetaCredito tarjeta)
        {
            tarjeta.Titular = tarjeta.Titular?.Trim() ?? string.Empty;
            tarjeta.EmpresaResponsable = tarjeta.EmpresaResponsable?.Trim() ?? string.Empty;
            tarjeta.Alias = tarjeta.Alias?.Trim() ?? string.Empty;
            tarjeta.Ultimos4 = tarjeta.Ultimos4?.Trim() ?? string.Empty;
            tarjeta.Moneda = string.IsNullOrWhiteSpace(tarjeta.Moneda) ? "PEN" : tarjeta.Moneda.Trim().ToUpperInvariant();
            tarjeta.Estado = string.IsNullOrWhiteSpace(tarjeta.Estado) ? "ACTIVA" : tarjeta.Estado.Trim().ToUpperInvariant();
            tarjeta.Activo = tarjeta.Estado == "ACTIVA";
            tarjeta.Observacion = tarjeta.Observacion?.Trim() ?? string.Empty;
        }

        private static void NormalizarPeriodo(TarjetaCreditoPeriodo periodo)
        {
            periodo.FechaCorte = periodo.FechaCorte.Date;
            periodo.FechaVencimiento = periodo.FechaVencimiento.Date;
            periodo.ImportePEN = periodo.ImportePEN.HasValue ? Math.Round(periodo.ImportePEN.Value, 2) : null;
            periodo.ImporteUSD = periodo.ImporteUSD.HasValue ? Math.Round(periodo.ImporteUSD.Value, 2) : null;
            periodo.Estado = string.IsNullOrWhiteSpace(periodo.Estado)
                ? (periodo.ImportePEN.HasValue || periodo.ImporteUSD.HasValue ? "PENDIENTE" : "ABIERTO")
                : periodo.Estado.Trim().ToUpperInvariant();
            periodo.Observacion = periodo.Observacion?.Trim() ?? string.Empty;
        }

        private static TarjetaCreditoResultado ResultadoTarjeta(int id, string mensaje) => new()
        {
            IdTarjetaCredito = id,
            Resultado = false,
            Mensaje = mensaje
        };

        private static TarjetaCreditoPeriodoResultado ResultadoPeriodo(int id, string mensaje) => new()
        {
            IdTarjetaPeriodo = id,
            Resultado = false,
            Mensaje = mensaje
        };

        private static TarjetaCreditoPagoResultado ResultadoPago(int id, string mensaje) => new()
        {
            IdPago = id,
            Resultado = false,
            Mensaje = mensaje
        };

        private static bool SoloDigitos(string valor)
        {
            foreach (char c in valor)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            return true;
        }

        private static List<string> NormalizarMonedas(IEnumerable<string>? monedasOrigen, out string mensaje)
        {
            mensaje = string.Empty;
            List<string> monedas = (monedasOrigen ?? [])
                .Where(moneda => !string.IsNullOrWhiteSpace(moneda))
                .Select(moneda => moneda.Trim().ToUpperInvariant())
                .Distinct()
                .OrderBy(moneda => moneda)
                .ToList();

            if (monedas.Count is < 1 or > 2 || monedas.Any(moneda => moneda is not ("PEN" or "USD")))
                mensaje = "Debe indicar una o dos monedas habilitadas: PEN y/o USD.";

            return monedas;
        }

        private static string Usuario(string usuario) => string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim();
    }
}
