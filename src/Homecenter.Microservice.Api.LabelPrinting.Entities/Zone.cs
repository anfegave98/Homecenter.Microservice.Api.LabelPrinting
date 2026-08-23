namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Zona logistica de la tienda. Es la dimension contra la que se valida
/// la disponibilidad de inventario al momento de imprimir.
/// </summary>
public class Zone : EntityBase
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
