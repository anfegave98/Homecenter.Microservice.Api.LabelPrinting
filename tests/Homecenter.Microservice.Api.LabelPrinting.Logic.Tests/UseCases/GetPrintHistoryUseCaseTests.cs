using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Printing;
using Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.TestSupport;
using Homecenter.Microservice.Api.LabelPrinting.Logic.UseCases;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.UseCases;

/// <summary>
/// Visibilidad del historial segun el rol. Cubre CP-12.
///
/// Lo que se verifica es que la restriccion llegue al repositorio, no que las filas
/// se filtren despues: filtrar en memoria (o peor, en el frontend) dejaria los datos
/// accesibles para cualquiera que llame al endpoint directamente.
/// </summary>
public sealed class GetPrintHistoryUseCaseTests
{
    private const int OperarioUserId = 7;

    [Fact]
    public async Task El_operario_solo_consulta_sus_propias_solicitudes()
    {
        // Arrange
        var (useCase, repository) = Build(OperarioUserId, RoleName.Operario);

        // Act
        var response = await useCase.ExecuteAsync(new PrintHistoryFilterDto());

        // Assert
        repository.LastRestrictToUserId.Should().Be(OperarioUserId);
        response.Meta.Should().BeEquivalentTo(new { scope = "OWN" }, options => options.ExcludingMissingMembers());
    }

    [Theory]
    [InlineData(RoleName.Supervisor)]
    [InlineData(RoleName.Admin)]
    public async Task Supervisor_y_administrador_consultan_la_operacion_completa(string role)
    {
        // Arrange
        var (useCase, repository) = Build(userId: 99, role);

        // Act
        var response = await useCase.ExecuteAsync(new PrintHistoryFilterDto());

        // Assert
        repository.LastRestrictToUserId.Should().BeNull();
        response.Meta.Should().BeEquivalentTo(new { scope = "ALL" }, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task El_operario_no_puede_ampliar_su_alcance_manipulando_el_filtro()
    {
        // Enviar userName de otra persona en el query string no debe cambiar nada:
        // la restriccion la impone el backend desde el token.
        // Arrange
        var (useCase, repository) = Build(OperarioUserId, RoleName.Operario);

        // Act
        await useCase.ExecuteAsync(new PrintHistoryFilterDto { UserName = "supervisor.tienda" });

        // Assert
        repository.LastRestrictToUserId.Should().Be(OperarioUserId);
    }

    private static (GetPrintHistoryUseCase UseCase, InMemoryPrintRequestRepository Repository) Build(
        int userId,
        params string[] roles)
    {
        var repository = new InMemoryPrintRequestRepository();
        var useCase = new GetPrintHistoryUseCase(
            repository,
            new StubCurrentUserAccessor(userId, "usuario.prueba", roles));

        return (useCase, repository);
    }
}
