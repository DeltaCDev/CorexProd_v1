using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ValidacionInsumosWindow : Window
    {
        public ValidacionInsumosWindow(
            OrdenCompraInterna oci,
            IReadOnlyCollection<OrdenTrabajoValidacionProducto> productos)
        {
            InitializeComponent();
            ProductosGrid.ItemsSource = productos;
            OciText.Text = $"OCI {oci.NumeroOci} | Cliente: {oci.NombreCliente}";

            int faltantes = productos.Count(x => x.EstadoInsumos == "Faltantes");
            int sinFicha = productos.Count(x => x.EstadoInsumos == "Sin ficha tecnica");
            int completos = productos.Count - faltantes - sinFicha;

            MensajeText.Text =
                $"Resumen: {completos} completo(s), {faltantes} con faltantes y {sinFicha} sin ficha tecnica. " +
                "Esta validación es informativa; puede continuar para revisar el detalle y generar la OT.";
        }

        private void Continuar_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
