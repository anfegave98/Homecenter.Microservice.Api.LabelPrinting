namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

/// <summary>
/// Envelope uniforme de respuesta para toda la API.
/// Un rechazo de negocio viaja como HTTP 200 con Success = false: no es un error tecnico,
/// es una decision del dominio que el consumidor debe poder leer sin interpretar codigos HTTP.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public ApiError? Error { get; init; }

    public object? Meta { get; init; }

    public static ApiResponse<T> Ok(T data, object? meta = null) =>
        new() { Success = true, Data = data, Meta = meta };

    public static ApiResponse<T> Fail(ApiError error, T? data = default) =>
        new() { Success = false, Data = data, Error = error };

    public static ApiResponse<T> Fail(string code, string message, IReadOnlyCollection<object>? details = null) =>
        Fail(new ApiError { Code = code, Message = message, Details = details });
}
