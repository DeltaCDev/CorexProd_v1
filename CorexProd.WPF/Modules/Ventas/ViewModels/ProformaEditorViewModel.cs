using CorexProd.Entidad.Entidades;
using CorexProd.Negocio.Negocio;
using CorexProd.WPF.Commands;
using CorexProd.WPF.Helpers;
using CorexProd.WPF.Modules.Shared.Views;
using CorexProd.WPF.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace CorexProd.WPF.Modules.Ventas.ViewModels
{
    public class ProformaEditorViewModel : BaseViewModel
    {
        private const string ApiUrlPredeterminada = "https://ruc.com.pe/api/v1/consultas";
        private const string ApiTokenPredeterminado = "0a682fbe-009d-4758-aad1-2ff1092ab7c2-838d6cd5-620a-4add-9632-a3b37c5ae216";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private readonly ProformaNegocio _proformaNegocio = new();
        private readonly ClienteNegocio _clienteNegocio = new();
        private readonly ProductoNegocio _productoNegocio = new();
        private readonly List<Cliente> _todosLosClientes = [];

        private int _idProforma;
        private string _serieNumero = string.Empty;
        private DateTime _fechaEmision = DateTime.Today;
        private DateTime _fechaVencimiento = DateTime.Today;
        private string _ordenCompraCliente = string.Empty;
        private Cliente? _clienteSeleccionado;
        private Cliente? _clienteBusquedaResaltado;
        private string _textoBusquedaCliente = string.Empty;
        private bool _clienteDropdownAbierto;
        private string _observacion = string.Empty;
        private bool _mostrarClienteRapido;
        private string _nuevoClienteTipoDocumento = "RUC";
        private string _nuevoClienteNumeroDocumento = string.Empty;
        private string _nuevoClienteNombre = string.Empty;
        private string _nuevoClienteDireccion = string.Empty;
        private string _nuevoClienteTelefono = string.Empty;
        private string _nuevoClienteCorreo = string.Empty;
        private bool _isConsultandoNuevoClienteDocumento;
        private decimal _igvPorcentaje;
        private bool _igvActivo;
        private string _condicionTributaria = string.Empty;

        public ObservableCollection<Cliente> Clientes { get; } = [];
        public ObservableCollection<Producto> Productos { get; } = [];
        public ObservableCollection<ProformaDetalleItemViewModel> Detalles { get; } = [];

        public Action? CerrarVentana { get; set; }
        public bool Guardado { get; private set; }

        public int IdProforma
        {
            get => _idProforma;
            set
            {
                _idProforma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Titulo));
            }
        }

        public string Titulo => IdProforma > 0 ? "Editar Proforma" : "Nueva Proforma";

        public string SerieNumero
        {
            get => _serieNumero;
            set
            {
                _serieNumero = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaEmision
        {
            get => _fechaEmision;
            set
            {
                _fechaEmision = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaVencimiento
        {
            get => _fechaVencimiento;
            set
            {
                _fechaVencimiento = value;
                OnPropertyChanged();
            }
        }

        public string OrdenCompraCliente
        {
            get => _ordenCompraCliente;
            set
            {
                _ordenCompraCliente = value;
                OnPropertyChanged();
            }
        }

        public Cliente? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public Cliente? ClienteBusquedaResaltado
        {
            get => _clienteBusquedaResaltado;
            set
            {
                _clienteBusquedaResaltado = value;
                OnPropertyChanged();
            }
        }

        public string TextoBusquedaCliente
        {
            get => _textoBusquedaCliente;
            set
            {
                _textoBusquedaCliente = value;
                if (ClienteSeleccionado?.ClienteBusqueda != _textoBusquedaCliente)
                {
                    ClienteSeleccionado = null;
                }

                OnPropertyChanged();
                FiltrarClientes();
            }
        }

        public bool ClienteDropdownAbierto
        {
            get => _clienteDropdownAbierto;
            set
            {
                _clienteDropdownAbierto = value;
                OnPropertyChanged();
            }
        }

        public string Observacion
        {
            get => _observacion;
            set
            {
                _observacion = value;
                OnPropertyChanged();
            }
        }

        public bool MostrarClienteRapido
        {
            get => _mostrarClienteRapido;
            set
            {
                _mostrarClienteRapido = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClienteRapidoVisibility));
            }
        }

        public Visibility ClienteRapidoVisibility => MostrarClienteRapido ? Visibility.Visible : Visibility.Collapsed;

        public string NuevoClienteTipoDocumento
        {
            get => _nuevoClienteTipoDocumento;
            set
            {
                _nuevoClienteTipoDocumento = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteNumeroDocumento
        {
            get => _nuevoClienteNumeroDocumento;
            set
            {
                _nuevoClienteNumeroDocumento = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteNombre
        {
            get => _nuevoClienteNombre;
            set
            {
                _nuevoClienteNombre = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteDireccion
        {
            get => _nuevoClienteDireccion;
            set
            {
                _nuevoClienteDireccion = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteTelefono
        {
            get => _nuevoClienteTelefono;
            set
            {
                _nuevoClienteTelefono = value;
                OnPropertyChanged();
            }
        }

        public string NuevoClienteCorreo
        {
            get => _nuevoClienteCorreo;
            set
            {
                _nuevoClienteCorreo = value;
                OnPropertyChanged();
            }
        }

        public bool IsConsultandoNuevoClienteDocumento
        {
            get => _isConsultandoNuevoClienteDocumento;
            set
            {
                _isConsultandoNuevoClienteDocumento = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public decimal Subtotal => Detalles.Sum(d => d.Importe);
        public decimal Descuento => Detalles.Sum(d => d.Descuento);
        public decimal Igv => _igvActivo
            ? Math.Round(Subtotal * _igvPorcentaje / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        public decimal Total => Subtotal + Igv;
        public string EtiquetaIgv => _igvActivo ? $"IGV ({_igvPorcentaje:N2}%):" : "IGV:";
        public string CondicionTributaria => _condicionTributaria;

        public ICommand AgregarProductoCommand { get; }
        public ICommand CargaMasivaCommand { get; }
        public ICommand QuitarProductoCommand { get; }
        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand MostrarClienteRapidoCommand { get; }
        public ICommand ConsultarNuevoClienteDocumentoCommand { get; }
        public ICommand GuardarClienteRapidoCommand { get; }
        public ICommand CancelarClienteRapidoCommand { get; }

        public ProformaEditorViewModel(Proforma? proforma = null, bool copiar = false)
        {
            AgregarProductoCommand = new RelayCommand(_ => AgregarFila());
            CargaMasivaCommand = new RelayCommand(_ => AbrirCargaMasiva());
            QuitarProductoCommand = new RelayCommand(parametro => QuitarFila(parametro));
            GuardarCommand = new RelayCommand(_ => Guardar());
            CancelarCommand = new RelayCommand(_ => CerrarVentana?.Invoke());
            MostrarClienteRapidoCommand = new RelayCommand(_ => MostrarClienteRapido = true);
            ConsultarNuevoClienteDocumentoCommand = new RelayCommand(
                async _ => await ConsultarNuevoClienteDocumentoAsync(),
                _ => !IsConsultandoNuevoClienteDocumento);
            GuardarClienteRapidoCommand = new RelayCommand(_ => GuardarClienteRapido());
            CancelarClienteRapidoCommand = new RelayCommand(_ => LimpiarClienteRapido());

            CargarClientes();
            CargarProductos();

            (decimal porcentaje, bool activo, string condicion) = _proformaNegocio.ObtenerConfiguracionIgv();
            _igvPorcentaje = porcentaje;
            _igvActivo = activo;
            _condicionTributaria = condicion;

            if (proforma != null)
            {
                CargarProforma(proforma, copiar);
            }
            else
            {
                SerieNumero = _proformaNegocio.ObtenerSiguienteSerieNumero();
                AgregarFila();
            }
        }

        private void CargarClientes()
        {
            _todosLosClientes.Clear();
            Clientes.Clear();

            foreach (Cliente cliente in _clienteNegocio.Listar().Where(c => c.Estado))
            {
                _todosLosClientes.Add(cliente);
            }

            FiltrarClientes();
        }

        private void FiltrarClientes()
        {
            Clientes.Clear();
            ClienteBusquedaResaltado = null;

            string busqueda = TextoBusquedaCliente.Trim();

            if (busqueda.Length < 3)
            {
                ClienteDropdownAbierto = false;
                return;
            }

            foreach (Cliente cliente in _todosLosClientes
                .Where(c => CoincideCliente(c, busqueda))
                .Take(20))
            {
                Clientes.Add(cliente);
            }

            ClienteBusquedaResaltado = Clientes.FirstOrDefault();
            ClienteDropdownAbierto = Clientes.Count > 0;
        }

        public void SeleccionarClienteBusqueda()
        {
            SeleccionarClienteBusqueda(ClienteBusquedaResaltado);
        }

        public void SeleccionarClienteBusqueda(Cliente? cliente)
        {
            if (cliente == null)
            {
                return;
            }

            ClienteSeleccionado = cliente;
            _textoBusquedaCliente = ClienteSeleccionado.ClienteBusqueda;
            OnPropertyChanged(nameof(TextoBusquedaCliente));
            Clientes.Clear();
            ClienteDropdownAbierto = false;
        }

        private static bool CoincideCliente(Cliente cliente, string busqueda)
        {
            return Contiene(cliente.NombreRazonSocial, busqueda)
                || Contiene(cliente.NumeroDocumento, busqueda)
                || Contiene(cliente.TipoDocumento, busqueda);
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return valor?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) == true;
        }

        private void CargarProductos()
        {
            Productos.Clear();

            foreach (Producto producto in _productoNegocio.Listar().Where(p => p.Estado))
            {
                Productos.Add(producto);
            }
        }

        private void CargarProforma(Proforma proforma, bool copiar)
        {
            IdProforma = copiar ? 0 : proforma.IdProforma;
            SerieNumero = copiar ? _proformaNegocio.ObtenerSiguienteSerieNumero() : proforma.SerieNumero;
            FechaEmision = DateTime.Today;
            FechaVencimiento = DateTime.Today;
            OrdenCompraCliente = proforma.OrdenCompraCliente;
            ClienteSeleccionado = _todosLosClientes.FirstOrDefault(c => c.IdCliente == proforma.IdCliente);
            TextoBusquedaCliente = ClienteSeleccionado?.ClienteBusqueda ?? string.Empty;
            Observacion = proforma.Observacion;

            if (!copiar)
            {
                _igvPorcentaje = proforma.IgvPorcentaje;
                _igvActivo = !proforma.CondicionTributaria.Equals("Exonerado de IGV", StringComparison.OrdinalIgnoreCase);
                _condicionTributaria = proforma.CondicionTributaria;
                OnPropertyChanged(nameof(EtiquetaIgv));
                OnPropertyChanged(nameof(CondicionTributaria));
            }

            Detalles.Clear();

            foreach (ProformaDetalle detalle in proforma.Detalles)
            {
                ProformaDetalleItemViewModel item = CrearFilaDetalle();
                item.IdProformaDetalle = copiar ? 0 : detalle.IdProformaDetalle;
                item.CargarProducto(detalle.IdProducto);
                item.Cantidad = detalle.Cantidad;
                item.PrecioUnitario = detalle.PrecioUnitario;
                item.Descuento = detalle.Descuento;
                item.Observacion = detalle.Observacion;
                Detalles.Add(item);
            }

            if (Detalles.Count == 0)
            {
                AgregarFila();
            }

            NotificarTotales();
        }

        private void AgregarFila()
        {
            Detalles.Add(CrearFilaDetalle());
            NotificarTotales();
        }

        private ProformaDetalleItemViewModel CrearFilaDetalle()
        {
            ProformaDetalleItemViewModel item = new(Productos);
            item.TotalesActualizados += NotificarTotales;
            item.ProductoCambiado += ValidarProductoRepetido;
            return item;
        }

        private void QuitarFila(object? parametro)
        {
            if (parametro is not ProformaDetalleItemViewModel item)
            {
                return;
            }

            if (Detalles.Count == 1)
            {
                NotificationService.Warning("Debe mantener al menos una fila de producto");
                return;
            }

            Detalles.Remove(item);
            NotificarTotales();
        }

        private void AbrirCargaMasiva()
        {
            CargaMasivaProductosWindow ventana = new(
                $"Carga masiva de productos - {SerieNumero}",
                BuscarProductoCargaMasiva,
                ampliarVentana: true)
            {
                Owner = Application.Current.MainWindow
            };

            if (ventana.ShowDialog() != true)
            {
                return;
            }

            int productosProcesados = 0;
            decimal unidadesAgregadas = 0;

            foreach (CargaMasivaProductoFila fila in ventana.ProductosSeleccionados)
            {
                if (fila.Producto is not Producto producto)
                {
                    continue;
                }

                AgregarProductoCargaMasiva(producto, fila.Cantidad);
                productosProcesados++;
                unidadesAgregadas += fila.Cantidad;
            }

            NotificarTotales();
            NotificationService.Success($"Carga masiva aplicada. Productos procesados: {productosProcesados}. Unidades agregadas: {unidadesAgregadas:N2}. Errores encontrados: {ventana.ErroresEncontrados}");
        }

        private CargaMasivaProductoInfo? BuscarProductoCargaMasiva(string codigo)
        {
            Producto? producto = Productos.FirstOrDefault(p =>
                p.Codigo.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));

            if (producto == null)
            {
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

        private void AgregarProductoCargaMasiva(Producto producto, decimal cantidad)
        {
            ProformaDetalleItemViewModel? filaExistente = Detalles.FirstOrDefault(d => d.IdProducto == producto.IdProducto);

            if (filaExistente != null)
            {
                filaExistente.Cantidad += cantidad;
                return;
            }

            ProformaDetalleItemViewModel fila = Detalles.FirstOrDefault(d => d.IdProducto == 0) ?? CrearFilaDetalle();

            if (!Detalles.Contains(fila))
            {
                Detalles.Add(fila);
            }

            fila.CargarProducto(producto.IdProducto);
            fila.Cantidad = cantidad;
            fila.Observacion = producto.Descripcion;
        }

        private void ValidarProductoRepetido(ProformaDetalleItemViewModel filaActual, Producto producto)
        {
            ProformaDetalleItemViewModel? filaExistente = Detalles
                .FirstOrDefault(d => !ReferenceEquals(d, filaActual) && d.IdProducto == producto.IdProducto);

            if (filaExistente == null)
            {
                return;
            }

            bool unir = ConfirmDialogService.Confirmar(
                "El producto ya existe en otra fila. ¿Desea unir la cantidad a la fila anterior?",
                "Producto repetido");

            if (unir)
            {
                filaExistente.Cantidad += filaActual.Cantidad;
                filaActual.CargarProducto(0);
                NotificarTotales();
            }
        }

        private void GuardarClienteRapido()
        {
            Cliente cliente = new()
            {
                TipoDocumento = NuevoClienteTipoDocumento,
                NumeroDocumento = NuevoClienteNumeroDocumento,
                NombreRazonSocial = NuevoClienteNombre,
                Direccion = NuevoClienteDireccion,
                Telefono = NuevoClienteTelefono,
                Correo = NuevoClienteCorreo,
                Estado = true
            };

            string mensaje = _clienteNegocio.Guardar(cliente);

            if (!mensaje.Contains("correctamente"))
            {
                NotificationService.Warning(mensaje);
                return;
            }

            NotificationService.Success(mensaje);
            CargarClientes();
            ClienteSeleccionado = _todosLosClientes
                .FirstOrDefault(c => c.NumeroDocumento == NuevoClienteNumeroDocumento.Trim()
                    && c.NombreRazonSocial == NuevoClienteNombre.Trim());
            TextoBusquedaCliente = ClienteSeleccionado?.ClienteBusqueda ?? string.Empty;
            LimpiarClienteRapido();
        }

        private async Task ConsultarNuevoClienteDocumentoAsync()
        {
            string tipoDocumento = NuevoClienteTipoDocumento.Trim().ToUpperInvariant();
            string numeroDocumento = NuevoClienteNumeroDocumento.Trim();

            if (tipoDocumento != "DNI" && tipoDocumento != "RUC")
            {
                NotificationService.Warning("La consulta solo esta disponible para DNI y RUC");
                return;
            }

            int longitudEsperada = tipoDocumento == "DNI" ? 8 : 11;

            if (numeroDocumento.Length != longitudEsperada || !numeroDocumento.All(char.IsDigit))
            {
                NotificationService.Warning($"Ingrese un {tipoDocumento} valido de {longitudEsperada} digitos");
                return;
            }

            try
            {
                IsConsultandoNuevoClienteDocumento = true;

                using JsonDocument respuesta = await ConsultarApiDocumentoAsync(tipoDocumento, numeroDocumento);
                JsonElement raiz = respuesta.RootElement;

                if (!ObtenerBooleano(raiz, "success"))
                {
                    NotificationService.Warning(ObtenerTexto(raiz, "message", "mensaje", "error") ?? "No se encontro informacion para el documento");
                    return;
                }

                string? nombre = ObtenerTexto(raiz, "nombre_completo", "nombre_o_razon_social", "razon_social", "nombre");
                string? direccion = ObtenerTexto(raiz, "direccion", "domicilio_fiscal");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    NotificationService.Warning("La API respondio correctamente, pero no envio nombre o razon social");
                    return;
                }

                NuevoClienteNombre = nombre;

                if (!string.IsNullOrWhiteSpace(direccion))
                {
                    NuevoClienteDireccion = direccion;
                }

                NotificationService.Success("Datos consultados correctamente");
            }
            catch (TaskCanceledException)
            {
                NotificationService.Error("La consulta demoro demasiado. Intente nuevamente");
            }
            catch (HttpRequestException)
            {
                NotificationService.Error("No se pudo conectar con la API de consulta");
            }
            catch (JsonException)
            {
                NotificationService.Error("La API devolvio una respuesta no valida");
            }
            finally
            {
                IsConsultandoNuevoClienteDocumento = false;
            }
        }

        private static async Task<JsonDocument> ConsultarApiDocumentoAsync(string tipoDocumento, string numeroDocumento)
        {
            string apiUrl = ConfigurationManager.AppSettings["RucComPeApiUrl"] ?? ApiUrlPredeterminada;
            string token = ConfigurationManager.AppSettings["RucComPeApiToken"] ?? ApiTokenPredeterminado;
            string campoDocumento = tipoDocumento == "RUC" ? "ruc" : "dni";

            var payload = new Dictionary<string, string>
            {
                ["token"] = token,
                [campoDocumento] = numeroDocumento
            };

            string json = JsonSerializer.Serialize(payload);
            using StringContent contenido = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await HttpClient.PostAsync(apiUrl, contenido);

            await using Stream stream = await response.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(stream);
        }

        private static bool ObtenerBooleano(JsonElement elemento, string propiedad)
        {
            if (!elemento.TryGetProperty(propiedad, out JsonElement valor))
            {
                return false;
            }

            return valor.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(valor.GetString(), out bool resultado) && resultado,
                _ => false
            };
        }

        private static string? ObtenerTexto(JsonElement elemento, params string[] propiedades)
        {
            foreach (string propiedad in propiedades)
            {
                if (elemento.TryGetProperty(propiedad, out JsonElement valor) && valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString();
                }
            }

            return null;
        }

        private void LimpiarClienteRapido()
        {
            MostrarClienteRapido = false;
            NuevoClienteTipoDocumento = "RUC";
            NuevoClienteNumeroDocumento = string.Empty;
            NuevoClienteNombre = string.Empty;
            NuevoClienteDireccion = string.Empty;
            NuevoClienteTelefono = string.Empty;
            NuevoClienteCorreo = string.Empty;
        }

        private void Guardar()
        {
            Proforma proforma = new()
            {
                IdProforma = IdProforma,
                FechaEmision = FechaEmision,
                FechaVencimiento = FechaVencimiento,
                OrdenCompraCliente = OrdenCompraCliente,
                IdCliente = ClienteSeleccionado?.IdCliente ?? 0,
                Observacion = Observacion,
                IgvPorcentaje = _igvPorcentaje,
                CondicionTributaria = _condicionTributaria,
                UsuarioGenerador = SessionManager.UsuarioActual?.NombreUsuario ?? "Sistema",
                Detalles = Detalles.Select(d => d.CrearDetalle()).ToList()
            };

            string mensaje = _proformaNegocio.Guardar(proforma);

            if (mensaje.Contains("correctamente"))
            {
                Guardado = true;
                NotificationService.Success(mensaje);
                CerrarVentana?.Invoke();
            }
            else
            {
                NotificationService.Warning(mensaje);
            }
        }

        private void NotificarTotales()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Descuento));
            OnPropertyChanged(nameof(Igv));
            OnPropertyChanged(nameof(Total));
        }
    }
}
