using System.Text.Json.Serialization;

namespace CorexProd.App.Models;

public sealed record LoginRequest(string Usuario, string Clave);

public sealed record LoginResponse(
    LoginUser Usuario,
    IReadOnlyList<string> Menus,
    DateTime FechaHora,
    string Mensaje);

public sealed record LoginUser(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    int IdRol,
    string NombreRol);

public sealed record HealthResponse(
    string Estado,
    string BaseDatos,
    string Servidor,
    DateTime FechaHora);

public sealed record ApiListResponse<T>(
    int Total,
    IReadOnlyList<T> Items);

public sealed record ProductoStock(
    int IdProducto,
    string Codigo,
    string CodigoModelo,
    string Producto,
    string Categoria,
    decimal StockActual);

public sealed record InsumoStock(
    int IdInsumo,
    string Codigo,
    string Insumo,
    string Categoria,
    string Unidad,
    decimal StockActual);

public sealed record ApiProblem(
    string? Mensaje,
    string? Title,
    string? Detail,
    [property: JsonPropertyName("status")] int? Status);
