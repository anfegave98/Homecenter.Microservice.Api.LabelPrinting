using Homecenter.Microservice.Api.LabelPrinting.Entities;

namespace Homecenter.Microservice.Api.LabelPrinting.Abstractions.Repositories;

/// <summary>Acceso a las etiquetas pre-generadas.</summary>
public interface ILabelRepository
{
    /// <summary>
    /// Resuelve una etiqueta por LPN o por ETQ, con su documento y productos cargados.
    /// La entrada funcional del submodulo es la etiqueta o la unidad logistica, nunca el SKU.
    /// </summary>
    /// <param name="key">Identificador de unidad logistica o de etiqueta.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La etiqueta con su contexto, o null si no existe.</returns>
    Task<Label?> GetByLpnOrEtqAsync(string key, CancellationToken cancellationToken = default);
}
