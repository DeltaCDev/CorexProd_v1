using CorexProd.Entidad.Entidades;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CorexProd.WPF.Helpers
{
    public static class MobileNotificationPublisher
    {
        private static readonly HttpClient Client = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public static void OtNueva(OrdenTrabajo ot, string origen = "Desktop")
        {
            string titulo = $"Nueva OT {ot.NumeroOT}";
            string mensaje = $"Generada desde {origen}. Cliente: {Texto(ot.NombreCliente)}. OC: {Texto(ot.OrdenCompraCliente)}. Tipo: {Texto(ot.TipoOT)}. Cantidad: {ot.TotalPlanificado:N2}.";
            Publish("OT_NUEVA", titulo, mensaje, ot.IdOrdenTrabajo, ot.NumeroOT);
        }

        public static void Transferencia(OrdenTrabajo ot, string origen, string destino, decimal cantidad, string productos, bool esTerminacion)
        {
            string titulo = esTerminacion ? $"OT {ot.NumeroOT}: producto terminado" : $"OT {ot.NumeroOT}: transferencia";
            string mensaje = $"De {Texto(origen)} a {Texto(destino)}. Cantidad: {cantidad:N2}. Producto(s): {Texto(productos)}.";
            Publish(esTerminacion ? "OT_TERMINACION" : "OT_TRANSFERENCIA", titulo, mensaje, ot.IdOrdenTrabajo, ot.NumeroOT);
        }

        public static void Merma(OrdenTrabajo ot, string area, string producto, decimal cantidad, string detalle)
        {
            string titulo = $"OT {ot.NumeroOT}: merma registrada";
            string mensaje = $"Area: {Texto(area)}. Producto: {Texto(producto)}. Cantidad merma: {cantidad:N2}. Detalle: {Texto(detalle)}.";
            Publish("OT_MERMA", titulo, mensaje, ot.IdOrdenTrabajo, ot.NumeroOT);
        }

        public static void Reserva(OrdenTrabajo ot, string area, string producto, decimal cantidad)
        {
            string titulo = $"OT {ot.NumeroOT}: reserva de proceso";
            string mensaje = $"Area: {Texto(area)}. Producto: {Texto(producto)}. Cantidad reservada: {cantidad:N2}.";
            Publish("OT_RESERVA", titulo, mensaje, ot.IdOrdenTrabajo, ot.NumeroOT);
        }

        public static void Publish(string tipo, string titulo, string mensaje, int? idOrdenTrabajo, string numeroOT)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    string baseUrl = ObtenerApiBaseUrl();
                    if (string.IsNullOrWhiteSpace(baseUrl))
                        return;

                    await Client.PostAsJsonAsync(
                        $"{baseUrl}/api/notificaciones/publicar",
                        new
                        {
                            tipo,
                            titulo,
                            mensaje,
                            idOrdenTrabajo,
                            numeroOT
                        });
                }
                catch
                {
                    // La operacion de produccion ya fue realizada; una falla de aviso movil no debe interrumpir Desktop.
                }
            });
        }

        private static string ObtenerApiBaseUrl()
        {
            string value = ConfigurationManager.AppSettings["CorexProdApiUrl"] ?? "http://localhost:5000";
            value = value.Trim().TrimEnd('/');
            if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                value = "http://" + value["https://".Length..];
            return value;
        }

        private static string Texto(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}
