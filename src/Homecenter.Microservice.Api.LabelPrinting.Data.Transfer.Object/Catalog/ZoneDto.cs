namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Catalog;

/// <summary>Zona logistica disponible para operar.</summary>
public sealed class ZoneDto
{
    /// <summary>Identificador de la zona.</summary>
    public required int Id { get; init; }

    /// <summary>Codigo operativo, por ejemplo ZONA-PICKING-A.</summary>
    public required string Code { get; init; }

    /// <summary>Nombre legible de la zona.</summary>
    public required string Name { get; init; }
}
