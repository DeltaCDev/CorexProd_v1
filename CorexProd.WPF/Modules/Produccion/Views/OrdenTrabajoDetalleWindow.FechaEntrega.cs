using CorexProd.Entidad.Entidades;
using System;
using System.Windows;
using System.Windows.Media;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoDetalleWindow
    {
        private void FechaEntregaPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ActualizarFechaEntrega();
        }

        private void ActualizarFechaEntrega()
        {
            if (_ot == null || _ot.IdOrdenCompraInterna <= 0)
            {
                MostrarFechaNoRegistrada();
                return;
            }

            OrdenCompraInterna? ordenCompra = _ociNegocio.Obtener(_ot.IdOrdenCompraInterna);
            if (ordenCompra == null || ordenCompra.FechaEntrega == default)
            {
                MostrarFechaNoRegistrada();
                return;
            }

            DateTime fechaEntrega = ordenCompra.FechaEntrega.Date;
            int diasRestantes = (fechaEntrega - DateTime.Today).Days;
            bool otFinalizada = EsEstadoFinalizado(_ot.EstadoOperativo);
            bool requiereAlerta = !otFinalizada && diasRestantes <= 3;

            FechaEntregaText.Text = fechaEntrega.ToString("dd/MM/yyyy");
            FechaEntregaAlertaText.Text = CrearTextoPlazo(diasRestantes, otFinalizada);

            if (requiereAlerta)
            {
                FechaEntregaBorder.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                FechaEntregaBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                FechaEntregaText.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                FechaEntregaAlertaText.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                return;
            }

            FechaEntregaBorder.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
            FechaEntregaBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            FechaEntregaText.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            FechaEntregaAlertaText.Foreground = new SolidColorBrush(
                otFinalizada ? Color.FromRgb(22, 101, 52) : Color.FromRgb(100, 116, 139));
        }

        private void MostrarFechaNoRegistrada()
        {
            FechaEntregaText.Text = "No registrada";
            FechaEntregaAlertaText.Text = "Sin fecha de entrega";
            FechaEntregaBorder.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
            FechaEntregaBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            FechaEntregaText.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            FechaEntregaAlertaText.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        }

        private static string CrearTextoPlazo(int diasRestantes, bool otFinalizada)
        {
            if (otFinalizada)
                return "Producción finalizada";

            return diasRestantes switch
            {
                < 0 => $"Vencida hace {Math.Abs(diasRestantes)} día{(Math.Abs(diasRestantes) == 1 ? string.Empty : "s")}",
                0 => "Vence hoy",
                1 => "Vence mañana",
                <= 3 => $"Vence en {diasRestantes} días",
                _ => $"Faltan {diasRestantes} días"
            };
        }

        private static bool EsEstadoFinalizado(string? estado)
        {
            string normalizado = (estado ?? string.Empty)
                .Trim()
                .Replace('_', ' ')
                .ToUpperInvariant();

            return normalizado is
                "TERMINADO" or
                "TERMINADA" or
                "TERMINADO PARCIAL" or
                "TERMINADA PARCIAL" or
                "ANULADO" or
                "ANULADA";
        }
    }
}
