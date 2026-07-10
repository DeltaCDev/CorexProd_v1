using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class OrdenCompraInternaDetalleWindow : Window
    {
        private readonly OrdenCompraInterna _orden;
        private readonly EmpresaNegocio _empresaNegocio = new();

        public OrdenCompraInternaDetalleWindow(OrdenCompraInterna orden)
        {
            _orden = orden;
            InitializeComponent();
            DataContext = orden;
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();

        private void DescargarPdf_Click(object sender, RoutedEventArgs e)
        {
            Empresa? empresa = ObtenerEmpresa();
            if (empresa == null)
                return;

            SaveFileDialog dialogo = new()
            {
                Title = "Guardar orden de compra",
                FileName = CrearNombreArchivo(),
                Filter = "Documento PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true
            };

            if (dialogo.ShowDialog(this) != true)
                return;

            try
            {
                ExportarPdf(dialogo.FileName, empresa);
                NotificationService.Success("Orden de compra generada correctamente.");
                Process.Start(new ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo generar la orden de compra: {ex.Message}");
            }
        }

        private void Compartir_Click(object sender, RoutedEventArgs e)
        {
            Empresa? empresa = ObtenerEmpresa();
            if (empresa == null)
                return;

            try
            {
                string carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "CorexProd",
                    "Compartir");
                Directory.CreateDirectory(carpeta);

                string ruta = Path.Combine(carpeta, CrearNombreArchivo());
                ExportarPdf(ruta, empresa);
                Clipboard.SetText(ruta);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{ruta}\"",
                    UseShellExecute = true
                });

                NotificationService.Success("PDF preparado para compartir. La ubicación del archivo también fue copiada.");
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo preparar el archivo para compartir: {ex.Message}");
            }
        }

        private Empresa? ObtenerEmpresa()
        {
            Empresa? empresa = _empresaNegocio.ObtenerPredeterminada();
            if (empresa == null)
                NotificationService.Warning("Debe registrar una empresa predeterminada antes de generar el PDF.");
            return empresa;
        }

        private void ExportarPdf(string ruta, Empresa empresa) =>
            ProformaPdfExporter.Exportar(ruta, empresa, CrearDocumentoPdf(_orden));

        private string CrearNombreArchivo()
        {
            string numero = string.IsNullOrWhiteSpace(_orden.OrdenCompraCliente)
                ? _orden.NumeroOci
                : _orden.OrdenCompraCliente;

            foreach (char caracter in Path.GetInvalidFileNameChars())
                numero = numero.Replace(caracter, '-');

            return $"OrdenCompra_{numero}.pdf";
        }

        private static Proforma CrearDocumentoPdf(OrdenCompraInterna orden) => new()
        {
            SerieNumero = orden.NumeroOci,
            FechaEmision = orden.FechaEmision,
            FechaVencimiento = orden.FechaEntrega == default ? orden.FechaEmision : orden.FechaEntrega,
            OrdenCompraCliente = orden.OrdenCompraCliente,
            NombreCliente = orden.NombreCliente,
            UsuarioGenerador = orden.UsuarioGenerador,
            Subtotal = orden.Subtotal,
            Descuento = orden.Descuento,
            Igv = orden.Igv,
            IgvPorcentaje = orden.IgvPorcentaje,
            CondicionTributaria = orden.CondicionTributaria,
            Total = orden.Total,
            Estado = orden.Estado,
            Observacion = string.Empty,
            Detalles = orden.Detalles.Select(detalle => new ProformaDetalle
            {
                IdProducto = detalle.IdProducto,
                CodigoProducto = detalle.CodigoProducto,
                NombreProducto = detalle.NombreProducto,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Descuento = detalle.Descuento,
                Importe = detalle.Importe,
                Observacion = detalle.Observacion
            }).ToList()
        };
    }
}
