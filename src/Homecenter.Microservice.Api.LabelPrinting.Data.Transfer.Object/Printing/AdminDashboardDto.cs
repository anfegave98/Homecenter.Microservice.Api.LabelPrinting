namespace Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

/// <summary>
/// Indicadores operativos de impresion.
///
/// Son la primera pantalla util ante un incidente productivo: permiten distinguir
/// si un pico de fallas viene de reglas de negocio (inventario, documentos anulados)
/// o de una falla tecnica, sin abrir la base de datos.
/// </summary>
public sealed class AdminDashboardDto
{
    /// <summary>Total de solicitudes procesadas.</summary>
    public required int TotalRequests { get; init; }

    /// <summary>Solicitudes aprobadas.</summary>
    public required int Approved { get; init; }

    /// <summary>Solicitudes rechazadas.</summary>
    public required int Rejected { get; init; }

    /// <summary>Eventos marcados como reimpresion.</summary>
    public required int Reprints { get; init; }

    /// <summary>Conteo de rechazos agrupado por codigo, de mayor a menor.</summary>
    public required IReadOnlyDictionary<string, int> RejectionsByCode { get; init; }
}
