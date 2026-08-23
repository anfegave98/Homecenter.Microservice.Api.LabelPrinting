using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

/// <summary>Consulta de una ETQ/LPN con su contexto de negocio.</summary>
public interface IResolveLabelUseCase
{
    /// <summary>Resuelve la etiqueta, su documento y la disponibilidad en la zona.</summary>
    /// <param name="lpn">Identificador de unidad logistica o de etiqueta.</param>
    /// <param name="zoneCode">Zona a consultar. Si se omite, se usa la del documento.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Detalle de la etiqueta, o el error si no existe.</returns>
    Task<ApiResponse<LabelDetailDto>> ExecuteAsync(
        string lpn,
        string? zoneCode,
        CancellationToken cancellationToken = default);
}
