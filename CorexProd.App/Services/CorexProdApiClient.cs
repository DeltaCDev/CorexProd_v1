using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorexProd.App.Models;

namespace CorexProd.App.Services;

public sealed class CorexProdApiClient
{
    private const string ApiBaseUrlKey = "ApiBaseUrl";
    private const string DefaultApiBaseUrl = "http://192.168.68.112:5056";

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CorexProdApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string BaseUrl
    {
        get
        {
            string url = Preferences.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
            string normalizedUrl = url.TrimEnd('/');
            if (normalizedUrl.Equals("http://192.168.68.112:5000", StringComparison.OrdinalIgnoreCase)
                || normalizedUrl.Equals("http://192.168.68.112:5055", StringComparison.OrdinalIgnoreCase))
            {
                Preferences.Set(ApiBaseUrlKey, DefaultApiBaseUrl);
                return DefaultApiBaseUrl;
            }

            return url;
        }
        set => Preferences.Set(ApiBaseUrlKey, NormalizeBaseUrl(value));
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<HealthResponse>("api/health", cancellationToken);
    }

    public async Task<LoginResponse> LoginAsync(string usuario, string clave, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendAsync(
            client => client.PostAsJsonAsync(BuildUrl("api/auth/login"), new LoginRequest(usuario, clave), _jsonOptions, cancellationToken),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Usuario o clave incorrectos.");
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<LoginResponse>(response, cancellationToken);
    }

    public async Task<ApiListResponse<ProductoStock>> GetProductosAsync(string buscar, string etiqueta = "", CancellationToken cancellationToken = default)
    {
        List<string> parametros = [];

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            parametros.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(etiqueta))
        {
            parametros.Add($"etiqueta={Uri.EscapeDataString(etiqueta.Trim())}");
        }

        string query = parametros.Count == 0 ? string.Empty : $"?{string.Join("&", parametros)}";
        return await GetAsync<ApiListResponse<ProductoStock>>($"api/stock/productos{query}", cancellationToken);
    }

    public async Task<ApiListResponse<InsumoStock>> GetInsumosAsync(string buscar, CancellationToken cancellationToken = default)
    {
        string query = string.IsNullOrWhiteSpace(buscar) ? string.Empty : $"?buscar={Uri.EscapeDataString(buscar.Trim())}";
        return await GetAsync<ApiListResponse<InsumoStock>>($"api/stock/insumos{query}", cancellationToken);
    }

    public async Task<ApiListResponse<ProformaResumen>> GetProformasAsync(string buscar, CancellationToken cancellationToken = default)
    {
        string query = string.IsNullOrWhiteSpace(buscar) ? string.Empty : $"?buscar={Uri.EscapeDataString(buscar.Trim())}";
        return await GetAsync<ApiListResponse<ProformaResumen>>($"api/proformas{query}", cancellationToken);
    }

    public async Task<ProformaDetalleResponse> GetProformaDetalleAsync(int idProforma, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ProformaDetalleResponse>($"api/proformas/{idProforma}", cancellationToken);
    }

    public async Task<ApiListResponse<OciResumen>> GetOciAsync(string buscar, CancellationToken cancellationToken = default)
    {
        string query = string.IsNullOrWhiteSpace(buscar) ? string.Empty : $"?buscar={Uri.EscapeDataString(buscar.Trim())}";
        return await GetAsync<ApiListResponse<OciResumen>>($"api/oci{query}", cancellationToken);
    }

    public async Task<OciDetalleResponse> GetOciDetalleAsync(int idOrdenCompraInterna, CancellationToken cancellationToken = default)
    {
        return await GetAsync<OciDetalleResponse>($"api/oci/{idOrdenCompraInterna}", cancellationToken);
    }

    public async Task<StockManualPrepararResponse> GetStockManualPrepararAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<StockManualPrepararResponse>("api/stock/manual/preparar", cancellationToken);
    }

    public async Task<ApiListResponse<ProductoStockBusquedaApi>> BuscarProductosStockManualAsync(int idAlmacen, string buscar, CancellationToken cancellationToken = default)
    {
        string query = $"?idAlmacen={idAlmacen}&buscar={Uri.EscapeDataString((buscar ?? string.Empty).Trim())}";
        return await GetAsync<ApiListResponse<ProductoStockBusquedaApi>>($"api/stock/manual/productos{query}", cancellationToken);
    }

    public async Task<IngresoManualStockResponse> IngresarStockManualAsync(IngresoManualStockRequest request, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendAsync(
            client => client.PostAsJsonAsync(BuildUrl("api/stock/manual/ingresar"), request, _jsonOptions, cancellationToken),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<IngresoManualStockResponse>(response, cancellationToken);
    }

    public async Task<ApiListResponse<OrdenTrabajoResumen>> GetOrdenesTrabajoAsync(string buscar, CancellationToken cancellationToken = default)
    {
        string query = string.IsNullOrWhiteSpace(buscar) ? string.Empty : $"?buscar={Uri.EscapeDataString(buscar.Trim())}";
        return await GetAsync<ApiListResponse<OrdenTrabajoResumen>>($"api/ordenes-trabajo{query}", cancellationToken);
    }

    public async Task<OrdenTrabajoDetalleResponse> GetOrdenTrabajoDetalleAsync(int idOrdenTrabajo, CancellationToken cancellationToken = default)
    {
        return await GetAsync<OrdenTrabajoDetalleResponse>($"api/ordenes-trabajo/{idOrdenTrabajo}", cancellationToken);
    }

    public async Task<OperacionOrdenTrabajoResponse> LanzarOrdenTrabajoAsync(int idOrdenTrabajo, OrdenTrabajoLanzarRequest request, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendAsync(
            client => client.PostAsJsonAsync(BuildUrl($"api/ordenes-trabajo/{idOrdenTrabajo}/lanzar"), request, _jsonOptions, cancellationToken),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<OperacionOrdenTrabajoResponse>(response, cancellationToken);
    }

    public async Task<OperacionOrdenTrabajoResponse> TransferirOrdenTrabajoAsync(int idOrdenTrabajo, OrdenTrabajoTransferirRequest request, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendAsync(
            client => client.PostAsJsonAsync(BuildUrl($"api/ordenes-trabajo/{idOrdenTrabajo}/transferir"), request, _jsonOptions, cancellationToken),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<OperacionOrdenTrabajoResponse>(response, cancellationToken);
    }

    public async Task<OperacionOrdenTrabajoResponse> RegistrarMermaOrdenTrabajoAsync(int idOrdenTrabajo, OrdenTrabajoMermaRequest request, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendAsync(
            client => client.PostAsJsonAsync(BuildUrl($"api/ordenes-trabajo/{idOrdenTrabajo}/merma"), request, _jsonOptions, cancellationToken),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<OperacionOrdenTrabajoResponse>(response, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string route, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendAsync(
            client => client.GetAsync(BuildUrl(route), cancellationToken),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<T>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpClient, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            return await send(_httpClient);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("La API no respondió a tiempo.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"No se pudo conectar con la API local. Revise la URL del servidor. Detalle: {ex.Message}");
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        ApiProblem? problem = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                problem = JsonSerializer.Deserialize<ApiProblem>(body, _jsonOptions);
            }
            catch (JsonException)
            {
                // Keep the generic message when the API returns non-JSON content.
            }
        }

        string message = problem?.Mensaje
            ?? problem?.Detail
            ?? problem?.Title
            ?? $"La API devolvió HTTP {(int)response.StatusCode}.";

        throw new InvalidOperationException(message);
    }

    private async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        T? value = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        return value ?? throw new InvalidOperationException("La API devolvió una respuesta vacía.");
    }

    private static string NormalizeBaseUrl(string? value)
    {
        string url = (value ?? string.Empty).Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(url) ? DefaultApiBaseUrl : url;
    }

    private string BuildUrl(string route)
    {
        return $"{NormalizeBaseUrl(BaseUrl)}/{route.TrimStart('/')}";
    }
}
