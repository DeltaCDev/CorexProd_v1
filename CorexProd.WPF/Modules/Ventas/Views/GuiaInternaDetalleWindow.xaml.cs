using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class GuiaInternaDetalleWindow : Window
    {
        private readonly GuiaInterna _guia;
        private readonly EmpresaNegocio _empresaNegocio = new();

        public GuiaInternaDetalleWindow(GuiaInterna guia)
        {
            _guia = guia;
            InitializeComponent();
            DataContext = guia;
        }

        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            string? error = GuiaInternaImpresionService.Reimprimir(_guia);
            if (string.IsNullOrWhiteSpace(error))
                NotificationService.Success("Guia interna enviada a impresion correctamente.");
            else
                NotificationService.Warning(error);
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
                GuiaInternaPdfExporter.Exportar(ruta, empresa, _guia);
                Clipboard.SetText(ruta);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{ruta}\"",
                    UseShellExecute = true
                });

                NotificationService.Success("PDF preparado para compartir. La ubicacion del archivo tambien fue copiada.");
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

        private string CrearNombreArchivo()
        {
            string numero = string.IsNullOrWhiteSpace(_guia.NumeroGuia)
                ? $"Guia_{_guia.IdGuiaInterna}"
                : _guia.NumeroGuia.Trim();

            foreach (char caracter in Path.GetInvalidFileNameChars())
                numero = numero.Replace(caracter, '-');

            return $"Guia_Interna_{numero}.pdf";
        }
    }
}
