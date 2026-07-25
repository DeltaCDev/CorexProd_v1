using CorexProd.Entidad.Entidades;
using CorexProd.WPF.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

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

        private OrdenServicioDetalleWindowModel? Modelo => DataContext as OrdenServicioDetalleWindowModel;

        private void VerFotoFila_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: OrdenServicioFoto foto } && Modelo != null)
                Modelo.FotoSeleccionada = foto;
        }

        private void FotoAnterior_Click(object sender, RoutedEventArgs e) => Modelo?.MoverFoto(-1);

        private void FotoSiguiente_Click(object sender, RoutedEventArgs e) => Modelo?.MoverFoto(1);

        private void AbrirFoto_Click(object sender, RoutedEventArgs e)
        {
            OrdenServicioFoto? foto = Modelo?.FotoSeleccionada;
            string ruta = foto?.ObtenerRutaLocal() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
            {
                NotificationService.Warning("No se encontro el archivo de la foto.");
                return;
            }

            Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        }
    }

    internal sealed class OrdenServicioDetalleWindowModel : INotifyPropertyChanged
    {
        private OrdenServicioFoto? _fotoSeleccionada;

        public OrdenServicioDetalleWindowModel(OrdenServicio orden, int tabSeleccionado)
        {
            Orden = orden;
            TabSeleccionado = tabSeleccionado;
            Movimientos = orden.Entregas
                .Concat(orden.Recepciones)
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.IdMovimiento)
                .ToList();
            FotoSeleccionada = orden.Fotos
                .OrderBy(x => x.Orden)
                .ThenBy(x => x.IdOrdenServicioFoto)
                .FirstOrDefault();
        }

        public OrdenServicio Orden { get; }
        public int TabSeleccionado { get; set; }
        public string Titulo => $"Orden de servicio {Orden.NumeroOrden}";
        public List<OrdenServicioMovimiento> Movimientos { get; }
        public bool HayFotos => Orden.Fotos.Count > 0;
        public bool SinFotos => !HayFotos;
        public string FotoActualTexto
        {
            get
            {
                if (FotoSeleccionada == null)
                    return "Fotos";

                int indice = FotosOrdenadas.IndexOf(FotoSeleccionada) + 1;
                string titulo = string.IsNullOrWhiteSpace(FotoSeleccionada.Titulo)
                    ? FotoSeleccionada.NombreArchivo
                    : FotoSeleccionada.Titulo;
                return $"{indice}/{Orden.Fotos.Count} - {titulo}";
            }
        }

        public OrdenServicioFoto? FotoSeleccionada
        {
            get => _fotoSeleccionada;
            set
            {
                _fotoSeleccionada = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FotoActualTexto));
            }
        }

        private List<OrdenServicioFoto> FotosOrdenadas => Orden.Fotos
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.IdOrdenServicioFoto)
            .ToList();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void MoverFoto(int direccion)
        {
            List<OrdenServicioFoto> fotos = FotosOrdenadas;
            if (fotos.Count == 0)
                return;

            int indiceActual = FotoSeleccionada == null ? 0 : fotos.IndexOf(FotoSeleccionada);
            if (indiceActual < 0)
                indiceActual = 0;

            int indiceNuevo = (indiceActual + direccion + fotos.Count) % fotos.Count;
            FotoSeleccionada = fotos[indiceNuevo];
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
