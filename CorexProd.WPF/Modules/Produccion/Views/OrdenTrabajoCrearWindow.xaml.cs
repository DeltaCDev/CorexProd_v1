using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CorexProd.WPF.Modules.Produccion.Views
{
    public partial class OrdenTrabajoCrearWindow : Window, INotifyPropertyChanged
    {
        private enum UsoReservaDecision
        {
            UsarSoloNecesario,
            UsarTodaReserva,
            Cancelar
        }

        private readonly OrdenCompraInterna _oci;
        private readonly List<OrdenTrabajoValidacionProducto> _productos;
        private readonly OrdenTrabajo? _otRelacionada;
        private readonly OrdenTrabajoNegocio _negocio = new();
        private readonly ParametroNegocio _parametroNegocio = new();
        private bool _guardando;
        private bool _adicionalStockHabilitado;

        public bool AdicionalStockHabilitado
        {
            get => _adicionalStockHabilitado;
            private set
            {
                if (_adicionalStockHabilitado == value) return;
                _adicionalStockHabilitado = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AdicionalStockHabilitado)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public OrdenTrabajoCrearWindow(
            OrdenCompraInterna oci,
            IEnumerable<OrdenTrabajoValidacionProducto> productos,
            OrdenTrabajo? otRelacionada = null)
        {
            InitializeComponent();
            _oci = oci;
            _otRelacionada = otRelacionada;
            _productos = productos.ToList();
            CabeceraText.Text = otRelacionada == null
                ? $"OCI {oci.NumeroOci} | Cliente: {oci.NombreCliente}"
                : $"Regularizacion de OT {otRelacionada.NumeroOT} | OCI {oci.NumeroOci} | Cliente: {oci.NombreCliente}";
            EmpresaText.Text = oci.NombreCliente;
            OrdenCompraText.Text = oci.OrdenCompraCliente;
            FechaEmisionText.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            DateTime fechaEntregaOt = oci.FechaEntrega == default
                ? DateTime.Today
                : oci.FechaEntrega.Date.AddDays(-1);
            FechaEntregaPicker.SelectedDate = fechaEntregaOt;
            FechaEntregaReferenciaText.Text = oci.FechaEntrega == default
                ? "Referencia OC: no registrada"
                : $"Pactada OC: {oci.FechaEntrega:dd/MM/yyyy}";
            if (!string.IsNullOrWhiteSpace(oci.Observacion))
                ObservacionText.Text = oci.Observacion.Trim();

            if (otRelacionada != null && string.IsNullOrWhiteSpace(ObservacionText.Text))
                ObservacionText.Text = $"Regularizacion de OT por faltante de {otRelacionada.NumeroOT}";
            DetallesGrid.ItemsSource = _productos;
            if (otRelacionada != null)
            {
                AgregarStockButton.IsEnabled = false;
                AgregarStockButton.Content = "Adicional no disponible en regularización";
                AdicionalStockInfo.Text = "La regularización solo repone el pendiente de la OT origen.";
            }
        }

        private void Observacion_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajoValidacionProducto producto)
                return;

            string observacion = string.IsNullOrWhiteSpace(producto.Observacion)
                ? "El producto no tiene observaciones registradas."
                : producto.Observacion;
            MessageBox.Show(
                observacion,
                $"Observaciones - {producto.CodigoProducto}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ImprimirFicha_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajoValidacionProducto producto)
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(producto.CodigoProducto))
                {
                    NotificationService.Warning("El producto no tiene codigo para buscar su ficha tecnica.");
                    return;
                }

                var parametroRuta = _parametroNegocio.ObtenerPorCodigo("RUTA_FICHA_TECNICA");

                if (parametroRuta == null)
                {
                    NotificationService.Warning("No existe el parametro RUTA_FICHA_TECNICA.");
                    return;
                }

                string rutaBase = parametroRuta.ValorParametro;

                if (string.IsNullOrWhiteSpace(rutaBase))
                {
                    NotificationService.Warning("La ruta de fichas tecnicas esta vacia.");
                    return;
                }

                string? rutaPdf = FichaTecnicaArchivoHelper.BuscarRutaPdf(
                    rutaBase,
                    producto.CodigoProducto);

                if (rutaPdf == null)
                {
                    string rutaEsperada = FichaTecnicaArchivoHelper.RutaEsperada(
                        rutaBase,
                        producto.CodigoProducto);
                    NotificationService.Warning($"No hay ficha tecnica en la ruta:\n{rutaEsperada}");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = rutaPdf,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error al abrir la ficha tecnica:\n{ex.Message}");
            }
        }

        private void VerDetalle_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OrdenTrabajoValidacionProducto producto)
                return;

            IReadOnlyCollection<OrdenTrabajoInsumoDetalle> insumos =
                _negocio.DetalleInsumos(producto.IdOrdenCompraInternaDetalle);
            new DetalleInsumosProductoWindow(producto, _oci.OrdenCompraCliente, insumos)
            {
                Owner = this
            }.ShowDialog();
        }

        private void Crear_Click(object sender, RoutedEventArgs e)
        {
            if (_guardando) return;

            try
            {
                _guardando = true;
                GuardarButton.IsEnabled = false;

                UsoReservaDecision usoReserva = ConfirmarUsoReserva();
                if (usoReserva == UsoReservaDecision.Cancelar)
                    return;

                if (_productos.Any(x => x.AdicionalStock < 0))
                {
                    NotificationService.Warning("La cantidad adicional para stock no puede ser negativa.");
                    return;
                }

                List<OrdenTrabajoPlanificacion> items = _productos.Select(producto =>
                    new OrdenTrabajoPlanificacion
                    {
                        IdOrdenCompraInternaDetalle = producto.IdOrdenCompraInternaDetalle,
                        CantidadPlanificada = producto.TotalProduccion
                    }).ToList();

                Usuario? usuarioAutorizaStock = null;
                if (_otRelacionada == null && _productos.Any(x => x.AdicionalStock > 0))
                {
                    AutorizacionOperacionWindow autorizacion = new("Autorizar adicional para stock")
                    {
                        Owner = this
                    };
                    if (autorizacion.ShowDialog() != true)
                        return;

                    usuarioAutorizaStock = _negocio.AutenticarUsuario(autorizacion.Usuario, autorizacion.Clave);
                    if (usuarioAutorizaStock == null)
                    {
                        NotificationService.Warning("El usuario o la contraseña del autorizador no son válidos.");
                        return;
                    }
                }

                int idUsuario = SessionManager.UsuarioActual?.IdUsuario ?? 0;
                (int idOrdenTrabajo, string numero) = _negocio.Crear(
                    _oci.IdOrdenCompraInterna,
                    idUsuario,
                    ObservacionText.Text.Trim(),
                    items,
                    _otRelacionada?.IdOrdenTrabajo,
                    usoReserva == UsoReservaDecision.UsarTodaReserva,
                    usuarioAutorizaStock?.IdUsuario);

                OrdenTrabajo? otCreada = _negocio.Obtener(idOrdenTrabajo);
                if (otCreada != null)
                    MobileNotificationPublisher.OtNueva(otCreada, _otRelacionada == null ? "Desktop" : "Desktop - regularizacion");

                new DocumentoGeneradoResumenWindow(
                    "OT generada correctamente",
                    $"Se generó la OT correctamente: {numero}.",
                    "Para producir los siguientes productos:",
                    _productos.Select(producto => new DocumentoGeneradoProducto
                    {
                        Codigo = producto.CodigoProducto,
                        Producto = producto.NombreProducto,
                        Cantidad = producto.TotalProduccion
                    }))
                {
                    Owner = this
                }.ShowDialog();

                new OrdenTrabajoImpresionWindow(
                    numero,
                    _oci.NumeroOci,
                    _oci.OrdenCompraCliente,
                    _oci.NombreCliente,
                    _oci.UsuarioGenerador,
                    usuarioAutorizaStock?.NombreUsuario ?? otCreada?.UsuarioAutoriza ?? string.Empty,
                    _oci.FechaRegistro == default ? _oci.FechaEmision : _oci.FechaRegistro,
                    ObservacionText.Text.Trim(),
                    _productos.Select(producto => new OrdenTrabajoImpresionItem
                    {
                        Codigo = producto.CodigoProducto,
                        Producto = producto.NombreProducto,
                        Observacion = producto.Observacion,
                        CantidadOc = producto.Deficit,
                        CantidadStock = producto.AdicionalStock
                    }),
                    FechaEntregaPicker.SelectedDate,
                    _oci.FechaEntrega == default ? null : _oci.FechaEntrega)
                {
                    Owner = this
                }.ShowDialog();

                DialogResult = true;
            }
            catch (Exception ex)
            {
                NotificationService.Error(ex.Message);
                MessageBox.Show(
                    this,
                    $"No se pudo generar la OT:\n\n{ex.Message}",
                    "Error al generar OT",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _guardando = false;
                GuardarButton.IsEnabled = true;
            }
        }

        private void VistaPrevia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new OrdenTrabajoImpresionWindow(
                    "BORRADOR",
                    _oci.NumeroOci,
                    _oci.OrdenCompraCliente,
                    _oci.NombreCliente,
                    _oci.UsuarioGenerador,
                    string.Empty,
                    DateTime.Now,
                    ObservacionText.Text.Trim(),
                    _productos.Select(producto => new OrdenTrabajoImpresionItem
                    {
                        Codigo = producto.CodigoProducto,
                        Producto = producto.NombreProducto,
                        Observacion = producto.Observacion,
                        CantidadOc = producto.Deficit,
                        CantidadStock = producto.AdicionalStock
                    }),
                    FechaEntregaPicker.SelectedDate,
                    _oci.FechaEntrega == default ? null : _oci.FechaEntrega)
                {
                    Owner = this
                }.ShowDialog();
            }
            catch (Exception ex)
            {
                NotificationService.Error($"No se pudo mostrar la vista previa:\n{ex.Message}");
            }
        }

        private void AgregarStock_Click(object sender, RoutedEventArgs e)
        {
            if (_otRelacionada != null) return;
            AdicionalStockHabilitado = true;
            AgregarStockButton.IsEnabled = false;
            AgregarStockButton.Content = "Adicional para stock habilitado";
            AdicionalStockInfo.Text = "Digite el adicional por producto y revise el total a producir.";
            DetallesGrid.Items.Refresh();
        }

        private void DetallesGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            bool esAdicional = e.Column.Header?.ToString()?.Contains("Adicional", StringComparison.OrdinalIgnoreCase) == true;
            if (!esAdicional || !AdicionalStockHabilitado)
                e.Cancel = true;
        }

        private UsoReservaDecision ConfirmarUsoReserva()
        {
            List<OrdenTrabajoValidacionProducto> conReserva = _productos
                .Where(producto => producto.TieneStockProcesoReservado)
                .ToList();

            if (conReserva.Count == 0)
                return UsoReservaDecision.UsarSoloNecesario;

            decimal reservaDisponible = conReserva.Sum(producto => producto.StockProcesoDisponible);
            decimal reservaNecesaria = conReserva.Sum(producto => producto.CantidadReservaNecesaria);
            decimal reservaExcedente = conReserva.Sum(producto => producto.CantidadReservaExcedente);

            string mensaje =
                "Existen unidades reservadas para uno o mas productos.\n\n" +
                $"Reserva disponible: {reservaDisponible:N2}\n" +
                $"Reserva necesaria para cubrir el requerimiento: {reservaNecesaria:N2}\n" +
                $"Excedente si procesas toda la reserva: {reservaExcedente:N2}\n\n" +
                "Si eliges Si, se usara solo la cantidad necesaria.\n" +
                "Si eliges No, se procesara toda la reserva disponible y el excedente quedara como stock terminado.\n" +
                "Cancelar detiene la generacion de la OT.";

            MessageBoxResult respuesta = MessageBox.Show(
                this,
                mensaje,
                "Uso de stock reservado",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);

            return respuesta switch
            {
                MessageBoxResult.Yes => UsoReservaDecision.UsarSoloNecesario,
                MessageBoxResult.No => UsoReservaDecision.UsarTodaReserva,
                _ => UsoReservaDecision.Cancelar
            };
        }
    }
}
