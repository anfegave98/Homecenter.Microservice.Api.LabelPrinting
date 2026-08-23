using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Rules;

/// <summary>Regla 1 — existencia de la ETQ/LPN y de la zona. Cubre CP-01.</summary>
public sealed class LabelExistsRuleTests
{
    private readonly LabelExistsRule _rule = new();

    [Fact]
    public void Aprueba_cuando_la_etiqueta_y_la_zona_existen()
    {
        // Arrange
        var context = PrintScenarioBuilder.Valid().Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Rechaza_con_LPN_NOT_FOUND_cuando_la_etiqueta_no_existe()
    {
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithLpn("LPN-NO-EXISTE").WithoutLabel().Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.LpnNotFound);
        result.Message.Should().Contain("LPN-NO-EXISTE");
    }

    [Fact]
    public void Rechaza_con_ZONE_NOT_FOUND_cuando_la_zona_solicitada_no_existe()
    {
        // Sin zona resuelta no hay contra que validar disponibilidad: distinguir este
        // rechazo del de inventario le dice al operario que corrija la zona, no el pedido.
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithoutZone().Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.ZoneNotFound);
    }
}
