using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

public interface IPrintRequestRepository
{
    /// <summary>Persiste la solicitud junto con su traza de reglas.</summary>
    Task AddAsync(PrintRequest request, CancellationToken cancellationToken = default);

    /// <summary>Indica si existe una impresion aprobada previa para la ETQ/LPN (Regla 4).</summary>
    Task<bool> HasApprovedPrintAsync(string lpnId, CancellationToken cancellationToken = default);
}
