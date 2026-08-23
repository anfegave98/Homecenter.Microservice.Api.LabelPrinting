using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Rules;

/// <summary>
/// Orquestacion del motor: orden de evaluacion, corte temprano y traza de auditoria.
/// </summary>
public sealed class PrintRuleEngineTests
{
    private static PrintRuleEngine BuildEngine() => new(new IPrintRule[]
    {
        // Se registran desordenadas a proposito: el motor debe ordenarlas por Order.
        new ReprintPolicyRule(),
        new LabelExistsRule(),
        new RequiredDataRule(),
        new ZoneAvailabilityRule(),
        new DocumentStatusRule()
    });

    [Fact]
    public void Aprueba_cuando_ninguna_regla_se_incumple()
    {
        // Arrange
        var context = PrintScenarioBuilder.Valid().Build();

        // Act
        var evaluation = BuildEngine().Evaluate(context);

        // Assert
        evaluation.IsApproved.Should().BeTrue();
        evaluation.Failure.Should().BeNull();
        evaluation.Trace.Should().HaveCount(5);
    }

    [Fact]
    public void Evalua_las_reglas_en_el_orden_declarado_sin_importar_el_de_registro()
    {
        // Arrange
        var context = PrintScenarioBuilder.Valid().Build();

        // Act
        var evaluation = BuildEngine().Evaluate(context);

        // Assert
        evaluation.Trace.Select(x => x.RuleCode).Should().ContainInOrder(
            RuleCodes.RequiredData,
            RuleCodes.LabelExists,
            RuleCodes.DocumentStatus,
            RuleCodes.ZoneAvailability,
            RuleCodes.ReprintPolicy);
    }

    [Fact]
    public void Corta_en_la_primera_violacion_y_no_evalua_las_reglas_posteriores()
    {
        // Validar inventario de un LPN inexistente produciria un segundo motivo de
        // rechazo que contradice al verdadero.
        // Arrange
        var context = PrintScenarioBuilder.Valid().WithoutLabel().Build();

        // Act
        var evaluation = BuildEngine().Evaluate(context);

        // Assert
        evaluation.Failure!.RejectionCode.Should().Be(RejectionCodes.LpnNotFound);
        evaluation.Trace.Should().HaveCount(2);
        evaluation.Trace.Select(x => x.RuleCode).Should().NotContain(RuleCodes.ZoneAvailability);
    }

    [Fact]
    public void Conserva_la_traza_de_lo_evaluado_hasta_el_corte()
    {
        // La auditoria debe poder mostrar que SI se reviso, no solo que fallo.
        // Arrange
        var context = PrintScenarioBuilder.Valid()
            .WithDocumentStatus(DocumentStatus.Anulada)
            .Build();

        // Act
        var evaluation = BuildEngine().Evaluate(context);

        // Assert
        evaluation.Trace.Should().HaveCount(3);
        evaluation.Trace.Take(2).Should().OnlyContain(x => x.Passed);
        evaluation.Trace.Last().Passed.Should().BeFalse();
    }

    [Fact]
    public void El_estado_del_documento_se_evalua_antes_que_el_inventario()
    {
        // Un documento anulado no debe rechazarse por falta de stock: el motivo real
        // es el estado, y es el que el operario necesita ver.
        // Arrange
        var context = new PrintScenarioBuilder()
            .WithDocumentStatus(DocumentStatus.Anulada)
            .WithProduct("PROD-004", requested: 5, available: 1)
            .Build();

        // Act
        var evaluation = BuildEngine().Evaluate(context);

        // Assert
        evaluation.Failure!.RejectionCode.Should().Be(RejectionCodes.InvalidDocumentStatus);
    }
}
