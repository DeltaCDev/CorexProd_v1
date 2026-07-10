using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

            Title = "Detalle Orden de Compra";
            DataContext = orden;
            Loaded += OrdenCompraInternaDetalleWindow_Loaded;
        }

        private void OrdenCompraInternaDetalleWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OrdenCompraInternaDetalleWindow_Loaded;
            ConfigurarCabecera();
        }

        private void ConfigurarCabecera()
        {
            TextBlock? titulo = BuscarDescendiente<TextBlock>(
                this,
                texto => string.Equals(texto.Text, "Detalle de OCI", StringComparison.Ordinal));

            if (titulo != null)
                titulo.Text = "Detalle Orden de Compra";

            AgregarUsuarioCabecera();
        }

        private void AgregarUsuarioCabecera()
        {
            TextBlock? etiquetaOcInterna = BuscarDescendiente<TextBlock>(
                this,
                texto => string.Equals(texto.Text, "OC Interna", StringComparison.Ordinal));

            if (etiquetaOcInterna?.Parent is not StackPanel panelOcInterna
                || panelOcInterna.Parent is not Grid gridDatos)
            {
                return;
            }

            bool usuarioYaAgregado = gridDatos.Children
                .OfType<StackPanel>()
                .SelectMany(panel => panel.Children.OfType<TextBlock>())
                .Any(texto => string.Equals(texto.Text, "Usuario", StringComparison.Ordinal));

            if (usuarioYaAgregado)
                return;

            while (gridDatos.ColumnDefinitions.Count < 5)
                gridDatos.ColumnDefinitions.Add(new ColumnDefinition());

            gridDatos.ColumnDefinitions[0].Width = new GridLength(1.6, GridUnitType.Star);
            gridDatos.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            gridDatos.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            gridDatos.ColumnDefinitions[3].Width = new GridLength(1.15, GridUnitType.Star);
            gridDatos.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);

            Grid.SetColumn(panelOcInterna, 4);
            panelOcInterna.Margin = new Thickness(22, 0, 0, 0);

            StackPanel panelUsuario = new()
            {
                Margin = new Thickness(22, 0, 0, 0)
            };
            Grid.SetColumn(panelUsuario, 3);

            TextBlock etiquetaUsuario = new()
            {
                Text = "Usuario"
            };

            if (TryFindResource("FieldLabel") is Style estiloEtiqueta)
                etiquetaUsuario.Style = estiloEtiqueta;

            string usuario = string.IsNullOrWhiteSpace(_orden.UsuarioGenerador)
                ? "No registrado"
                : _orden.UsuarioGenerador.Trim();

            TextBlock valorUsuario = new()
            {
                Text = usuario,
                ToolTip = usuario
            };

            if (TryFindResource("FieldValue") is Style estiloValor)
                valorUsuario.Style = estiloValor;

            panelUsuario.Children.Add(etiquetaUsuario);
            panelUsuario.Children.Add(valorUsuario);
            gridDatos.Children.Add(panelUsuario);
        }

        private static T? BuscarDescendiente<T>(DependencyObject origen, Func<T, bool> condicion)
            where T : DependencyObject
        {
            int cantidad = VisualTreeHelper.GetChildrenCount(origen);
            for (int i = 0; i < cantidad; i++)
            {
                DependencyObject hijo = VisualTreeHelper.GetChild(origen, i);
                if (hijo is T encontrado && condicion(encontrado))
                    return encontrado;

                T? resultado = BuscarDescendiente(hijo, condicion);
                if (resultado != null)
                    return resultado;
            }

            return null;
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
