using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ProduccionResultadoWindow : Window
    {
        public ProduccionResultadoWindow(
            string titulo,
            string mensaje,
            string origen,
            string destino,
            decimal cantidad,
            decimal saldoPendiente,
            string estado)
        {
            InitializeComponent();
            TituloText.Text = titulo;
            MensajeText.Text = mensaje;
            OrigenText.Text = $"Area origen: {origen}";
            DestinoText.Text = $"Area destino: {destino}";
            CantidadText.Text = $"Cantidad transferida: {cantidad:N3}";
            SaldoText.Text = $"Saldo pendiente en origen: {saldoPendiente:N3}";
            EstadoText.Text = estado;
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
