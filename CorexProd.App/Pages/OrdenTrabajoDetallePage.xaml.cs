using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CorexProd.App.Models;
using CorexProd.App.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CorexProd.App.Pages;

[QueryProperty(nameof(IdOrden), "id")]
public partial class OrdenTrabajoDetallePage : ContentPage
{
    private readonly CorexProdApiClient _apiClient;
    private readonly SessionState _session;
    private readonly ObservableCollection<OrdenTrabajoAreaItem> _areasVisibles = [];
    private readonly ObservableCollection<AreaFiltro> _areasFiltro = [];
    private OrdenTrabajoDetalleResponse? _detalleActual;
    private IDispatcherTimer? _refreshTimer;
    private bool _isRefreshing;
    private bool _isOperating;
    private int? _areaSeleccionadaDespuesOperacion;
    private int? _areaSeleccionadaActual;
    private int _id;

    public OrdenTrabajoDetallePage()
    {
        InitializeComponent();
        _apiClient = ServiceHelper.GetRequiredService<CorexProdApiClient>();
        _session = ServiceHelper.GetRequiredService<SessionState>();
        AreasTabsView.ItemsSource = _areasFiltro;
        AreasView.ItemsSource = _areasVisibles;
    }

    public string IdOrden
    {
        set => _id = int.TryParse(value, out int id) ? id : 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _refreshTimer ??= CrearTimer();
        _refreshTimer.Start();
        await CargarDetalleAsync(_id);
    }

    protected override void OnDisappearing()
    {
        _refreshTimer?.Stop();
        base.OnDisappearing();
    }

    private async void OnRefreshing(object? sender, EventArgs e) => await CargarDetalleAsync(_id);

    private async void OnKardexClicked(object? sender, EventArgs e)
    {
        if (_detalleActual == null)
            return;

        try
        {
            ApiListResponse<OrdenTrabajoKardexItem> response = _session.EsDemo
                ? new ApiListResponse<OrdenTrabajoKardexItem>(0, [])
                : await _apiClient.GetOrdenTrabajoKardexAsync(_detalleActual.Cabecera.IdOrdenTrabajo);
            await Navigation.PushModalAsync(CrearKardexPage(response.Items));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Kardex", ex.Message, "OK");
        }
    }

    private async void OnHistorialClicked(object? sender, EventArgs e)
    {
        if (_detalleActual == null)
            return;

        try
        {
            ApiListResponse<OrdenTrabajoMovimientoItem> response = _session.EsDemo
                ? new ApiListResponse<OrdenTrabajoMovimientoItem>(0, [])
                : await _apiClient.GetOrdenTrabajoMovimientosAsync(_detalleActual.Cabecera.IdOrdenTrabajo);
            await Navigation.PushModalAsync(CrearHistorialPage(response.Items));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Historial", ex.Message, "OK");
        }
    }

    private static ContentPage CrearKardexPage(IReadOnlyList<OrdenTrabajoKardexItem> items)
    {
        VerticalStackLayout contenido = new() { Padding = 14, Spacing = 12 };
        contenido.Add(new Label { Text = "Kardex de OT", FontFamily = "OpenSansSemibold", FontSize = 22, TextColor = Color.FromArgb("#101828") });
        contenido.Add(new Label { Text = $"{items.Count} ingreso(s) a kardex", FontSize = 12, TextColor = Color.FromArgb("#667085") });

        foreach (OrdenTrabajoKardexItem item in items)
            contenido.Add(CrearKardexCard(item));

        if (items.Count == 0)
            contenido.Add(new Label { Text = "No hay ingresos a kardex para esta OT.", Padding = 12, TextColor = Color.FromArgb("#667085") });

        return CrearModalListado("Kardex", contenido);
    }

    private static ContentPage CrearHistorialPage(IReadOnlyList<OrdenTrabajoMovimientoItem> items)
    {
        VerticalStackLayout contenido = new() { Padding = 14, Spacing = 12 };
        contenido.Add(new Label { Text = "Historial de movimientos", FontFamily = "OpenSansSemibold", FontSize = 22, TextColor = Color.FromArgb("#101828") });

        Picker productoPicker = CrearFiltroPicker(
            "Todas las prendas",
            items.Select(x => $"{x.CodigoProducto} - {x.NombreProducto}").Distinct().OrderBy(x => x));
        Picker usuarioPicker = CrearFiltroPicker(
            "Todos los usuarios",
            items.Select(x => x.Usuario).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x));
        Picker areaPicker = CrearFiltroPicker(
            "Todas las areas",
            items.SelectMany(x => new[] { x.Origen, x.Destino }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x));
        Entry desdeEntry = new() { Placeholder = "Desde dd/mm/aaaa", BackgroundColor = Colors.White, Keyboard = Keyboard.Text };
        Entry hastaEntry = new() { Placeholder = "Hasta dd/mm/aaaa", BackgroundColor = Colors.White, Keyboard = Keyboard.Text };
        Label contador = new() { FontSize = 12, TextColor = Color.FromArgb("#667085") };

        Grid filtros = new()
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowSpacing = 8,
            ColumnSpacing = 8
        };
        filtros.Add(productoPicker, 0, 0);
        filtros.SetColumnSpan(productoPicker, 2);
        filtros.Add(usuarioPicker, 0, 1);
        filtros.Add(areaPicker, 1, 1);
        filtros.Add(desdeEntry, 0, 2);
        filtros.Add(hastaEntry, 1, 2);
        contenido.Add(filtros);

        Button aplicar = new() { Text = "Aplicar filtros", BackgroundColor = Color.FromArgb("#7A5AF8"), TextColor = Colors.White };
        contenido.Add(aplicar);
        contenido.Add(contador);
        VerticalStackLayout lista = new() { Spacing = 10 };
        contenido.Add(lista);

        Button cerrar = new() { Text = "Cerrar", BackgroundColor = Color.FromArgb("#3F1D95"), TextColor = Colors.White };
        contenido.Add(cerrar);

        ContentPage page = new()
        {
            Title = "Historial",
            BackgroundColor = Color.FromArgb("#F4F6F8"),
            Content = new ScrollView { Content = contenido }
        };
        cerrar.Clicked += async (_, _) => await page.Navigation.PopModalAsync();
        aplicar.Clicked += (_, _) => AplicarFiltros();
        AplicarFiltros();
        return page;

        void AplicarFiltros()
        {
            string producto = productoPicker.SelectedIndex <= 0 ? string.Empty : productoPicker.SelectedItem?.ToString() ?? string.Empty;
            string usuario = usuarioPicker.SelectedIndex <= 0 ? string.Empty : usuarioPicker.SelectedItem?.ToString() ?? string.Empty;
            string area = areaPicker.SelectedIndex <= 0 ? string.Empty : areaPicker.SelectedItem?.ToString() ?? string.Empty;
            DateTime? desde = ParseFechaFiltro(desdeEntry.Text);
            DateTime? hasta = ParseFechaFiltro(hastaEntry.Text)?.Date.AddDays(1).AddTicks(-1);

            List<OrdenTrabajoMovimientoItem> filtrados = items
                .Where(x => string.IsNullOrWhiteSpace(producto) || $"{x.CodigoProducto} - {x.NombreProducto}".Equals(producto, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(usuario) || x.Usuario.Equals(usuario, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(area) || x.Origen.Equals(area, StringComparison.OrdinalIgnoreCase) || x.Destino.Equals(area, StringComparison.OrdinalIgnoreCase))
                .Where(x => !desde.HasValue || x.FechaHora >= desde.Value)
                .Where(x => !hasta.HasValue || x.FechaHora <= hasta.Value)
                .OrderBy(x => x.CodigoProducto)
                .ThenBy(x => x.FechaHora)
                .ThenBy(x => x.Origen)
                .ThenBy(x => x.Destino)
                .ToList();

            lista.Children.Clear();
            foreach (OrdenTrabajoMovimientoItem item in filtrados)
                lista.Add(CrearMovimientoCard(item, area));

            if (filtrados.Count == 0)
                lista.Add(new Label { Text = "No hay movimientos con los filtros seleccionados.", Padding = 12, TextColor = Color.FromArgb("#667085") });

            contador.Text = string.IsNullOrWhiteSpace(area)
                ? $"{filtrados.Count} movimiento(s)"
                : $"{filtrados.Count} movimiento(s) de entrada/salida en {area}";
        }
    }

    private IDispatcherTimer CrearTimer()
    {
        IDispatcherTimer timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(15);
        timer.Tick += async (_, _) => await CargarDetalleAsync(_id, silencioso: true);
        return timer;
    }

    private async Task CargarDetalleAsync(int idOrdenTrabajo, bool silencioso = false, bool forzar = false)
    {
        if (idOrdenTrabajo <= 0 || _isRefreshing || (_isOperating && !forzar))
            return;

        try
        {
            _isRefreshing = true;
            Refresh.IsRefreshing = true;
            int? areaSeleccionada = _areaSeleccionadaDespuesOperacion
                ?? _areaSeleccionadaActual;
            _areaSeleccionadaDespuesOperacion = null;
            _detalleActual = _session.EsDemo
                ? DemoData.OrdenTrabajoDetalle(idOrdenTrabajo)
                : await _apiClient.GetOrdenTrabajoDetalleAsync(idOrdenTrabajo);
            TituloDetalleLabel.Text = _detalleActual.Cabecera.NumeroOT;
            ClienteDetalleLabel.Text = _detalleActual.Cabecera.NombreCliente;
            OciDetalleLabel.Text = _detalleActual.Cabecera.NumeroOci;
            OcClienteDetalleLabel.Text = TextoVacio(_detalleActual.Cabecera.OrdenCompraCliente);
            EstadoDetalleLabel.Text = _detalleActual.Cabecera.Estado;
            ActualizadoLabel.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
            CargarAreas(areaSeleccionada);
        }
        catch (Exception ex)
        {
            if (!silencioso)
                await DisplayAlertAsync("OT Producción", ex.Message, "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
            _isRefreshing = false;
        }
    }

    private void CargarAreas(int? areaSeleccionada = null)
    {
        List<AreaFiltro> areas = (_detalleActual?.Areas ?? [])
            .GroupBy(x => new { x.IdAreaProduccion, x.NombreArea, x.OrdenSecuencia })
            .OrderBy(x => x.Key.OrdenSecuencia)
            .Select(x => new AreaFiltro(x.Key.IdAreaProduccion, x.Key.NombreArea, x.Key.OrdenSecuencia, false))
            .ToList();

        AreaFiltro? seleccion = areas.FirstOrDefault(x => x.IdAreaProduccion == areaSeleccionada)
            ?? areas.FirstOrDefault();
        _areasFiltro.Clear();
        foreach (AreaFiltro area in areas)
            _areasFiltro.Add(area with { EstaSeleccionada = seleccion?.IdAreaProduccion == area.IdAreaProduccion });

        _areaSeleccionadaActual = seleccion?.IdAreaProduccion;
        MostrarAreaSeleccionada();
    }

    private void OnAreaTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not AreaFiltro selected)
            return;

        _areaSeleccionadaActual = selected.IdAreaProduccion;
        for (int i = 0; i < _areasFiltro.Count; i++)
            _areasFiltro[i] = _areasFiltro[i] with { EstaSeleccionada = _areasFiltro[i].IdAreaProduccion == selected.IdAreaProduccion };

        MostrarAreaSeleccionada();
    }

    private void MostrarAreaSeleccionada()
    {
        _areasVisibles.Clear();
        if (_detalleActual == null)
            return;

        AreaFiltro? area = _areasFiltro.FirstOrDefault(x => x.IdAreaProduccion == _areaSeleccionadaActual)
            ?? _areasFiltro.FirstOrDefault();
        if (area == null)
            return;

        bool esPrimeraArea = _detalleActual.Areas
            .GroupBy(x => x.IdAreaProduccion)
            .OrderBy(x => x.Min(y => y.OrdenSecuencia))
            .Select(x => x.Key)
            .FirstOrDefault() == area.IdAreaProduccion;

        foreach (OrdenTrabajoArea item in _detalleActual.Areas
                     .Where(x => x.IdAreaProduccion == area.IdAreaProduccion)
                     .OrderBy(x => ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto).Cliente)
                     .ThenBy(x => ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto).NumeroNuloOrden)
                     .ThenBy(x => ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto).Numero)
                     .ThenBy(x => ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto).Variante)
                     .ThenBy(x => ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto).OrdenTalla)
                     .ThenBy(x => ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto).TallaNumero)
                     .ThenBy(x => ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto).CodigoOrden)
                     .ThenBy(x => ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto).NombreProducto))
        {
            OrdenTrabajoProducto? producto = _detalleActual.Detalles.FirstOrDefault(x => x.IdDetalleOT == item.IdDetalleOT);
            _areasVisibles.Add(new OrdenTrabajoAreaItem(item, producto, esPrimeraArea));
        }
    }

    private async void OnIniciarClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OrdenTrabajoAreaItem item || _detalleActual == null)
            return;

        OrdenTrabajoArea area = item.Area;
        OrdenTrabajoProducto? producto = item.Producto;
        if (producto == null || !producto.Estado.Equals("PENDIENTE", StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync("OT Producción", "Este producto ya fue iniciado.", "OK");
            return;
        }

        decimal cantidad = await PedirCantidadAsync("Iniciar producción", area.CantidadPendiente, permitirMayor: true);
        if (cantidad <= 0)
            return;

        int? idAreaDestino = ObtenerIdSiguienteArea(area);
        string destino = idAreaDestino.HasValue ? ObtenerDestino(area) : "siguiente area";
        bool confirmar = await DisplayAlertAsync(
            "Confirmar",
            $"Se iniciaran {cantidad:N2} y se transferiran automaticamente desde {area.NombreArea} hacia {destino}.",
            "Confirmar",
            "Cancelar");
        if (!confirmar)
            return;

        _areaSeleccionadaDespuesOperacion = idAreaDestino;
        await EjecutarOperacionAsync(async idUsuario =>
        {
            if (_session.EsDemo)
                return new OperacionOrdenTrabajoResponse("Inicio y transferencia automatica simulados en modo demo.", 1001);

            OrdenTrabajoLanzarRequest request = new(
                idUsuario,
                idUsuario,
                [new(area.IdDetalleOT, cantidad, cantidad != producto.CantidadPlanificada ? "AJUSTE DESDE ANDROID" : string.Empty, "Inicio desde Android")]);

            await _apiClient.LanzarOrdenTrabajoAsync(_detalleActual.Cabecera.IdOrdenTrabajo, request);

            OrdenTrabajoTransferirRequest transferencia = new(
                area.IdAreaProduccion,
                idUsuario,
                idUsuario,
                false,
                "Inicio y transferencia automatica desde Android",
                [new(area.IdDetalleOT, cantidad)]);

            OperacionOrdenTrabajoResponse response = await _apiClient.TransferirOrdenTrabajoAsync(_detalleActual.Cabecera.IdOrdenTrabajo, transferencia);
            return response with { Mensaje = $"Produccion iniciada y transferida a {destino} correctamente." };
        });
    }

    private async void OnTransferirClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OrdenTrabajoAreaItem item || _detalleActual == null)
            return;

        OrdenTrabajoArea area = item.Area;
        if (!area.Disponible)
        {
            await DisplayAlertAsync("OT Producción", "No hay cantidad pendiente para mover en esta área.", "OK");
            return;
        }

        decimal cantidad = await PedirCantidadAsync(area.EsTermino ? "Terminar producto" : "Transferir producto", area.CantidadPendiente);
        if (cantidad <= 0)
            return;

        string destino = area.EsTermino ? "productos terminados" : ObtenerDestino(area);
        bool confirmar = await DisplayAlertAsync("Confirmar", $"Mover {cantidad:N2} desde {area.NombreArea} hacia {destino}.", "Confirmar", "Cancelar");
        if (!confirmar)
            return;

        await EjecutarOperacionAsync(async idUsuario =>
        {
            if (_session.EsDemo)
                return new OperacionOrdenTrabajoResponse("Transferencia simulada en modo demo.", 1001);

            OrdenTrabajoTransferirRequest request = new(
                area.IdAreaProduccion,
                idUsuario,
                idUsuario,
                area.EsTermino,
                "Movimiento desde Android",
                [new(area.IdDetalleOT, cantidad)]);

            return await _apiClient.TransferirOrdenTrabajoAsync(_detalleActual.Cabecera.IdOrdenTrabajo, request);
        }, evaluarTerminacion: area.EsTermino);
    }

    private async void OnMermaClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OrdenTrabajoAreaItem item || _detalleActual == null)
            return;

        OrdenTrabajoArea area = item.Area;
        if (!area.ManejaMerma)
        {
            await DisplayAlertAsync("OT Producción", "Esta área no tiene habilitado registro de merma.", "OK");
            return;
        }

        decimal cantidad = await PedirCantidadAsync("Registrar merma", area.CantidadPendiente);
        if (cantidad <= 0)
            return;

        string observacion = await DisplayPromptAsync("Motivo de merma", "Ingrese la observación", "Guardar", "Cancelar", "Observación", maxLength: 200) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(observacion))
            return;

        await EjecutarOperacionAsync(async idUsuario =>
        {
            if (_session.EsDemo)
                return new OperacionOrdenTrabajoResponse("Merma registrada en modo demo.", null);

            OrdenTrabajoMermaRequest request = new(area.IdDetalleArea, cantidad, "MERMA EN OPERACION", observacion, idUsuario, idUsuario);
            return await _apiClient.RegistrarMermaOrdenTrabajoAsync(_detalleActual.Cabecera.IdOrdenTrabajo, request);
        });
    }

    private async void OnFichaTecnicaClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not OrdenTrabajoAreaItem item)
            return;

        await AbrirFichaTecnicaAsync(item.Area.CodigoProducto);
    }

    private async Task AbrirFichaTecnicaAsync(string codigoProducto)
    {
        try
        {
            if (_session.EsDemo)
            {
                await DisplayAlertAsync("Ficha tecnica demo", $"Se abriria la ficha tecnica de {codigoProducto}.", "OK");
                return;
            }

            FichaTecnicaInfo info = await _apiClient.GetFichaTecnicaInfoAsync(codigoProducto);
            if (!info.Disponible)
            {
                await DisplayAlertAsync("Ficha tecnica", "La ficha tecnica esta registrada, pero el archivo no esta disponible.", "OK");
                return;
            }

            await Launcher.Default.OpenAsync(_apiClient.GetFichaTecnicaUrl(codigoProducto));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ficha tecnica", ex.Message, "OK");
        }
    }

    private async Task EjecutarOperacionAsync(Func<int, Task<OperacionOrdenTrabajoResponse>> operacion, bool evaluarTerminacion = false)
    {
        try
        {
            _isOperating = true;
            int completadosAntes = _detalleActual?.Detalles.Count(x => EsTerminado(x.Estado)) ?? 0;
            string estadoAntes = _detalleActual?.Cabecera.Estado ?? string.Empty;
            int idUsuario = _session.Usuario?.IdUsuario ?? 0;
            OperacionOrdenTrabajoResponse response = await operacion(idUsuario);
            await DisplayAlertAsync("OT Producción", response.IdOperacion.HasValue ? $"{response.Mensaje}\nOperación #{response.IdOperacion}" : response.Mensaje, "OK");
            await CargarDetalleAsync(_id, forzar: true);

            if (evaluarTerminacion)
                await MostrarResultadoTerminacionAsync(completadosAntes, estadoAntes);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("OT Producción", ex.Message, "OK");
        }
        finally
        {
            _isOperating = false;
        }
    }

    private async Task MostrarResultadoTerminacionAsync(int completadosAntes, string estadoAntes)
    {
        if (_detalleActual == null)
            return;

        int totalProductos = _detalleActual.Detalles.Count;
        int completadosDespues = _detalleActual.Detalles.Count(x => EsTerminado(x.Estado));

        if (totalProductos > 0 && completadosDespues > completadosAntes)
            await DisplayAlertAsync("Producto completado", $"{completadosDespues} de {totalProductos} completado.", "OK");

        bool estabaTerminada = EsTerminado(estadoAntes);
        bool estaTerminada = EsTerminado(_detalleActual.Cabecera.Estado);
        if (!estabaTerminada && estaTerminada)
        {
            string detalle = CrearDetalleParciales(_detalleActual);
            await DisplayAlertAsync("OT completa", detalle, "OK");
        }
    }

    private static bool EsTerminado(string? estado)
        => string.Equals(estado, "TERMINADA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, "TERMINADO", StringComparison.OrdinalIgnoreCase);

    private static string CrearDetalleParciales(OrdenTrabajoDetalleResponse detalle)
    {
        StringBuilder sb = new();
        sb.AppendLine($"{detalle.Cabecera.NumeroOT} completada.");
        sb.AppendLine($"OC Cliente: {detalle.Cabecera.OrdenCompraCliente}");
        sb.AppendLine();
        sb.AppendLine("Parciales:");

        foreach (OrdenTrabajoProducto producto in detalle.Detalles
                     .Select(x => new { Producto = x, Clave = ProductoOrdenHelper.CrearClave(x.CodigoProducto, x.NombreProducto) })
                     .OrderBy(x => x.Clave.Cliente)
                     .ThenBy(x => x.Clave.NumeroNuloOrden)
                     .ThenBy(x => x.Clave.Numero)
                     .ThenBy(x => x.Clave.Variante)
                     .ThenBy(x => x.Clave.OrdenTalla)
                     .ThenBy(x => x.Clave.TallaNumero)
                     .ThenBy(x => x.Clave.CodigoOrden)
                     .ThenBy(x => x.Clave.NombreProducto)
                     .Select(x => x.Producto))
        {
            sb.AppendLine($"{producto.CodigoProducto} - {producto.NombreProducto}");
            sb.AppendLine($"Producido: {producto.CantidadProducida:N2} / Planificado: {producto.CantidadPlanificada:N2}");
        }

        return sb.ToString().Trim();
    }

    private async Task<decimal> PedirCantidadAsync(string titulo, decimal maximo, bool permitirMayor = false)
    {
        string? texto = await DisplayPromptAsync(
            titulo,
            $"Cantidad disponible: {maximo:N2}",
            "Aceptar",
            "Cancelar",
            "Cantidad",
            keyboard: Keyboard.Numeric,
            initialValue: maximo.ToString("0.##", CultureInfo.InvariantCulture));

        if (string.IsNullOrWhiteSpace(texto))
            return 0;

        texto = texto.Replace(',', '.');
        if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal cantidad) || cantidad <= 0)
        {
            await DisplayAlertAsync("Cantidad inválida", "Ingrese una cantidad mayor a cero.", "OK");
            return 0;
        }

        if (!permitirMayor && cantidad > maximo)
        {
            await DisplayAlertAsync("Cantidad inválida", "La cantidad no puede superar el pendiente del área.", "OK");
            return 0;
        }

        return cantidad;
    }

    private string ObtenerDestino(OrdenTrabajoArea area)
    {
        if (_detalleActual == null)
            return "siguiente área";

        return _detalleActual.Areas
            .Where(x => x.OrdenSecuencia > area.OrdenSecuencia)
            .OrderBy(x => x.OrdenSecuencia)
            .Select(x => x.NombreArea)
            .FirstOrDefault() ?? "siguiente área";
    }

    private int? ObtenerIdSiguienteArea(OrdenTrabajoArea area)
    {
        if (_detalleActual == null)
            return null;

        return _detalleActual.Areas
            .Where(x => x.OrdenSecuencia > area.OrdenSecuencia)
            .OrderBy(x => x.OrdenSecuencia)
            .Select(x => (int?)x.IdAreaProduccion)
            .FirstOrDefault();
    }

    private static string TextoVacio(string? valor) => string.IsNullOrWhiteSpace(valor) ? "Sin OC cliente" : valor.Trim();

    private static ContentPage CrearModalListado(string titulo, VerticalStackLayout contenido)
    {
        Button cerrar = new() { Text = "Cerrar", BackgroundColor = Color.FromArgb("#3F1D95"), TextColor = Colors.White };
        ContentPage page = new()
        {
            Title = titulo,
            BackgroundColor = Color.FromArgb("#F4F6F8"),
            Content = new ScrollView { Content = contenido }
        };
        cerrar.Clicked += async (_, _) => await page.Navigation.PopModalAsync();
        contenido.Add(cerrar);
        return page;
    }

    private static Border CrearKardexCard(OrdenTrabajoKardexItem item)
    {
        Grid grid = new()
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            RowSpacing = 5,
            ColumnSpacing = 10
        };
        grid.Add(new Label { Text = item.CodigoProducto, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#10324A") }, 0, 0);
        grid.Add(new Label { Text = item.Cantidad.ToString("N2"), FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#067647") }, 1, 0);
        Label nombre = new() { Text = item.NombreProducto, TextColor = Color.FromArgb("#344054"), LineBreakMode = LineBreakMode.WordWrap };
        grid.Add(nombre, 0, 1);
        grid.SetColumnSpan(nombre, 2);
        Label meta = new() { Text = $"{item.Almacen} | {item.FechaMovimiento:dd/MM/yyyy HH:mm} | {item.Usuario}", FontSize = 12, TextColor = Color.FromArgb("#667085"), LineBreakMode = LineBreakMode.WordWrap };
        grid.Add(meta, 0, 2);
        grid.SetColumnSpan(meta, 2);
        return new Border { Padding = 12, BackgroundColor = Colors.White, Stroke = Color.FromArgb("#D9E0E6"), StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = grid };
    }

    private static Picker CrearFiltroPicker(string textoInicial, IEnumerable<string> valores)
    {
        List<string> opciones = [textoInicial];
        opciones.AddRange(valores
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x));

        Picker picker = new()
        {
            Title = textoInicial,
            ItemsSource = opciones,
            SelectedIndex = 0,
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb("#344054")
        };
        return picker;
    }

    private static DateTime? ParseFechaFiltro(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string text = value.Trim();
        string[] formatos = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];
        if (DateTime.TryParseExact(text, formatos, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime fecha))
            return fecha.Date;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out fecha))
            return fecha.Date;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha.Date;

        return null;
    }

    private static Border CrearMovimientoCard(OrdenTrabajoMovimientoItem item, string areaFiltro = "")
    {
        VerticalStackLayout stack = new() { Spacing = 6 };
        string enfoqueArea = string.Empty;
        if (!string.IsNullOrWhiteSpace(areaFiltro))
        {
            if (item.Destino.Equals(areaFiltro, StringComparison.OrdinalIgnoreCase))
                enfoqueArea = $"Llego a {areaFiltro}";
            else if (item.Origen.Equals(areaFiltro, StringComparison.OrdinalIgnoreCase))
                enfoqueArea = $"Salio de {areaFiltro}";
        }

        if (!string.IsNullOrWhiteSpace(enfoqueArea))
            stack.Add(new Label { Text = enfoqueArea, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#047857") });
        stack.Add(new Label { Text = item.Accion, FontFamily = "OpenSansSemibold", TextColor = Color.FromArgb("#10324A") });
        stack.Add(new Label { Text = $"{item.CodigoProducto} - {item.NombreProducto}", TextColor = Color.FromArgb("#344054"), LineBreakMode = LineBreakMode.WordWrap });
        stack.Add(new Label { Text = $"{item.Origen} -> {item.Destino} | Cantidad {item.Cantidad:N2}", FontSize = 12, TextColor = Color.FromArgb("#667085"), LineBreakMode = LineBreakMode.WordWrap });
        stack.Add(new Label { Text = $"{item.FechaHora:dd/MM/yyyy HH:mm} | {item.Usuario}", FontSize = 12, TextColor = Color.FromArgb("#667085") });
        if (!string.IsNullOrWhiteSpace(item.Observacion))
            stack.Add(new Label { Text = item.Observacion, FontSize = 12, TextColor = Color.FromArgb("#475467"), LineBreakMode = LineBreakMode.WordWrap });
        return new Border { Padding = 12, BackgroundColor = Colors.White, Stroke = Color.FromArgb("#D9E0E6"), StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = stack };
    }

    private sealed record AreaFiltro(int IdAreaProduccion, string NombreArea, int OrdenSecuencia, bool EstaSeleccionada)
    {
        public Color BackgroundColor => EstaSeleccionada ? Color.FromArgb("#E0F2FE") : Colors.White;
        public Color StrokeColor => EstaSeleccionada ? Color.FromArgb("#0284C7") : Color.FromArgb("#D9E0E6");
        public Color TextColor => EstaSeleccionada ? Color.FromArgb("#075985") : Color.FromArgb("#10324A");
    }

    private sealed record OrdenTrabajoAreaItem(OrdenTrabajoArea Area, OrdenTrabajoProducto? Producto, bool EsPrimeraArea)
    {
        private bool ProductoPendiente => Producto?.Estado.Equals("PENDIENTE", StringComparison.OrdinalIgnoreCase) == true;
        public bool MostrarIniciar => EsPrimeraArea && ProductoPendiente;
        public bool MostrarTransferir => !EsPrimeraArea || (EsPrimeraArea && !ProductoPendiente && Area.Disponible);
        public bool MostrarMerma => !EsPrimeraArea && Area.ManejaMerma;
        public bool PuedeIniciar => MostrarIniciar && Area.CantidadPendiente > 0;
        public bool PuedeTransferir => MostrarTransferir && Area.Disponible;
        public bool PuedeMerma => MostrarMerma && Area.Disponible;
        public double OpacidadIniciar => PuedeIniciar ? 1 : 0.42;
        public double OpacidadTransferir => PuedeTransferir ? 1 : 0.42;
        public double OpacidadMerma => PuedeMerma ? 1 : 0.42;
    }
}
