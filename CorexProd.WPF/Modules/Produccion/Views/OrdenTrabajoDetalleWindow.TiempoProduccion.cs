using CorexProd.Entidad.Entidades;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoDetalleWindow
    {
        private bool _temporizadorTiempoProduccionRegistrado;
        private bool _fechaTerminoProduccionConsultada;
        private DateTime? _fechaTerminoProduccionCache;

        private void TiempoProduccionPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_temporizadorTiempoProduccionRegistrado)
            {
                _refreshTimer.Tick += TiempoProduccionTimer_Tick;
                _temporizadorTiempoProduccionRegistrado = true;
            }

            ActualizarTiempoProduccion();
        }

        private void TiempoProduccionTimer_Tick(object? sender, EventArgs e)
        {
            ActualizarTiempoProduccion();
        }

        private void ActualizarTiempoProduccion()
        {
            if (_ot == null)
            {
                MostrarTiempoNoDisponible();
                return;
            }

            DateTime fechaEmision = _ot.FechaEmision != default
                ? _ot.FechaEmision
                : _ot.FechaRegistro;

            if (fechaEmision == default)
            {
                MostrarTiempoNoDisponible();
                return;
            }

            string estado = NormalizarEstadoProduccion(_ot.EstadoOperativo);
            bool anulada = estado is "ANULADO" or "ANULADA";
            bool terminadaParcial = estado is "TERMINADO PARCIAL" or "TERMINADA PARCIAL";
            bool terminada = estado is "TERMINADO" or "TERMINADA" || terminadaParcial;
            bool enProceso = estado is "EN PROCESO" or "PROCESO";

            FechaEmisionProduccionText.Text = $"F. emisión: {fechaEmision:dd/MM/yyyy HH:mm}";

            if (anulada)
            {
                DateTime? fechaAnulacion = _ot.FechaAnulacion;
                TiempoProduccionTituloText.Text = "Tiempo hasta anulación";
                FechaTerminoProduccionText.Text = fechaAnulacion.HasValue
                    ? $"F. término: {fechaAnulacion.Value:dd/MM/yyyy HH:mm}"
                    : "F. término: No registrada";
                TiempoProduccionText.Text = fechaAnulacion.HasValue
                    ? FormatearDuracion(fechaEmision, fechaAnulacion.Value)
                    : "No disponible";
                AplicarEstiloTiempoProduccion(
                    Color.FromRgb(254, 242, 242),
                    Color.FromRgb(252, 165, 165),
                    Color.FromRgb(185, 28, 28));
                return;
            }

            if (terminada)
            {
                DateTime? fechaTermino = ObtenerFechaTerminoProduccion();
                TiempoProduccionTituloText.Text = "Tiempo de elaboración";
                FechaTerminoProduccionText.Text = fechaTermino.HasValue
                    ? $"F. término: {fechaTermino.Value:dd/MM/yyyy HH:mm}"
                    : "F. término: No registrada";
                TiempoProduccionText.Text = fechaTermino.HasValue
                    ? FormatearDuracion(fechaEmision, fechaTermino.Value)
                    : "No disponible";

                if (terminadaParcial)
                {
                    AplicarEstiloTiempoProduccion(
                        Color.FromRgb(255, 247, 237),
                        Color.FromRgb(253, 186, 116),
                        Color.FromRgb(194, 65, 12));
                }
                else
                {
                    AplicarEstiloTiempoProduccion(
                        Color.FromRgb(240, 253, 244),
                        Color.FromRgb(134, 239, 172),
                        Color.FromRgb(22, 101, 52));
                }

                return;
            }

            if (enProceso)
            {
                TiempoProduccionTituloText.Text = "Tiempo de producción";
                FechaTerminoProduccionText.Text = "F. término: En proceso";
                TiempoProduccionText.Text = FormatearDuracion(fechaEmision, DateTime.Now);
                AplicarEstiloTiempoProduccion(
                    Color.FromRgb(239, 246, 255),
                    Color.FromRgb(147, 197, 253),
                    Color.FromRgb(29, 78, 216));
                return;
            }

            TiempoProduccionTituloText.Text = "Producción pendiente";
            FechaTerminoProduccionText.Text = "F. término: Pendiente";
            TiempoProduccionText.Text = "Aún no iniciada";
            AplicarEstiloTiempoProduccion(
                Color.FromRgb(248, 250, 252),
                Color.FromRgb(203, 213, 225),
                Color.FromRgb(71, 85, 105));
        }

        private DateTime? ObtenerFechaTerminoProduccion()
        {
            if (_fechaTerminoProduccionConsultada)
                return _fechaTerminoProduccionCache;

            try
            {
                _fechaTerminoProduccionCache = _negocio
                    .ListarMovimientos(_ot.IdOrdenTrabajo)
                    .Where(movimiento => string.Equals(
                        movimiento.AccionTecnica,
                        "CIERRE_PRODUCCION",
                        StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(movimiento => movimiento.FechaHora)
                    .Select(movimiento => (DateTime?)movimiento.FechaHora)
                    .FirstOrDefault();

                _fechaTerminoProduccionConsultada = true;
                return _fechaTerminoProduccionCache;
            }
            catch
            {
                return null;
            }
        }

        private void MostrarTiempoNoDisponible()
        {
            TiempoProduccionTituloText.Text = "Tiempo de producción";
            FechaEmisionProduccionText.Text = "F. emisión: No registrada";
            FechaTerminoProduccionText.Text = "F. término: No registrada";
            TiempoProduccionText.Text = "No disponible";
            AplicarEstiloTiempoProduccion(
                Color.FromRgb(248, 250, 252),
                Color.FromRgb(203, 213, 225),
                Color.FromRgb(100, 116, 139));
        }

        private void AplicarEstiloTiempoProduccion(Color fondo, Color borde, Color acento)
        {
            TiempoProduccionBorder.Background = new SolidColorBrush(fondo);
            TiempoProduccionBorder.BorderBrush = new SolidColorBrush(borde);
            TiempoProduccionTituloText.Foreground = new SolidColorBrush(acento);
            TiempoProduccionText.Foreground = new SolidColorBrush(acento);
        }

        private static string FormatearDuracion(DateTime inicio, DateTime termino)
        {
            if (termino < inicio)
                termino = inicio;

            TimeSpan duracion = termino - inicio;
            int dias = (int)duracion.TotalDays;
            int horas = duracion.Hours;
            int minutos = duracion.Minutes;

            if (duracion.TotalMinutes < 1)
                return "Menos de 1 min";

            if (dias > 0)
                return $"{dias} día{(dias == 1 ? string.Empty : "s")} {horas} h {minutos} min";

            if (horas > 0)
                return $"{horas} h {minutos} min";

            return $"{Math.Max(1, minutos)} min";
        }

        private static string NormalizarEstadoProduccion(string? estado) =>
            (estado ?? string.Empty)
                .Trim()
                .Replace('_', ' ')
                .ToUpperInvariant();
    }
}
