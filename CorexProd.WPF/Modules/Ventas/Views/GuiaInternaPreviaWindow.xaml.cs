using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Helpers;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class GuiaInternaPreviaWindow : Window
    {
        private readonly GuiaInterna _guia;

        public GuiaInternaPreviaWindow(GuiaInterna guia)
        {
            InitializeComponent();
            _guia = guia;
            DataContext = guia;
        }

        private void Continuar_Click(object sender, RoutedEventArgs e)
        {
            AbrirEditor();
        }

        private void Preparar_Click(object sender, RoutedEventArgs e)
        {
            AbrirEditor();
        }

        private void AbrirEditor()
        {
            if (!_guia.Detalles.Any(d => d.CantidadDespachar > 0))
            {
                NotificationService.Warning("No hay productos con stock disponible para despachar.");
                return;
            }

            GuiaInternaEditorWindow editor = new(_guia) { Owner = this };
            if (editor.ShowDialog() == true)
                DialogResult = true;
        }
    }
}
