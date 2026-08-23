using System.ComponentModel.DataAnnotations;

namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Filtros de consulta del historial de impresiones.
/// </summary>
public sealed class PrintHistoryFilterDto
{
    /// <summary>Tamano maximo de pagina admitido, para evitar consultas no acotadas.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Filtra por identificador de unidad logistica.</summary>
    [MaxLength(50)]
    public string? Lpn { get; set; }

    /// <summary>Filtra por codigo de zona.</summary>
    [MaxLength(50)]
    public string? ZoneCode { get; set; }

    /// <summary>
    /// Filtra por usuario solicitante. Se ignora para el rol Operario:
    /// a ese rol el backend le fuerza su propio usuario.
    /// </summary>
    [MaxLength(100)]
    public string? UserName { get; set; }

    /// <summary>APPROVED o REJECTED.</summary>
    [MaxLength(20)]
    public string? Result { get; set; }

    /// <summary>PRINT o REPRINT.</summary>
    [MaxLength(20)]
    public string? EventType { get; set; }

    /// <summary>Limite inferior del rango de fechas, inclusive.</summary>
    public DateTimeOffset? DateFrom { get; set; }

    /// <summary>Limite superior del rango de fechas, inclusive.</summary>
    public DateTimeOffset? DateTo { get; set; }

    /// <summary>Pagina solicitada, base 1.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "La pagina debe ser mayor o igual a 1.")]
    public int Page { get; set; } = 1;

    /// <summary>Registros por pagina.</summary>
    [Range(1, MaxPageSize, ErrorMessage = "El tamano de pagina debe estar entre 1 y 100.")]
    public int PageSize { get; set; } = 20;
}
