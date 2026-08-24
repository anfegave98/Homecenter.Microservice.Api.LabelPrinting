using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Rules;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Services;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;
using Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.UseCases;

/// <summary>
/// Resolucion de las reimpresiones que un operario dejo pendientes.
///
/// El punto delicado no es aprobar: es que aprobar no puede saltarse las reglas. Entre
/// la solicitud y la decision pasa tiempo real, y en ese tiempo el documento puede
/// anularse o el inventario agotarse.
/// </summary>
public sealed class ResolveReprintApprovalUseCaseTests
{
    private const int ZoneId = 1;
    private const string ZoneCode = "ZONA-PICKING-A";
    private const string Lpn = "LPN-000987654";

    private readonly SpyPrintSimulator _printer = new();

    [Fact]
    public async Task Imprime_y_cierra_la_solicitud_cuando_el_supervisor_aprueba()
    {
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.ApproveAsync(
            fixture.PendingId,
            new ReprintDecisionDto { Note = "Etiqueta danada, verificado en piso" });

        // Assert
        response.Success.Should().BeTrue();
        response.Data!.Result.Should().Be("APPROVED");
        response.Data.Zpl.Should().NotBeNullOrEmpty();
        _printer.Invocations.Should().Be(1);
    }

    [Fact]
    public async Task Registra_al_autorizador_sin_reemplazar_al_solicitante()
    {
        // Quien pide y quien autoriza son dos personas: una auditoria que las confunda
        // no puede responder quien aprobo el duplicado.
        // Arrange
        var fixture = BuildFixture();

        // Act
        await fixture.UseCase.ApproveAsync(
            fixture.PendingId,
            new ReprintDecisionDto { Note = "Verificado" });

        // Assert
        var decided = fixture.Audit.Updated.Should().ContainSingle().Subject;
        decided.IdUser.Should().Be(7, "el solicitante original no cambia");
        decided.IdApprover.Should().Be(3);
        decided.DecidedAt.Should().NotBeNull();
        decided.ApprovalNote.Should().Be("Verificado");
    }

    [Fact]
    public async Task Conserva_el_motivo_que_escribio_el_operario()
    {
        // Es lo unico que el supervisor tiene para decidir cuando abre la bandeja.
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.ApproveAsync(
            fixture.PendingId,
            new ReprintDecisionDto { Note = "Verificado" });

        // Assert
        response.Data!.ReprintReason.Should().Be("Etiqueta rota en el montacargas");
        response.Data.UserName.Should().Be("operario.tienda");
        response.Data.ApprovedBy.Should().Be("supervisor.tienda");
    }

    [Fact]
    public async Task No_imprime_cuando_el_documento_se_anulo_mientras_esperaba()
    {
        // El caso que justifica revalidar: la autorizacion llega tarde y el documento
        // origen ya no admite impresion. El visto bueno no puede volver valido lo que
        // dejo de serlo.
        // Arrange
        var fixture = BuildFixture(status: DocumentStatus.Anulada);

        // Act
        var response = await fixture.UseCase.ApproveAsync(
            fixture.PendingId,
            new ReprintDecisionDto { Note = "Autorizo" });

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Code.Should().Be(RejectionCodes.InvalidDocumentStatus);
        _printer.Invocations.Should().Be(0);
        fixture.Audit.Updated.Single().Result.Should().Be(PrintResult.Rejected);
    }

    [Fact]
    public async Task No_imprime_cuando_el_inventario_se_agoto_mientras_esperaba()
    {
        // Arrange
        var fixture = BuildFixture(availableQty: 0m);

        // Act
        var response = await fixture.UseCase.ApproveAsync(
            fixture.PendingId,
            new ReprintDecisionDto { Note = "Autorizo" });

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.InsufficientInventory);
        _printer.Invocations.Should().Be(0);
    }

    [Fact]
    public async Task Niega_la_solicitud_dejando_el_motivo_registrado()
    {
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.RejectAsync(
            fixture.PendingId,
            new ReprintDecisionDto { Note = "La etiqueta original aparecio" });

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Code.Should().Be(RejectionCodes.ReprintRejectedByApprover);

        var decided = fixture.Audit.Updated.Should().ContainSingle().Subject;
        decided.Result.Should().Be(PrintResult.Rejected);
        decided.ApprovalNote.Should().Be("La etiqueta original aparecio");
        decided.IdApprover.Should().Be(3);
        _printer.Invocations.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Exige_motivo_para_negar(string? note)
    {
        // Un rechazo sin explicacion deja al operario sin saber que corregir.
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.RejectAsync(
            fixture.PendingId,
            new ReprintDecisionDto { Note = note });

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.ApprovalNoteRequired);
        fixture.Audit.Updated.Should().BeEmpty("una solicitud no resuelta no puede quedar tocada");
    }

    [Fact]
    public async Task Responde_sin_encontrar_nada_cuando_la_solicitud_ya_fue_resuelta()
    {
        // Dos supervisores pueden abrir la bandeja a la vez. El segundo no debe poder
        // volver a decidir sobre algo ya cerrado.
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.ApproveAsync(
            fixture.PendingId + 999,
            new ReprintDecisionDto { Note = "Autorizo" });

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.PendingRequestNotFound);
        _printer.Invocations.Should().Be(0);
    }

    [Fact]
    public async Task Deja_traza_de_la_segunda_evaluacion_sin_borrar_la_primera()
    {
        // El historial debe mostrar que se valido al pedir y que se valido al autorizar:
        // son dos momentos distintos y pueden haber dado resultados distintos.
        // Arrange
        var fixture = BuildFixture(existingTraceSteps: 5);

        // Act
        await fixture.UseCase.ApproveAsync(fixture.PendingId, new ReprintDecisionDto { Note = "Autorizo" });

        // Assert
        var decided = fixture.Audit.Updated.Single();
        decided.AuditLogs.Should().HaveCountGreaterThan(5);
        decided.AuditLogs.Should().Contain(x => x.RuleCode == RuleCodes.ReprintApproval && x.Passed);
    }

    // -----------------------------------------------------------------------
    // Montaje
    // -----------------------------------------------------------------------

    private Fixture BuildFixture(
        DocumentStatus status = DocumentStatus.Liberada,
        decimal availableQty = 10m,
        int existingTraceSteps = 0)
    {
        var zone = new Zone { Id = ZoneId, Code = ZoneCode, Name = "Zona Picking A" };

        var documentProduct = new DocumentProduct
        {
            Id = 1,
            IdDocument = 1,
            IdProduct = 1,
            RequestedQty = 2m,
            Uom = "UND",
            Product = new Product { Id = 1, ProductCode = "PROD-001", ProductDescription = "Martillo 16oz" }
        };

        var document = new Document
        {
            Id = 1,
            RequestId = "REQ-20260605-001",
            DocumentType = "NOTA_PEDIDO",
            DocumentNumber = "NP-458721",
            Status = status,
            IdZone = ZoneId,
            RequestedBy = "usuario.operacion",
            RequestDateTime = DateTimeOffset.UtcNow,
            Zone = zone,
            DocumentProducts = new List<DocumentProduct> { documentProduct }
        };

        var label = new Label
        {
            Id = 1,
            IdDocument = 1,
            EtqId = "ETQ-10001",
            LpnId = Lpn,
            IsPreGenerated = true,
            TemplateCode = "TPL-ETQ-STD-4X6",
            Zpl = "^XA^FDprueba^FS^XZ",
            Document = document
        };

        var inventory = new InventoryAvailability
        {
            IdProduct = 1,
            IdZone = ZoneId,
            AvailableQty = availableQty,
            IsStocked = true
        };

        // hasApprovedPrint queda en true: la solicitud pendiente existe precisamente
        // porque la etiqueta ya se habia impreso antes.
        var audit = new InMemoryPrintRequestRepository(hasApprovedPrint: true);

        var pending = new PrintRequest
        {
            Id = 100,
            CorrelationId = Guid.NewGuid(),
            EtqId = "ETQ-10001",
            LpnId = Lpn,
            IdZone = ZoneId,
            IdUser = 7,
            DocumentNumber = "NP-458721",
            Result = PrintResult.PendingApproval,
            EventType = PrintEventType.Reprint,
            RejectionCode = RejectionCodes.ReprintPendingApproval,
            ReprintReason = "Etiqueta rota en el montacargas",
            ProcessedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            Zone = zone,
            User = new User { Id = 7, UserName = "operario.tienda", FullName = "Operario Tienda 01" }
        };

        for (var step = 0; step < existingTraceSteps; step++)
        {
            pending.AuditLogs.Add(new PrintAuditLog
            {
                RuleCode = $"R{step}_PREVIA",
                Passed = true,
                EvaluatedAt = pending.ProcessedAt
            });
        }

        audit.Pending.Add(pending);

        var approver = new StubCurrentUserAccessor(3, "supervisor.tienda", RoleName.Supervisor);

        var contextBuilder = new PrintContextBuilder(
            new InMemoryLabelRepository(label),
            new InMemoryZoneRepository(zone),
            new InMemoryInventoryRepository(inventory),
            audit,
            approver);

        var engine = new PrintRuleEngine(new IPrintRule[]
        {
            new RequiredDataRule(),
            new LabelExistsRule(),
            new DocumentStatusRule(),
            new ZoneAvailabilityRule(),
            new ReprintPolicyRule()
        });

        var useCase = new ResolveReprintApprovalUseCase(
            audit,
            approver,
            _printer,
            contextBuilder,
            engine,
            NullLogger<ResolveReprintApprovalUseCase>.Instance);

        return new Fixture(useCase, audit, pending.Id);
    }

    private sealed record Fixture(
        ResolveReprintApprovalUseCase UseCase,
        InMemoryPrintRequestRepository Audit,
        int PendingId);
}
