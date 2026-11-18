using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Windows;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class ServicioExternoDetalleWindow : Window
    {
        public ServicioExternoDetalleWindow(string numeroOt, IEnumerable<OrdenTrabajoServicioExternoDetalle> servicios)
        {
            InitializeComponent();
            TituloText.Text = $"Servicios externos de {numeroOt}";
            ServiciosGrid.ItemsSource = servicios;
        }
    }
}
