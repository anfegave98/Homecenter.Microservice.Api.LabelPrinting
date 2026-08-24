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

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.UseCases;

/// <summary>
/// Caso de uso completo con repositorios dobles en memoria. Cubre CP-01 a CP-10.
///
/// Las pruebas de reglas verifican cada decision en aislamiento; estas verifican lo
/// que solo se ve al ensamblar todo: que se imprima unicamente cuando corresponde,
/// que la auditoria se persista siempre y que el rechazo de negocio viaje como
/// respuesta valida y no como error tecnico.
/// </summary>
public sealed class ProcessPrintRequestUseCaseTests
{
    private const int ZoneId = 1;
    private const string ZoneCode = "ZONA-PICKING-A";

    private readonly SpyPrintSimulator _printer = new();

    [Fact]
    public async Task Aprueba_la_primera_impresion_y_entrega_el_zpl()
    {
        // CP-07.
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Success.Should().BeTrue();
        response.Data!.Result.Should().Be("APPROVED");
        response.Data.EventType.Should().Be("PRINT");
        response.Data.Zpl.Should().NotBeNullOrWhiteSpace();
        _printer.Invocations.Should().Be(1);
    }

    [Fact]
    public async Task El_exito_incluye_el_bloque_legacy_compatible_con_el_anexo()
    {
        // El consumidor de responseEtq.json no debe romperse, pero tampoco se le puede
        // ocultar que la ETQ arrastra mas productos de los que ese contrato representa.
        // Arrange
        var fixture = BuildFixture(products: new[]
        {
            (code: "PROD-001", requested: 2m, available: 10m, stocked: true),
            (code: "PROD-002", requested: 1m, available: 4m, stocked: true)
        });

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Data!.Legacy.Should().NotBeNull();
        response.Data.Legacy!.Sku.Should().Be("PROD-001");
        response.Data.Legacy.Unidades.Should().Be(2);
        response.Data.Legacy.HasMultipleProducts.Should().BeTrue();
    }

    [Fact]
    public async Task Rechaza_un_LPN_inexistente_sin_imprimir()
    {
        // CP-01.
        // Arrange
        var fixture = BuildFixture(labelExists: false);

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-NO-EXISTE", ZoneCode = ZoneCode });

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Code.Should().Be(RejectionCodes.LpnNotFound);
        _printer.Invocations.Should().Be(0);
    }

    [Theory]
    [InlineData(DocumentStatus.Anulada)]
    [InlineData(DocumentStatus.Devuelta)]
    public async Task Rechaza_documentos_en_estado_bloqueado(DocumentStatus status)
    {
        // CP-02 y CP-03.
        // Arrange
        var fixture = BuildFixture(status: status);

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Code.Should().Be(RejectionCodes.InvalidDocumentStatus);
        _printer.Invocations.Should().Be(0);
    }

    [Fact]
    public async Task Rechaza_por_disponibilidad_insuficiente_detallando_el_producto()
    {
        // CP-04.
        // Arrange
        var fixture = BuildFixture(products: new[] { (code: "PROD-004", requested: 5m, available: 1m, stocked: true) });

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.InsufficientInventory);
        response.Error.Details.Should().ContainSingle();
    }

    [Fact]
    public async Task Rechaza_producto_con_stock_pero_no_abastecido_en_la_zona()
    {
        // CP-05.
        // Arrange
        var fixture = BuildFixture(products: new[] { (code: "PROD-005", requested: 2m, available: 50m, stocked: false) });

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.NotStocked);
    }

    [Fact]
    public async Task Aprueba_cuando_la_cantidad_solicitada_iguala_la_disponible()
    {
        // CP-06, el limite inclusivo verificado extremo a extremo.
        // Arrange
        var fixture = BuildFixture(products: new[] { (code: "PROD-006", requested: 4m, available: 4m, stocked: true) });

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Marca_como_REPRINT_y_audita_el_motivo_cuando_un_supervisor_reimprime()
    {
        // CP-08.
        // Arrange
        var fixture = BuildFixture(
            hasPreviousPrint: true,
            userName: "supervisor.tienda",
            roles: new[] { RoleName.Supervisor });

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto
        {
            Lpn = "LPN-000987654",
            ZoneCode = ZoneCode,
            ReprintReason = "Etiqueta danada en piso"
        });

        // Assert
        response.Success.Should().BeTrue();
        response.Data!.EventType.Should().Be("REPRINT");
        response.Data.ReprintReason.Should().Be("Etiqueta danada en piso");
        fixture.Audit.Saved.Single().ReprintReason.Should().Be("Etiqueta danada en piso");
    }

    [Fact]
    public async Task Rechaza_la_reimpresion_sin_motivo()
    {
        // CP-09.
        // Arrange
        var fixture = BuildFixture(
            hasPreviousPrint: true,
            userName: "supervisor.tienda",
            roles: new[] { RoleName.Supervisor });

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.ReprintReasonRequired);
        _printer.Invocations.Should().Be(0);
    }

    [Fact]
    public async Task Deja_pendiente_la_reimpresion_solicitada_por_un_operario()
    {
        // CP-10. No se imprime, pero tampoco se cierra: queda esperando decision.
        // Arrange
        var fixture = BuildFixture(hasPreviousPrint: true);

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto
        {
            Lpn = "LPN-000987654",
            ZoneCode = ZoneCode,
            ReprintReason = "Se perdio la etiqueta"
        });

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.ReprintPendingApproval);
        response.Data!.Result.Should().Be("PENDING_APPROVAL");
        _printer.Invocations.Should().Be(0);
    }

    [Fact]
    public async Task Audita_como_pendiente_la_reimpresion_derivada_a_autorizacion()
    {
        // El motivo debe quedar guardado desde el primer momento: es lo unico que el
        // supervisor tendra para decidir cuando abra la bandeja mas tarde.
        // Arrange
        var fixture = BuildFixture(hasPreviousPrint: true);

        // Act
        await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto
        {
            Lpn = "LPN-000987654",
            ZoneCode = ZoneCode,
            ReprintReason = "Se perdio la etiqueta"
        });

        // Assert
        var saved = fixture.Audit.Saved.Should().ContainSingle().Subject;
        saved.Result.Should().Be(PrintResult.PendingApproval);
        saved.EventType.Should().Be(PrintEventType.Reprint);
        saved.ReprintReason.Should().Be("Se perdio la etiqueta");
        saved.IdApprover.Should().BeNull();
        saved.DecidedAt.Should().BeNull();
    }

    [Fact]
    public async Task Audita_la_solicitud_aprobada_con_su_traza_de_reglas()
    {
        // Arrange
        var fixture = BuildFixture();

        // Act
        await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        var saved = fixture.Audit.Saved.Should().ContainSingle().Subject;
        saved.Result.Should().Be(PrintResult.Approved);
        saved.EventType.Should().Be(PrintEventType.Print);
        saved.AuditLogs.Should().HaveCount(5);
        saved.AuditLogs.Should().OnlyContain(x => x.Passed);
    }

    [Fact]
    public async Task Audita_tambien_la_solicitud_rechazada_con_su_codigo_y_motivo()
    {
        // La auditoria nunca es condicional al exito: los rechazos son justamente lo
        // que se investiga durante un incidente productivo.
        // Arrange
        var fixture = BuildFixture(status: DocumentStatus.Anulada);

        // Act
        await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        var saved = fixture.Audit.Saved.Should().ContainSingle().Subject;
        saved.Result.Should().Be(PrintResult.Rejected);
        saved.RejectionCode.Should().Be(RejectionCodes.InvalidDocumentStatus);
        saved.RejectionMessage.Should().NotBeNullOrWhiteSpace();
        saved.AuditLogs.Should().Contain(x => !x.Passed);
    }

    [Fact]
    public async Task Audita_incluso_cuando_el_LPN_no_existe()
    {
        // Sin este registro, un LPN mal digitado repetidamente seria invisible.
        // Arrange
        var fixture = BuildFixture(labelExists: false);

        // Act
        await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-NO-EXISTE", ZoneCode = ZoneCode });

        // Assert
        var saved = fixture.Audit.Saved.Should().ContainSingle().Subject;
        saved.LpnId.Should().Be("LPN-NO-EXISTE");
        saved.EtqId.Should().BeNull();
        saved.RejectionCode.Should().Be(RejectionCodes.LpnNotFound);
    }

    [Fact]
    public async Task El_rechazo_de_negocio_conserva_el_correlationId_para_diagnostico()
    {
        // El rechazo viaja como respuesta valida del dominio, no como error tecnico,
        // y aun asi debe poder rastrearse hasta su registro de auditoria.
        // Arrange
        var fixture = BuildFixture(status: DocumentStatus.Anulada);

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Data.Should().NotBeNull();
        response.Data!.CorrelationId.Should().NotBeEmpty();
        response.Data.CorrelationId.Should().Be(fixture.Audit.Saved.Single().CorrelationId);
    }

    [Fact]
    public async Task El_usuario_auditado_es_el_del_token_y_no_el_del_body()
    {
        // Arrange
        var fixture = BuildFixture(userId: 42, userName: "operario.tienda");

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654", ZoneCode = ZoneCode });

        // Assert
        response.Data!.UserName.Should().Be("operario.tienda");
        fixture.Audit.Saved.Single().IdUser.Should().Be(42);
    }

    [Fact]
    public async Task Usa_la_zona_del_documento_cuando_la_solicitud_no_la_indica()
    {
        // El anexo requetEtq.json solo trae el LPN: omitir la zona debe seguir funcionando.
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto { Lpn = "LPN-000987654" });

        // Assert
        response.Success.Should().BeTrue();
        response.Data!.ZoneCode.Should().Be(ZoneCode);
    }

    [Fact]
    public async Task Rechaza_cuando_la_zona_solicitada_no_existe()
    {
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.ExecuteAsync(new PrintRequestCreateDto
        {
            Lpn = "LPN-000987654",
            ZoneCode = "ZONA-INVENTADA"
        });

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.ZoneNotFound);
    }

    // -----------------------------------------------------------------------
    // Montaje
    // -----------------------------------------------------------------------

    private Fixture BuildFixture(
        bool labelExists = true,
        DocumentStatus status = DocumentStatus.Liberada,
        bool hasPreviousPrint = false,
        int userId = 1,
        string userName = "operario.tienda",
        string[]? roles = null,
        (string code, decimal requested, decimal available, bool stocked)[]? products = null)
    {
        products ??= new[] { (code: "PROD-001", requested: 2m, available: 10m, stocked: true) };

        var zone = new Zone { Id = ZoneId, Code = ZoneCode, Name = "Zona Picking A" };

        var documentProducts = products
            .Select((line, index) => new DocumentProduct
            {
                Id = index + 1,
                IdDocument = 1,
                IdProduct = index + 1,
                RequestedQty = line.requested,
                Uom = "UND",
                Product = new Product
                {
                    Id = index + 1,
                    ProductCode = line.code,
                    ProductDescription = $"Descripcion de {line.code}"
                }
            })
            .ToList();

        var inventory = products
            .Select((line, index) => new InventoryAvailability
            {
                IdProduct = index + 1,
                IdZone = ZoneId,
                AvailableQty = line.available,
                IsStocked = line.stocked
            })
            .ToArray();

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
            DocumentProducts = documentProducts
        };

        var label = labelExists
            ? new Label
            {
                Id = 1,
                IdDocument = 1,
                EtqId = "ETQ-10001",
                LpnId = "LPN-000987654",
                IsPreGenerated = true,
                TemplateCode = "TPL-ETQ-STD-4X6",
                Zpl = "^XA^FDprueba^FS^XZ",
                Document = document
            }
            : null;

        var audit = new InMemoryPrintRequestRepository(hasPreviousPrint);

        var engine = new PrintRuleEngine(new IPrintRule[]
        {
            new RequiredDataRule(),
            new LabelExistsRule(),
            new DocumentStatusRule(),
            new ZoneAvailabilityRule(),
            new ReprintPolicyRule()
        });

        var currentUser = new StubCurrentUserAccessor(userId, userName, roles ?? new[] { RoleName.Operario });

        var contextBuilder = new PrintContextBuilder(
            new InMemoryLabelRepository(label),
            new InMemoryZoneRepository(zone),
            new InMemoryInventoryRepository(inventory),
            audit,
            currentUser);

        var useCase = new ProcessPrintRequestUseCase(
            audit,
            currentUser,
            _printer,
            contextBuilder,
            engine,
            NullLogger<ProcessPrintRequestUseCase>.Instance);

        return new Fixture(useCase, audit);
    }

    private sealed record Fixture(ProcessPrintRequestUseCase UseCase, InMemoryPrintRequestRepository Audit);
}
