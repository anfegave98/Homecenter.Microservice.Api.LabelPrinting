using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.UseCases;

public interface IResolveLabelUseCase
{
    Task<ApiResponse<LabelDetailDto>> ExecuteAsync(
        string lpn,
        string? zoneCode,
        CancellationToken cancellationToken = default);
}
