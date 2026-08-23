namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

/// <summary>
/// Envelope uniforme de respuesta para toda la API.
/// Un rechazo de negocio viaja como HTTP 200 con Success = false: no es un error tecnico,
/// es una decision del dominio que el consumidor debe poder leer sin interpretar codigos HTTP.
/// </summary>
/// <typeparam name="T">Tipo del contenido transportado.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>Indica si la operacion resulto satisfactoria.</summary>
    public bool Success { get; init; }

    /// <summary>Contenido de la respuesta. Puede venir informado incluso en un rechazo.</summary>
    public T? Data { get; init; }

    /// <summary>Detalle del error o rechazo. Null cuando Success es true.</summary>
    public ApiError? Error { get; init; }

    /// <summary>Metadatos adicionales, como la paginacion de un listado.</summary>
    public object? Meta { get; init; }

    /// <summary>Construye una respuesta satisfactoria.</summary>
    /// <param name="data">Contenido a transportar.</param>
    /// <param name="meta">Metadatos opcionales.</param>
    /// <returns>Respuesta con Success en true.</returns>
    public static ApiResponse<T> Ok(T data, object? meta = null) =>
        new() { Success = true, Data = data, Meta = meta };

    /// <summary>Construye una respuesta fallida a partir de un error ya armado.</summary>
    /// <param name="error">Error a reportar.</param>
    /// <param name="data">Contenido parcial opcional.</param>
    /// <returns>Respuesta con Success en false.</returns>
    public static ApiResponse<T> Fail(ApiError error, T? data = default) =>
        new() { Success = false, Data = data, Error = error };

    /// <summary>Construye una respuesta fallida a partir de un codigo y un mensaje.</summary>
    /// <param name="code">Codigo estable del error.</param>
    /// <param name="message">Mensaje legible.</param>
    /// <param name="details">Detalle granular opcional.</param>
    /// <returns>Respuesta con Success en false.</returns>
    public static ApiResponse<T> Fail(string code, string message, IReadOnlyCollection<object>? details = null) =>
        Fail(new ApiError { Code = code, Message = message, Details = details });
}
