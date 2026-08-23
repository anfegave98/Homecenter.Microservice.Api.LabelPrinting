using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Rules;

/// <summary>Regla 0 — datos minimos de la solicitud. Cubre CP-11.</summary>
public sealed class RequiredDataRuleTests
{
    private readonly RequiredDataRule _rule = new();

    [Fact]
    public void Aprueba_cuando_la_solicitud_trae_lpn_y_usuario()
    {
        // Arrange
        var context = PrintScenarioBuilder.Valid().Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeTrue();
        result.RuleCode.Should().Be(RuleCodes.RequiredData);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_cuando_el_lpn_viene_vacio_o_en_blanco(string lpn)
    {
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithLpn(lpn).Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.MissingRequiredData);
        result.Details.Should().Contain("lpn");
    }

    [Fact]
    public void Rechaza_cuando_no_hay_usuario_autenticado()
    {
        // El usuario se toma del JWT: si llega vacio, la solicitud no es atribuible
        // a nadie y auditarla no serviria de nada.
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithUser(string.Empty).Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.MissingRequiredData);
        result.Details.Should().Contain("usuario");
    }

    [Fact]
    public void Reporta_todos_los_datos_faltantes_en_una_sola_respuesta()
    {
        // Devolver los faltantes de uno en uno obligaria al operario a reintentar
        // tantas veces como campos le falten.
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithLpn(string.Empty).WithUser(string.Empty).Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Details.Should().BeEquivalentTo(new object[] { "lpn", "usuario" });
    }
}
