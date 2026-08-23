namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Zona logistica de la tienda. Es la dimension contra la que se valida
/// la disponibilidad de inventario al momento de imprimir.
/// </summary>
public class Zone : EntityBase
{
    /// <summary>Codigo operativo de la zona, por ejemplo ZONA-PICKING-A.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre legible de la zona.</summary>
    public string Name { get; set; } = string.Empty;
}
