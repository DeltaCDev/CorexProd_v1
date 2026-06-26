using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorexProd.App.Models;

namespace CorexProd.App.Services;

public sealed class CorexProdApiClient
{
    private const string ApiBaseUrlKey = "ApiBaseUrl";
    private const string DefaultApiBaseUrl = "http://192.168.68.112:5000";

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
        get => Preferences.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Set(ApiBaseUrlKey, NormalizeBaseUrl(value));
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<HealthResponse>("api/health", cancellationToken);
    }

    public async Task<LoginResponse> LoginAsync(string usuario, string clave, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendAsync(
            client => client.PostAsJsonAsync("api/auth/login", new LoginRequest(usuario, clave), _jsonOptions, cancellationToken),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Usuario o clave incorrectos.");
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<LoginResponse>(response, cancellationToken);
    }

    public async Task<ApiListResponse<ProductoStock>> GetProductosAsync(string buscar, CancellationToken cancellationToken = default)
    {
        string query = string.IsNullOrWhiteSpace(buscar) ? string.Empty : $"?buscar={Uri.EscapeDataString(buscar.Trim())}";
        return await GetAsync<ApiListResponse<ProductoStock>>($"api/stock/productos{query}", cancellationToken);
    }

    public async Task<ApiListResponse<InsumoStock>> GetInsumosAsync(string buscar, CancellationToken cancellationToken = default)
    {
        string query = string.IsNullOrWhiteSpace(buscar) ? string.Empty : $"?buscar={Uri.EscapeDataString(buscar.Trim())}";
        return await GetAsync<ApiListResponse<InsumoStock>>($"api/stock/insumos{query}", cancellationToken);
    }

    private async Task<T> GetAsync<T>(string route, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendAsync(
            client => client.GetAsync(route, cancellationToken),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<T>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpClient, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        _httpClient.BaseAddress = new Uri($"{NormalizeBaseUrl(BaseUrl)}/");

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
}
