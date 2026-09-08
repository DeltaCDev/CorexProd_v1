using CorexProd.Datos.Datos;
using CorexProd.Entidad.Entidades;
using System;
using System.Collections.Generic;

namespace CorexProd.Negocio.Negocio
{
    public class ProgramacionTesoreriaNegocio
    {
        private readonly ProgramacionTesoreriaDatos _datos = new();

        public List<ProgramacionPagoGeneralItem> ObtenerProgramacionGeneral(
            DateTime fechaInicio,
            DateTime fechaFin,
            bool incluirPendientesAnteriores,
            string? estado = null,
            string? categoria = null,
            string? moneda = null,
            string? texto = null)
        {
            if (fechaInicio == default)
                throw new ArgumentException("Debe ingresar la fecha de inicio.");

            if (fechaFin == default)
                throw new ArgumentException("Debe ingresar la fecha de fin.");

            if (fechaInicio.Date > fechaFin.Date)
                throw new ArgumentException("La fecha de inicio no puede ser mayor a la fecha de fin.");

            return _datos.ObtenerProgramacionGeneral(
                fechaInicio.Date,
                fechaFin.Date,
                incluirPendientesAnteriores,
                NormalizarFiltro(estado),
                NormalizarCategoria(categoria),
                NormalizarFiltro(moneda),
                texto);
        }

        private static string? NormalizarFiltro(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) || valor.Equals("Todos", StringComparison.OrdinalIgnoreCase)
                ? null
                : valor.Trim().ToUpperInvariant();
        }

        private static string? NormalizarCategoria(string? categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria) || categoria.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                return null;

            return categoria.Trim().ToUpperInvariant() switch
            {
                "FACTURA CRÉDITO" => "FACTURA_CREDITO",
                "FACTURA CREDITO" => "FACTURA_CREDITO",
                "PRÉSTAMO" => "PRESTAMO",
                "PRESTAMO" => "PRESTAMO",
                "LETRA" => "LETRA",
                "TARJETA" => "TARJETA",
                _ => categoria.Trim().ToUpperInvariant()
            };
        }
    }
}
