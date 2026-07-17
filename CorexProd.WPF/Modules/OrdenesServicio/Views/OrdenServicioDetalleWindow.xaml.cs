using CorexProd.Entidad.Entidades;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CorexProd.WPF.Modules.OrdenesServicio.Views
{
    public partial class OrdenServicioDetalleWindow : Window
    {
        public OrdenServicioDetalleWindow(OrdenServicio orden, int tabSeleccionado = 0)
        {
            InitializeComponent();
            DataContext = new OrdenServicioDetalleWindowModel(orden, tabSeleccionado);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }

    internal sealed class OrdenServicioDetalleWindowModel
    {
        public OrdenServicioDetalleWindowModel(OrdenServicio orden, int tabSeleccionado)
        {
            Orden = orden;
            TabSeleccionado = tabSeleccionado;
            Movimientos = orden.Entregas
                .Concat(orden.Recepciones)
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.IdMovimiento)
                .ToList();
        }

        public OrdenServicio Orden { get; }
        public int TabSeleccionado { get; set; }
        public string Titulo => $"Orden de servicio {Orden.NumeroOrden}";
        public List<OrdenServicioMovimiento> Movimientos { get; }
    }
}
