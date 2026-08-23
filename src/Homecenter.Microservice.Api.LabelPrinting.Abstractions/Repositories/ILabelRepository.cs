using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

public interface ILabelRepository
{
    /// <summary>
    /// Resuelve una etiqueta por LPN o por ETQ, con su documento y productos cargados.
    /// La entrada funcional del submodulo es la etiqueta o la unidad logistica, nunca el SKU.
    /// </summary>
    Task<Label?> GetByLpnOrEtqAsync(string key, CancellationToken cancellationToken = default);
}
