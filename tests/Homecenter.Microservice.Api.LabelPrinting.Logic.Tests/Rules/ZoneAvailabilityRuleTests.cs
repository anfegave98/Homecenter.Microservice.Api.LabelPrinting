using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Rules;

/// <summary>Regla 3 — disponibilidad y abastecimiento por zona. Cubre CP-04, CP-05 y CP-06.</summary>
public sealed class ZoneAvailabilityRuleTests
{
    private readonly ZoneAvailabilityRule _rule = new();

    [Fact]
    public void Aprueba_cuando_todos_los_productos_tienen_cantidad_y_estan_abastecidos()
    {
        // Arrange
        var context = PrintScenarioBuilder.Valid()
            .WithProduct("PROD-002", requested: 1, available: 4)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Aprueba_cuando_la_cantidad_solicitada_iguala_exactamente_la_disponible()
    {
        // CP-06. El limite es inclusivo: pedir todo lo que hay es valido. Este es el
        // caso que un `>` en vez de `>=` rompe sin que ninguna prueba feliz lo note.
        // Arrange
        var context = new PrintScenarioBuilder()
            .WithProduct("PROD-006", requested: 4, available: 4)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Rechaza_con_INSUFFICIENT_INVENTORY_cuando_falta_cantidad()
    {
        // CP-04.
        // Arrange
        var context = new PrintScenarioBuilder()
            .WithProduct("PROD-004", requested: 5, available: 1)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.InsufficientInventory);
    }

    [Fact]
    public void Rechaza_con_NOT_STOCKED_cuando_hay_stock_pero_el_producto_no_esta_abastecido()
    {
        // CP-05. Son dos condiciones independientes: 50 unidades disponibles no
        // habilitan operar el producto en una zona donde no esta abastecido.
        // Arrange
        var context = new PrintScenarioBuilder()
            .WithProduct("PROD-005", requested: 2, available: 50, isStocked: false)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.NotStocked);
    }

    [Fact]
    public void Detalla_que_producto_fallo_y_por_que()
    {
        // Un rechazo generico no cumple el criterio de aceptacion de la HU-02:
        // el operario tiene que poder actuar sobre el resultado.
        // Arrange
        var context = new PrintScenarioBuilder()
            .WithProduct("PROD-004", requested: 5, available: 1)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        var shortage = result.Details.Should().ContainSingle()
            .Which.Should().BeOfType<InventoryShortageDto>().Subject;

        shortage.ProductCode.Should().Be("PROD-004");
        shortage.RequestedQty.Should().Be(5);
        shortage.AvailableQty.Should().Be(1);
        shortage.IsStocked.Should().BeTrue();
    }

    [Fact]
    public void Reporta_todos_los_productos_incumplidos_y_no_solo_el_primero()
    {
        // Cortar en el primer faltante obligaria al operario a descubrir los problemas
        // de uno en uno, reintentando la impresion cada vez.
        // Arrange
        var context = new PrintScenarioBuilder()
            .WithProduct("PROD-A", requested: 5, available: 1)
            .WithProduct("PROD-B", requested: 1, available: 99)
            .WithProduct("PROD-C", requested: 2, available: 50, isStocked: false)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Details.Should().HaveCount(2);
        result.Details!.Cast<InventoryShortageDto>().Select(x => x.ProductCode)
            .Should().BeEquivalentTo("PROD-A", "PROD-C");
    }

    [Fact]
    public void Prevalece_INSUFFICIENT_INVENTORY_cuando_coexisten_ambos_incumplimientos()
    {
        // Un solo codigo tiene que representar el rechazo. Se prioriza el faltante de
        // cantidad porque es el que el operario puede resolver reponiendo.
        // Arrange
        var context = new PrintScenarioBuilder()
            .WithProduct("PROD-A", requested: 5, available: 1)
            .WithProduct("PROD-C", requested: 2, available: 50, isStocked: false)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.RejectionCode.Should().Be(RejectionCodes.InsufficientInventory);
    }

    [Fact]
    public void Rechaza_cuando_el_producto_no_tiene_registro_de_inventario_en_la_zona()
    {
        // Ausencia de fila no es disponibilidad cero implicita que se pueda ignorar:
        // se trata como incumplimiento y se declara la razon en el detalle.
        // Arrange
        var context = new PrintScenarioBuilder()
            .WithProductWithoutInventoryRecord("PROD-SIN-REGISTRO", requested: 1)
            .Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.Details!.Cast<InventoryShortageDto>().Single().Reason.Should().Contain("no tiene registro");
    }

    [Fact]
    public void Rechaza_cuando_la_etiqueta_no_tiene_productos_asociados()
    {
        // Aprobar aqui imprimiria una etiqueta que no ampara ninguna mercancia.
        // Arrange
        var context = new PrintScenarioBuilder().Build();

        // Act
        var result = _rule.Evaluate(context);

        // Assert
        result.Passed.Should().BeFalse();
        result.RejectionCode.Should().Be(RejectionCodes.InsufficientInventory);
    }
}
