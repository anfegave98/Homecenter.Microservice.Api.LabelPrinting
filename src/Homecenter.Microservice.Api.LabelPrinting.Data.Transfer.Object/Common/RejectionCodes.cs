namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;

/// <summary>
/// Codigos de rechazo del submodulo. Son parte del contrato publico: el frontend
/// decide con ellos que mensaje mostrar, asi que cambiarlos rompe al consumidor.
/// </summary>
public static class RejectionCodes
{
    /// <summary>Faltan datos obligatorios en la solicitud (Regla 0).</summary>
    public const string MissingRequiredData = "MISSING_REQUIRED_DATA";

    /// <summary>La ETQ/LPN no existe en los datos mock (Regla 1).</summary>
    public const string LpnNotFound = "LPN_NOT_FOUND";

    /// <summary>La zona solicitada no existe.</summary>
    public const string ZoneNotFound = "ZONE_NOT_FOUND";

    /// <summary>El documento origen esta ANULADA o DEVUELTA (Regla 2).</summary>
    public const string InvalidDocumentStatus = "INVALID_DOCUMENT_STATUS";

    /// <summary>Uno o mas productos no tienen disponibilidad suficiente en la zona (Regla 3).</summary>
    public const string InsufficientInventory = "INSUFFICIENT_INVENTORY";

    /// <summary>Uno o mas productos no estan abastecidos en la zona (Regla 3).</summary>
    public const string NotStocked = "NOT_STOCKED";

    /// <summary>La reimpresion exige motivo (Regla 4).</summary>
    public const string ReprintReasonRequired = "REPRINT_REASON_REQUIRED";

    /// <summary>El rol del usuario no autoriza reimpresiones (Regla 4).</summary>
    public const string ReprintNotAuthorized = "REPRINT_NOT_AUTHORIZED";
}

/// <summary>
/// Identificadores de las reglas evaluadas. Se persisten en la auditoria para poder
/// responder que se valido y en que orden, sin reconstruir el caso a mano.
/// </summary>
public static class RuleCodes
{
    public const string RequiredData = "R0_REQUIRED_DATA";
    public const string LabelExists = "R1_LABEL_EXISTS";
    public const string DocumentStatus = "R2_DOCUMENT_STATUS";
    public const string ZoneAvailability = "R3_ZONE_AVAILABILITY";
    public const string ReprintPolicy = "R4_REPRINT_POLICY";
}
