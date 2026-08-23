namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

/// <summary>
/// Resultado paginado. Toda consulta de listado devuelve esta forma para evitar respuestas no acotadas.
/// </summary>
public sealed class PagedResult<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }

    public required int Total { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
