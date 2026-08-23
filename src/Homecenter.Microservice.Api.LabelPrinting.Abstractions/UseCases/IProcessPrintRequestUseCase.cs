using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

/// <summary>Procesamiento de solicitudes de impresion sobre etiquetas pre-generadas.</summary>
public interface IProcessPrintRequestUseCase
{
    /// <summary>Evalua las reglas, imprime si procede y registra la auditoria.</summary>
    /// <param name="request">Solicitud con LPN, zona y motivo de reimpresion si aplica.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Resultado aprobado o rechazado, siempre auditado.</returns>
    Task<ApiResponse<PrintResultDto>> ExecuteAsync(
        PrintRequestCreateDto request,
        CancellationToken cancellationToken = default);
}
