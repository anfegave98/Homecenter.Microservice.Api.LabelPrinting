using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Common;
using Homecenter.Microservice.Api.LabelPrinting.Entities;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;
using Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.UseCases;

/// <summary>
/// Entrega de la etiqueta de una solicitud aprobada.
///
/// Lo que hay que proteger es que la descarga sea única. Si se pudiera repetir, el
/// control de reimpresiones quedaría de adorno: cualquiera obtendría copias sin motivo
/// ni autorización.
/// </summary>
public sealed class DownloadLabelUseCaseTests
{
    private const string Lpn = "LPN-000987654";

    [Fact]
    public async Task Entrega_el_zpl_de_una_solicitud_aprobada()
    {
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.ExecuteAsync(100);

        // Assert
        response.Success.Should().BeTrue();
        response.Data!.Content.Should().Be("^XA^FDprueba^FS^XZ");
        response.Data.FileName.Should().Be("ETQ-10001_100.zpl");
    }

    [Fact]
    public async Task Deja_constancia_de_quien_descargo_y_cuando()
    {
        // Arrange
        var fixture = BuildFixture();

        // Act
        await fixture.UseCase.ExecuteAsync(100);

        // Assert
        var updated = fixture.Audit.Updated.Should().ContainSingle().Subject;
        updated.DownloadedAt.Should().NotBeNull();
        updated.IdDownloadedBy.Should().Be(7);
    }

    [Fact]
    public async Task Niega_la_segunda_descarga_de_la_misma_solicitud()
    {
        // El control entero depende de esto: una aprobación entrega una etiqueta, no un
        // permiso permanente para reimprimir sin motivo.
        // Arrange
        var fixture = BuildFixture(alreadyDownloaded: true);

        // Act
        var response = await fixture.UseCase.ExecuteAsync(100);

        // Assert
        response.Success.Should().BeFalse();
        response.Error!.Code.Should().Be(RejectionCodes.LabelAlreadyDownloaded);
        fixture.Audit.Updated.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_operario_no_descarga_la_etiqueta_de_otro()
    {
        // Arrange
        var fixture = BuildFixture(requesterId: 99);

        // Act
        var response = await fixture.UseCase.ExecuteAsync(100);

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.LabelNotAvailable);
        fixture.Audit.LastDownloadRestriction.Should().Be(7, "la restricción debe imponerse en el repositorio");
    }

    [Fact]
    public async Task Un_supervisor_descarga_la_etiqueta_de_cualquiera()
    {
        // Mismo alcance que el historial: quien ve toda la operación puede resolverla.
        // Arrange
        var fixture = BuildFixture(requesterId: 99, roles: new[] { RoleName.Supervisor });

        // Act
        var response = await fixture.UseCase.ExecuteAsync(100);

        // Assert
        response.Success.Should().BeTrue();
        fixture.Audit.LastDownloadRestriction.Should().BeNull();
    }

    [Fact]
    public async Task No_entrega_nada_cuando_la_solicitud_no_existe()
    {
        // Arrange
        var fixture = BuildFixture();

        // Act
        var response = await fixture.UseCase.ExecuteAsync(404);

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.LabelNotAvailable);
    }

    [Fact]
    public async Task No_entrega_nada_cuando_la_etiqueta_ya_no_se_resuelve()
    {
        // La solicitud se aprobó, luego la etiqueta existía. Si desapareció, el dato
        // cambió debajo y no hay archivo que entregar.
        // Arrange
        var fixture = BuildFixture(labelExists: false);

        // Act
        var response = await fixture.UseCase.ExecuteAsync(100);

        // Assert
        response.Error!.Code.Should().Be(RejectionCodes.LabelNotAvailable);
        fixture.Audit.Updated.Should().BeEmpty("no se marca como descargada una etiqueta que no se entregó");
    }

    // -----------------------------------------------------------------------
    // Montaje
    // -----------------------------------------------------------------------

    private static Fixture BuildFixture(
        bool alreadyDownloaded = false,
        bool labelExists = true,
        int requesterId = 7,
        string[]? roles = null)
    {
        var label = labelExists
            ? new Label
            {
                Id = 1,
                IdDocument = 1,
                EtqId = "ETQ-10001",
                LpnId = Lpn,
                IsPreGenerated = true,
                TemplateCode = "TPL-ETQ-STD-4X6",
                Zpl = "^XA^FDprueba^FS^XZ"
            }
            : null;

        var audit = new InMemoryPrintRequestRepository();

        audit.Approved.Add(new PrintRequest
        {
            Id = 100,
            CorrelationId = Guid.NewGuid(),
            EtqId = "ETQ-10001",
            LpnId = Lpn,
            IdUser = requesterId,
            Result = PrintResult.Approved,
            EventType = PrintEventType.Print,
            ProcessedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            DownloadedAt = alreadyDownloaded ? DateTimeOffset.UtcNow.AddMinutes(-1) : null
        });

        var currentUser = new StubCurrentUserAccessor(
            7,
            "operario.tienda",
            roles ?? new[] { RoleName.Operario });

        var useCase = new DownloadLabelUseCase(
            audit,
            new InMemoryLabelRepository(label),
            currentUser,
            NullLogger<DownloadLabelUseCase>.Instance);

        return new Fixture(useCase, audit);
    }

    private sealed record Fixture(DownloadLabelUseCase UseCase, InMemoryPrintRequestRepository Audit);
}
