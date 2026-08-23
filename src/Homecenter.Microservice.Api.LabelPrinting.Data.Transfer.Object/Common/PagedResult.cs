namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

/// <summary>
/// Resultado paginado. Toda consulta de listado devuelve esta forma para evitar
/// respuestas no acotadas.
/// </summary>
/// <typeparam name="T">Tipo de los elementos de la pagina.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Elementos de la pagina actual.</summary>
    public required IReadOnlyCollection<T> Items { get; init; }

    /// <summary>Total de registros que cumplen el filtro.</summary>
    public required int Total { get; init; }

    /// <summary>Pagina devuelta, base 1.</summary>
    public required int Page { get; init; }

    /// <summary>Cantidad de registros por pagina.</summary>
    public required int PageSize { get; init; }

    /// <summary>Numero total de paginas disponibles.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
