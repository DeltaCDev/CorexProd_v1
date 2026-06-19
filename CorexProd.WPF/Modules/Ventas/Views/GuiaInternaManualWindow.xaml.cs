using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Shared.Views;
using CorexProd.WPF.Modules.Ventas.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Ventas.Views
{
    public partial class GuiaInternaManualWindow : Window
    {
        private readonly GuiaInternaNegocio _negocio = new();
        private readonly IngresoManualStockNegocio _almacenes = new();
        private readonly UsuarioNegocio _usuarios = new();
        private readonly ClienteNegocio _clientes = new();
        private GuiaInterna _guia;
        private bool _cargando;
        private bool _actualizandoCliente;
        private List<Cliente> _todosLosClientes = [];

        public ObservableCollection<GuiaInternaManualDetalleViewModel> DetallesEdicion { get; } = [];

        public GuiaInternaManualWindow(GuiaInterna guia)
        {
            _guia = guia;
            _guia.Detalles.Clear();
            _guia.UsuarioEmisor = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema";
            DetallesEdicion.Add(CrearFila());

            InitializeComponent();
            MotivoCombo.ItemsSource = new[]
            {
                "Entrega a cliente", "Consumo interno", "Entrega a un área", "Préstamo",
                "Muestra", "Reposición", "Donación", "Otro tipo de salida"
            };
            MotivoCombo.SelectedItem = string.IsNullOrWhiteSpace(_guia.MotivoEmisionManual) ? null : _guia.MotivoEmisionManual;
            _todosLosClientes = _clientes.Listar().Where(c => c.Estado).ToList();
            EmisorText.Text = SessionManager.UsuarioActual?.NombreCompleto ?? _guia.UsuarioEmisor;
            CargarCombos();
            DataContext = _guia;
        }

        private void CargarCombos()
        {
            _cargando = true;
            List<AlmacenStock> almacenes = _almacenes.ListarAlmacenes().Where(a => a.Estado).ToList();
            AlmacenCombo.ItemsSource = almacenes;
            AlmacenCombo.SelectedItem = almacenes.FirstOrDefault(a => a.IdAlmacen == _guia.IdAlmacen);

            List<Usuario> usuarios = _usuarios.Listar().Where(u => u.Estado).ToList();
            AutorizadorCombo.ItemsSource = usuarios;
            AutorizadorCombo.SelectedItem = usuarios.FirstOrDefault(u => u.NombreUsuario == _guia.UsuarioEmisor)
                ?? usuarios.FirstOrDefault();
            _cargando = false;
        }

        private void Almacen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cargando || AlmacenCombo.SelectedItem is not AlmacenStock almacen || almacen.IdAlmacen == _guia.IdAlmacen)
                return;

            GuiaInterna? nueva = _negocio.PrepararManual(almacen.IdAlmacen);
            if (nueva == null)
            {
                NotificationService.Warning("No se pudo cargar el almacén.");
                return;
            }

            nueva.Detalles.Clear();
            nueva.UsuarioEmisor = _guia.UsuarioEmisor;
            nueva.UsuarioAutorizador = _guia.UsuarioAutorizador;
            nueva.FechaEmision = _guia.FechaEmision;
            nueva.MotivoEmisionManual = _guia.MotivoEmisionManual;
            nueva.IdCliente = _guia.IdCliente;
            nueva.EmpresaDestino = _guia.EmpresaDestino;
            nueva.RucDestino = _guia.RucDestino;
            nueva.Observacion = _guia.Observacion;
            _guia = nueva;
            DataContext = _guia;

            DetallesEdicion.Clear();
            DetallesEdicion.Add(CrearFila());
        }

        private void Motivo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MotivoCombo.SelectedItem is string motivo)
                _guia.MotivoEmisionManual = motivo;
        }

        private void ClienteBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_actualizandoCliente)
                return;

            _guia.IdCliente = null;
            _guia.EmpresaDestino = string.Empty;
            _guia.RucDestino = string.Empty;
            string texto = ClienteBusquedaTextBox.Text.Trim();
            if (texto.Length == 0)
            {
                ClientePopup.IsOpen = false;
                return;
            }

            ClientesListBox.ItemsSource = _todosLosClientes.Where(c =>
                c.NombreRazonSocial.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                c.NumeroDocumento.Contains(texto, StringComparison.OrdinalIgnoreCase)).Take(30).ToList();
            ClientePopup.IsOpen = ClientesListBox.Items.Count > 0;
        }

        private void ClienteBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Cliente? cliente = (e.OriginalSource as FrameworkElement)?.DataContext as Cliente
                ?? ClientesListBox.SelectedItem as Cliente;
            if (cliente == null)
                return;

            _guia.IdCliente = cliente.IdCliente;
            _guia.EmpresaDestino = cliente.NombreRazonSocial;
            _guia.RucDestino = cliente.NumeroDocumento;
            _actualizandoCliente = true;
            ClienteBusquedaTextBox.Text = cliente.ClienteBusqueda;
            ClienteBusquedaTextBox.CaretIndex = ClienteBusquedaTextBox.Text.Length;
            _actualizandoCliente = false;
            ClientePopup.IsOpen = false;
        }

        private void LimpiarCliente_Click(object sender, RoutedEventArgs e)
        {
            _guia.IdCliente = null;
            _guia.EmpresaDestino = string.Empty;
            _guia.RucDestino = string.Empty;
            _actualizandoCliente = true;
            ClienteBusquedaTextBox.Clear();
            _actualizandoCliente = false;
            ClientePopup.IsOpen = false;
        }

        private GuiaInternaManualDetalleViewModel CrearFila() => new(BuscarProductos);

        private ObservableCollection<ProductoStockBusqueda> BuscarProductos(string texto)
        {
            ObservableCollection<ProductoStockBusqueda> resultado = [];
            int idAlmacen = (AlmacenCombo?.SelectedItem as AlmacenStock)?.IdAlmacen ?? _guia.IdAlmacen;

            foreach (ProductoStockBusqueda producto in _almacenes.BuscarProductos(idAlmacen, texto))
            {
                if (!DetallesEdicion.Any(d => d.IdProducto == producto.IdProducto))
                    resultado.Add(producto);
            }

            return resultado;
        }

        private void AgregarProducto_Click(object sender, RoutedEventArgs e) => DetallesEdicion.Add(CrearFila());

        private void QuitarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: GuiaInternaManualDetalleViewModel detalle })
                DetallesEdicion.Remove(detalle);

            if (DetallesEdicion.Count == 0)
                DetallesEdicion.Add(CrearFila());
        }

        private void ProductoBusquedaListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ProductoStockBusqueda? producto = (e.OriginalSource as FrameworkElement)?.DataContext as ProductoStockBusqueda;
            if (sender is not ListBox listBox || listBox.DataContext is not GuiaInternaManualDetalleViewModel detalle)
                return;

            producto ??= listBox.SelectedItem as ProductoStockBusqueda;
            if (producto == null)
                return;

            if (producto.StockActual <= 0)
            {
                detalle.ProductoDropdownAbierto = false;
                NotificationService.Warning("Producto seleccionado no tiene stock.");
                return;
            }

            detalle.AsignarProducto(producto);
        }

        private void CargaMasiva_Click(object sender, RoutedEventArgs e)
        {
            if (AlmacenCombo.SelectedItem is not AlmacenStock)
            {
                NotificationService.Warning("Seleccione un almacén antes de realizar la carga masiva.");
                return;
            }

            CargaMasivaProductosWindow ventana = new("Carga masiva de productos para guía", BuscarProductoCargaMasiva)
            {
                Owner = this
            };
            if (ventana.ShowDialog() != true)
                return;

            int procesados = 0;
            decimal unidades = 0;
            foreach (CargaMasivaProductoFila fila in ventana.ProductosSeleccionados)
            {
                if (fila.Producto is not ProductoStockBusqueda producto)
                    continue;

                AgregarProductoCargaMasiva(producto, fila.Cantidad);
                procesados++;
                unidades += fila.Cantidad;
            }

            NotificationService.Success($"Carga masiva aplicada. Productos procesados: {procesados}. Unidades agregadas: {unidades:N2}. Errores encontrados: {ventana.ErroresEncontrados}");
        }

        private CargaMasivaProductoInfo? BuscarProductoCargaMasiva(string codigo)
        {
            int idAlmacen = (AlmacenCombo.SelectedItem as AlmacenStock)?.IdAlmacen ?? 0;
            ProductoStockBusqueda? producto = _almacenes.BuscarProductos(idAlmacen, codigo.Trim())
                .FirstOrDefault(p => p.Codigo.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));

            if (producto == null)
                return null;

            if (producto.StockActual <= 0)
            {
                NotificationService.Warning("Producto seleccionado no tiene stock.");
                return null;
            }

            return new CargaMasivaProductoInfo
            {
                IdProducto = producto.IdProducto,
                Codigo = producto.Codigo,
                NombreProducto = producto.NombreProducto,
                Producto = producto
            };
        }

        private void AgregarProductoCargaMasiva(ProductoStockBusqueda producto, decimal cantidad)
        {
            GuiaInternaManualDetalleViewModel? existente = DetallesEdicion.FirstOrDefault(d => d.IdProducto == producto.IdProducto);
            if (existente != null)
            {
                existente.CantidadDespachar += cantidad;
                return;
            }

            GuiaInternaManualDetalleViewModel fila = DetallesEdicion.FirstOrDefault(d => d.IdProducto == 0) ?? CrearFila();
            if (!DetallesEdicion.Contains(fila))
                DetallesEdicion.Add(fila);
            fila.AsignarProducto(producto);
            fila.CantidadDespachar = cantidad;
        }

        private void Emitir_Click(object sender, RoutedEventArgs e)
        {
            DetallesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            DetallesGrid.CommitEdit(DataGridEditingUnit.Row, true);

            if (AlmacenCombo.SelectedItem is AlmacenStock almacen)
                _guia.IdAlmacen = almacen.IdAlmacen;
            if (AutorizadorCombo.SelectedItem is Usuario usuario)
                _guia.UsuarioAutorizador = usuario.NombreUsuario;
            _guia.MotivoEmisionManual = MotivoCombo.SelectedItem as string ?? string.Empty;

            _guia.Detalles = DetallesEdicion.Where(d => d.IdProducto > 0).Select(d => d.ToEntity()).ToList();
            if (_guia.Detalles.Any(d => d.StockActual <= 0))
            {
                NotificationService.Warning("Producto seleccionado no tiene stock.");
                return;
            }

            string mensaje = _negocio.EmitirManual(_guia, out string numero);
            if (!mensaje.Contains("correctamente", StringComparison.OrdinalIgnoreCase))
            {
                NotificationService.Warning(mensaje);
                return;
            }

            _guia.NumeroGuia = numero;
            NotificationService.Success($"{mensaje} Stock y kardex actualizados.");
            DialogResult = true;
        }
    }
}
