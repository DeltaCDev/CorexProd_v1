using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorexProd.Negocio.Negocio
{
    public class SeguroNegocio
    {
        private static readonly HashSet<string> PeriodicidadesValidas =
        ["UNICO", "MENSUAL", "BIMESTRAL", "TRIMESTRAL", "SEMESTRAL", "ANUAL"];

        private readonly SeguroDatos _datos = new();
        private readonly ObligacionFinancieraDatos _obligacionDatos = new();

        public SeguroResultado Guardar(Seguro seguro, string usuario)
        {
            string validacion = ValidarSeguro(seguro);
            if (!string.IsNullOrWhiteSpace(validacion))
                return Resultado(seguro?.IdSeguro ?? 0, seguro?.IdObligacionFinanciera ?? 0, validacion);

            Normalizar(seguro);
            return _datos.Guardar(seguro, Usuario(usuario));
        }

        public SeguroResultado ActualizarSeguro(Seguro seguro, string usuario)
        {
            if (seguro == null || seguro.IdSeguro <= 0 || seguro.IdObligacionFinanciera <= 0)
                return Resultado(seguro?.IdSeguro ?? 0, seguro?.IdObligacionFinanciera ?? 0, "Debe seleccionar un seguro valido.");
            if (seguro.EstaAnulado)
                return Resultado(seguro.IdSeguro, seguro.IdObligacionFinanciera, "No se puede editar un seguro anulado.");
            string validacion = ValidarSeguro(seguro);
            if (!string.IsNullOrWhiteSpace(validacion))
                return Resultado(seguro.IdSeguro, seguro.IdObligacionFinanciera, validacion);
            Normalizar(seguro);
            return _datos.Actualizar(seguro, Usuario(usuario));
        }

        public List<SeguroResumen> Listar(string? texto = null, string? estado = null, int? idEntidadAseguradora = null)
        {
            return _datos.Listar(texto, estado, idEntidadAseguradora);
        }

        public Seguro? Obtener(int idObligacionFinanciera)
        {
            return idObligacionFinanciera <= 0 ? null : _datos.Obtener(idObligacionFinanciera);
        }

        public List<SeguroPeriodo> ListarPeriodos(int idObligacionFinanciera)
        {
            return Obtener(idObligacionFinanciera)?.Periodos ?? [];
        }

        public SeguroResultado Anular(int idObligacionFinanciera, string usuario, string motivo)
        {
            if (idObligacionFinanciera <= 0)
                return Resultado(0, idObligacionFinanciera, "Debe seleccionar un seguro valido.");
            if (string.IsNullOrWhiteSpace(motivo))
                return Resultado(0, idObligacionFinanciera, "Debe ingresar el motivo de anulacion.");
            return _datos.Anular(idObligacionFinanciera, Usuario(usuario), motivo.Trim());
        }

        public SeguroPeriodoResultado AnularPeriodo(int idCuotaObligacionFinanciera, string usuario, string motivo)
        {
            if (idCuotaObligacionFinanciera <= 0)
                return new SeguroPeriodoResultado { IdCuotaObligacionFinanciera = idCuotaObligacionFinanciera, Mensaje = "Debe seleccionar un periodo valido." };
            if (string.IsNullOrWhiteSpace(motivo))
                return new SeguroPeriodoResultado { IdCuotaObligacionFinanciera = idCuotaObligacionFinanciera, Mensaje = "Debe ingresar el motivo de anulacion del periodo." };
            return _datos.AnularPeriodo(idCuotaObligacionFinanciera, Usuario(usuario), motivo.Trim());
        }

        public List<EntidadFinanciera> ListarAseguradoras()
        {
            return _obligacionDatos.ListarEntidadesFinancieras("ASEGURADORA");
        }

        private static string ValidarSeguro(Seguro? seguro)
        {
            if (seguro == null)
                return "Debe ingresar los datos del seguro.";

            ObligacionFinanciera obligacion = seguro.Obligacion;
            if (obligacion == null || obligacion.IdEntidadFinanciera <= 0)
                return "Debe seleccionar una aseguradora.";

            string tipoTitular = obligacion.TipoTitular?.Trim().ToUpperInvariant() ?? string.Empty;
            if (tipoTitular is not ("EMPRESA" or "PERSONA" or "OTRO"))
                return "El tipo de titular debe ser EMPRESA, PERSONA u OTRO.";
            if (tipoTitular == "EMPRESA" && obligacion.IdTitular.HasValue && obligacion.IdTitular.Value <= 0)
                return "La empresa titular seleccionada no es valida.";
            if (obligacion.IdEmpresaResponsable.HasValue && obligacion.IdEmpresaResponsable.Value <= 0)
                return "La empresa responsable seleccionada no es valida.";
            if (string.IsNullOrWhiteSpace(obligacion.NombreTitular) && !(tipoTitular == "EMPRESA" && obligacion.IdTitular.HasValue))
                return "Debe indicar el titular del seguro.";
            if (string.IsNullOrWhiteSpace(obligacion.Concepto))
                return "Debe ingresar el concepto del seguro.";
            if ((obligacion.Moneda?.Trim().ToUpperInvariant() ?? string.Empty) is not ("PEN" or "USD"))
                return "La moneda debe ser PEN o USD.";
            if (!obligacion.MontoOriginal.HasValue || obligacion.MontoOriginal.Value <= 0)
                return "El monto original debe ser mayor a cero.";
            if (obligacion.FechaInicio == default)
                return "Debe ingresar la fecha de inicio.";
            if (string.IsNullOrWhiteSpace(seguro.TipoSeguro))
                return "Debe indicar el tipo de seguro.";
            if (string.IsNullOrWhiteSpace(seguro.NumeroPoliza))
                return "Debe indicar el numero de poliza.";
            if (seguro.FechaInicioCobertura == default || seguro.FechaFinCobertura == default || seguro.FechaFinCobertura.Date < seguro.FechaInicioCobertura.Date)
                return "Las fechas de cobertura no son validas.";

            string periodicidad = seguro.Periodicidad?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!PeriodicidadesValidas.Contains(periodicidad))
                return "La periodicidad del seguro no es valida.";
            if (seguro.Periodos == null || seguro.Periodos.Count == 0)
                return "Debe registrar al menos un periodo de pago.";

            bool tienePeriodoUnico = seguro.Periodos.Any(p => !p.EsCuota);
            if (tienePeriodoUnico && seguro.Periodos.Count != 1)
                return "Un periodo unico no puede coexistir con otros periodos.";
            if (periodicidad == "UNICO" && seguro.Periodos.Count != 1)
                return "La periodicidad UNICO requiere exactamente un periodo.";

            foreach (SeguroPeriodo periodo in seguro.Periodos)
            {
                if (periodo.FechaVencimiento == default || periodo.FechaVencimiento.Date < obligacion.FechaInicio.Date)
                    return "Las fechas de vencimiento de los periodos no son validas.";
                if (periodo.Importe <= 0)
                    return "Los importes de los periodos deben ser mayores a cero.";
                if (!periodo.EsCuota && (periodo.NumeroCuota != 1 || periodo.TotalCuotas != 1))
                    return "El periodo unico debe usar NumeroCuota y TotalCuotas igual a 1.";
                if (periodo.EsCuota && (periodo.NumeroCuota <= 0 || periodo.TotalCuotas <= 1 || periodo.NumeroCuota > periodo.TotalCuotas))
                    return "La numeracion de las cuotas del seguro no es valida.";
            }

            if (seguro.Periodos.Any(p => p.EsCuota))
            {
                int totalCuotas = seguro.Periodos.Max(p => p.TotalCuotas);
                if (seguro.Periodos.Count != totalCuotas || seguro.Periodos.Any(p => p.TotalCuotas != totalCuotas))
                    return "La cantidad de cuotas no coincide con el cronograma.";
                if (seguro.Periodos.GroupBy(p => p.NumeroCuota).Any(g => g.Count() > 1))
                    return "No se puede duplicar el numero de cuota dentro del seguro.";
            }

            decimal totalPeriodos = seguro.Periodos.Sum(p => p.Importe);
            if (Math.Round(totalPeriodos, 2) != Math.Round(obligacion.MontoOriginal.Value, 2))
                return "El monto original debe coincidir con la suma de los periodos.";

            return string.Empty;
        }

        private static void Normalizar(Seguro seguro)
        {
            ObligacionFinanciera obligacion = seguro.Obligacion;
            obligacion.TipoTitular = obligacion.TipoTitular.Trim().ToUpperInvariant();
            obligacion.NombreTitular = obligacion.NombreTitular?.Trim() ?? string.Empty;
            obligacion.CodigoReferencia = obligacion.CodigoReferencia?.Trim() ?? string.Empty;
            obligacion.NumeroContrato = obligacion.NumeroContrato?.Trim() ?? string.Empty;
            obligacion.Concepto = obligacion.Concepto?.Trim() ?? string.Empty;
            obligacion.Moneda = obligacion.Moneda.Trim().ToUpperInvariant();
            obligacion.MontoOriginal = Math.Round(obligacion.MontoOriginal!.Value, 2);
            obligacion.FechaInicio = obligacion.FechaInicio.Date;
            obligacion.Observacion = obligacion.Observacion?.Trim() ?? string.Empty;
            seguro.TipoSeguro = seguro.TipoSeguro.Trim();
            seguro.NumeroPoliza = seguro.NumeroPoliza.Trim();
            seguro.Periodicidad = seguro.Periodicidad.Trim().ToUpperInvariant();
            seguro.FechaInicioCobertura = seguro.FechaInicioCobertura.Date;
            seguro.FechaFinCobertura = seguro.FechaFinCobertura.Date;
            obligacion.TotalCuotas = seguro.Periodos.Max(p => p.TotalCuotas);

            foreach (SeguroPeriodo periodo in seguro.Periodos)
            {
                periodo.FechaVencimiento = periodo.FechaVencimiento.Date;
                periodo.Importe = Math.Round(periodo.Importe, 2);
                periodo.Observacion = periodo.Observacion?.Trim() ?? string.Empty;
            }
        }

        private static SeguroResultado Resultado(int idSeguro, int idObligacionFinanciera, string mensaje) => new()
        {
            IdSeguro = idSeguro,
            IdObligacionFinanciera = idObligacionFinanciera,
            Resultado = false,
            Mensaje = mensaje
        };

        private static string Usuario(string usuario) => string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim();
    }
}
