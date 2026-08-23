using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;

/// <summary>
/// Todo lo que las reglas necesitan para decidir, ya resuelto.
///
/// El caso de uso carga los datos y arma este contexto; las reglas solo deciden.
/// Esa separacion es deliberada: permite probar cada regla de negocio sin base de
/// datos, sin HTTP y sin dobles de repositorio.
/// </summary>
public sealed class PrintRuleContext
{
    /// <summary>LPN o ETQ recibido en la solicitud.</summary>
    public required string RequestedKey { get; init; }

    /// <summary>Zona solicitada por el operario. Null si no la indico.</summary>
    public string? RequestedZoneCode { get; init; }

    /// <summary>Motivo de reimpresion informado en la solicitud.</summary>
    public string? ReprintReason { get; init; }

    /// <summary>Usuario autenticado que ejecuta la solicitud.</summary>
    public required string UserName { get; init; }

    /// <summary>Roles del usuario. Determinan si puede reimprimir.</summary>
    public required IReadOnlyCollection<string> UserRoles { get; init; }

    /// <summary>Etiqueta resuelta. Null cuando el LPN no existe.</summary>
    public Label? Label { get; init; }

    /// <summary>Documento origen de la etiqueta.</summary>
    public Document? Document { get; init; }

    /// <summary>Zona efectiva contra la que se valida. Null si la zona pedida no existe.</summary>
    public Zone? Zone { get; init; }

    /// <summary>Productos asociados a la etiqueta con su cantidad solicitada.</summary>
    public IReadOnlyCollection<DocumentProduct> Products { get; init; } = Array.Empty<DocumentProduct>();

    /// <summary>Disponibilidad en la zona, indexada por identificador de producto.</summary>
    public IReadOnlyDictionary<int, InventoryAvailability> Availability { get; init; } =
        new Dictionary<int, InventoryAvailability>();

    /// <summary>True si ya existe una impresion aprobada previa para esta ETQ/LPN.</summary>
    public bool HasPreviousPrint { get; init; }
}
