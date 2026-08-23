using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Rules;

/// <summary>Regla 2 — estado del documento origen. Cubre CP-02 y CP-03.</summary>
public sealed class DocumentStatusRuleTests
{
    private readonly DocumentStatusRule _rule = new();

    [Theory]
    [InlineData(DocumentStatus.Anulada)]
    [InlineData(DocumentStatus.Devuelta)]
    public void Rechaza_los_estados_bloqueados_por_el_enunciado(DocumentStatus status)
    {
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithDocumentStatus(status).Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.InvalidDocumentStatus);
        result.Message.Should().Contain(status.ToString().ToUpperInvariant());
    }

    [Theory]
    [InlineData(DocumentStatus.Creada)]
    [InlineData(DocumentStatus.Liberada)]
    public void Permite_imprimir_en_los_estados_no_bloqueados(DocumentStatus status)
    {
        // CREADA tambien imprime: el enunciado solo bloquea ANULADA y DEVUELTA.
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithDocumentStatus(status).Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void No_opina_cuando_no_hay_documento_resuelto()
    {
        // El rechazo por LPN inexistente le corresponde a la Regla 1. Que esta regla
        // tambien fallara produciria dos motivos distintos para la misma causa.
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithoutLabel().Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeTrue();
    }
}
