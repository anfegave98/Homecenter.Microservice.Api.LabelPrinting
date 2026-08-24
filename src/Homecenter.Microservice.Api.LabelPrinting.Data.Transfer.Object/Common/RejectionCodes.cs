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

    /// <summary>
    /// La reimpresion quedo esperando la autorizacion de un Supervisor o Admin (Regla 4).
    ///
    /// No es un rechazo: la solicitud sigue viva. Se expone con success=false porque no
    /// se entrego ZPL, y el frontend lo distingue de un rechazo por este codigo.
    /// </summary>
    public const string ReprintPendingApproval = "REPRINT_PENDING_APPROVAL";

    /// <summary>Un Supervisor o Admin nego la reimpresion pendiente.</summary>
    public const string ReprintRejectedByApprover = "REPRINT_REJECTED_BY_APPROVER";

    /// <summary>La solicitud pendiente no existe o ya fue resuelta por otro autorizador.</summary>
    public const string PendingRequestNotFound = "PENDING_REQUEST_NOT_FOUND";

    /// <summary>Negar una reimpresion pendiente exige dejar el motivo por escrito.</summary>
    public const string ApprovalNoteRequired = "APPROVAL_NOTE_REQUIRED";

    /// <summary>La solicitud no existe, no fue aprobada o pertenece a otro usuario.</summary>
    public const string LabelNotAvailable = "LABEL_NOT_AVAILABLE";

    /// <summary>La etiqueta de esa solicitud ya se descargo una vez.</summary>
    public const string LabelAlreadyDownloaded = "LABEL_ALREADY_DOWNLOADED";
}

/// <summary>
/// Identificadores de las reglas evaluadas. Se persisten en la auditoria para poder
/// responder que se valido y en que orden, sin reconstruir el caso a mano.
/// </summary>
public static class RuleCodes
{
    /// <summary>Regla 0: validacion de datos obligatorios.</summary>
    public const string RequiredData = "R0_REQUIRED_DATA";
    /// <summary>Regla 1: existencia de la ETQ/LPN y de la zona.</summary>
    public const string LabelExists = "R1_LABEL_EXISTS";
    /// <summary>Regla 2: estado del documento origen.</summary>
    public const string DocumentStatus = "R2_DOCUMENT_STATUS";
    /// <summary>Regla 3: disponibilidad y abastecimiento por zona.</summary>
    public const string ZoneAvailability = "R3_ZONE_AVAILABILITY";
    /// <summary>Regla 4: politica de reimpresion.</summary>
    public const string ReprintPolicy = "R4_REPRINT_POLICY";
    /// <summary>Decision de un autorizador sobre una reimpresion pendiente.</summary>
    public const string ReprintApproval = "R4A_REPRINT_APPROVAL";
}
