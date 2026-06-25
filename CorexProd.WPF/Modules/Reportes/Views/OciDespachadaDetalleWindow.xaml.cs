using System;
using System.Data;
using System.Windows;

namespace CorexProd.WPF.Modules.Reportes.Views
{
    public partial class OciDespachadaDetalleWindow : Window
    {
        public OciDespachadaDetalleWindow(DataRowView cabecera, DataView detalle)
        {
            InitializeComponent();

            string oci = cabecera["OCI"]?.ToString() ?? string.Empty;
            string cliente = cabecera["Cliente"]?.ToString() ?? string.Empty;
            string proforma = cabecera["Proforma"]?.ToString() ?? string.Empty;
            string ot = cabecera["OT"]?.ToString() ?? string.Empty;
            decimal totalSolicitado = ObtenerDecimal(cabecera, "Total solicitado");
            decimal totalProducido = ObtenerDecimal(cabecera, "Total producido");
            decimal totalDespachado = ObtenerDecimal(cabecera, "Total despachado");

            Title = $"Detalle de {oci}";
            TxtSubtitulo.Text = $"Proforma: {proforma}  |  OT: {(string.IsNullOrWhiteSpace(ot) ? "Sin OT" : ot)}  |  Registros: {detalle.Count}";
            TxtOci.Text = oci;
            TxtCliente.Text = cliente;
            TxtTotales.Text = $"Solicitado: {totalSolicitado:N2}  |  Producido: {totalProducido:N2}  |  Despachado: {totalDespachado:N2}";
            GrdDetalle.ItemsSource = detalle;
        }

        private static decimal ObtenerDecimal(DataRowView fila, string columna)
        {
            if (!fila.Row.Table.Columns.Contains(columna) || fila[columna] == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToDecimal(fila[columna]);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
