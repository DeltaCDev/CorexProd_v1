using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CorexProd.App.Models;
using CorexProd.App.Services;

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
            int? areaSeleccionada = (AreasTabsView.SelectedItem as AreaFiltro)?.IdAreaProduccion;
            _detalleActual = await _apiClient.GetOrdenTrabajoDetalleAsync(idOrdenTrabajo);
            TituloDetalleLabel.Text = _detalleActual.Cabecera.NumeroOT;
            ResumenDetalleLabel.Text = $"{_detalleActual.Cabecera.NombreCliente}\nOCI: {_detalleActual.Cabecera.NumeroOci} | OC Cliente: {_detalleActual.Cabecera.OrdenCompraCliente}\nEstado: {_detalleActual.Cabecera.Estado}";
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

        AreasTabsView.SelectedItem = _areasFiltro.FirstOrDefault(x => x.IdAreaProduccion == seleccion?.IdAreaProduccion);
        MostrarAreaSeleccionada();
    }

    private void OnAreaSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is AreaFiltro selected)
        {
            for (int i = 0; i < _areasFiltro.Count; i++)
                _areasFiltro[i] = _areasFiltro[i] with { EstaSeleccionada = _areasFiltro[i].IdAreaProduccion == selected.IdAreaProduccion };
        }

        MostrarAreaSeleccionada();
    }

    private void MostrarAreaSeleccionada()
    {
        _areasVisibles.Clear();
        if (_detalleActual == null || AreasTabsView.SelectedItem is not AreaFiltro area)
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

        await EjecutarOperacionAsync(async idUsuario =>
        {
            OrdenTrabajoLanzarRequest request = new(
                idUsuario,
                idUsuario,
                [new(area.IdDetalleOT, cantidad, cantidad != producto.CantidadPlanificada ? "AJUSTE DESDE ANDROID" : string.Empty, "Inicio desde Android")]);

            return await _apiClient.LanzarOrdenTrabajoAsync(_detalleActual.Cabecera.IdOrdenTrabajo, request);
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
            OrdenTrabajoMermaRequest request = new(area.IdDetalleArea, cantidad, "MERMA EN OPERACION", observacion, idUsuario, idUsuario);
            return await _apiClient.RegistrarMermaOrdenTrabajoAsync(_detalleActual.Cabecera.IdOrdenTrabajo, request);
        });
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

    private sealed record AreaFiltro(int IdAreaProduccion, string NombreArea, int OrdenSecuencia, bool EstaSeleccionada)
    {
        public Color BackgroundColor => EstaSeleccionada ? Color.FromArgb("#E0F2FE") : Colors.White;
        public Color StrokeColor => EstaSeleccionada ? Color.FromArgb("#0284C7") : Color.FromArgb("#D9E0E6");
        public Color TextColor => EstaSeleccionada ? Color.FromArgb("#075985") : Color.FromArgb("#10324A");
    }

    private sealed record OrdenTrabajoAreaItem(OrdenTrabajoArea Area, OrdenTrabajoProducto? Producto, bool EsPrimeraArea)
    {
        public bool MostrarIniciar => EsPrimeraArea;
        public bool MostrarTransferir => !EsPrimeraArea;
        public bool MostrarMerma => !EsPrimeraArea && Area.ManejaMerma;
        public bool PuedeIniciar => MostrarIniciar && Producto?.Estado.Equals("PENDIENTE", StringComparison.OrdinalIgnoreCase) == true && Area.CantidadPendiente > 0;
        public bool PuedeTransferir => MostrarTransferir && Area.Disponible;
        public bool PuedeMerma => MostrarMerma && Area.Disponible;
        public double OpacidadIniciar => PuedeIniciar ? 1 : 0.42;
        public double OpacidadTransferir => PuedeTransferir ? 1 : 0.42;
        public double OpacidadMerma => PuedeMerma ? 1 : 0.42;
    }
}
