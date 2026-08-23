using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Rules;

/// <summary>Regla 4 — politica de reimpresion. Cubre CP-08, CP-09 y CP-10.</summary>
public sealed class ReprintPolicyRuleTests
{
    private readonly ReprintPolicyRule _rule = new();

    [Fact]
    public void No_aplica_cuando_es_la_primera_impresion_de_la_etiqueta()
    {
        // Un operario sin permiso de reimpresion debe poder imprimir por primera vez.
        // Arrange
        var context = PrintScenarioBuilder.Valid()
            .WithUser("operario.tienda", RoleName.Operario)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Theory]
    [InlineData(RoleName.Supervisor)]
    [InlineData(RoleName.Admin)]
    public void Autoriza_la_reimpresion_con_rol_habilitado_y_motivo(string role)
    {
        // CP-08.
        // Arrange
        var context = PrintScenarioBuilder.Valid()
            .AlreadyPrinted()
            .WithUser("supervisor.tienda", role)
            .WithReprintReason("Etiqueta danada en piso")
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeTrue();
        result.Message.Should().Contain("Etiqueta danada en piso");
    }

    [Fact]
    public void Rechaza_con_REPRINT_NOT_AUTHORIZED_cuando_el_rol_no_habilita()
    {
        // CP-10. La politica vive en la regla de negocio, no en el controlador: asi se
        // aplica igual sin importar por que camino entre la solicitud.
        // Arrange
        var context = PrintScenarioBuilder.Valid()
            .AlreadyPrinted()
            .WithUser("operario.tienda", RoleName.Operario)
            .WithReprintReason("Etiqueta danada en piso")
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.ReprintNotAuthorized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_con_REPRINT_REASON_REQUIRED_cuando_falta_el_motivo(string? reason)
    {
        // CP-09. Un motivo en blanco no es un motivo: sin el, la reimpresion queda
        // registrada pero no explicada, que es justo lo que la auditoria necesita.
        // Arrange
        var context = PrintScenarioBuilder.Valid()
            .AlreadyPrinted()
            .WithUser("supervisor.tienda", RoleName.Supervisor)
            .WithReprintReason(reason)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.ReprintReasonRequired);
    }

    [Fact]
    public void El_rol_se_evalua_antes_que_el_motivo()
    {
        // Pedirle el motivo a alguien que de todas formas no puede reimprimir lo lleva
        // a reintentar para chocar con un segundo rechazo.
        // Arrange
        var context = PrintScenarioBuilder.Valid()
            .AlreadyPrinted()
            .WithUser("operario.tienda", RoleName.Operario)
            .WithReprintReason(null)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.RejectionCode.Should().Be(RejectionCodes.ReprintNotAuthorized);
    }
}
