using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

public interface IProcessPrintRequestUseCase
{
    Task<ApiResponse<PrintResultDto>> ExecuteAsync(
        PrintRequestCreateDto request,
        CancellationToken cancellationToken = default);
}
