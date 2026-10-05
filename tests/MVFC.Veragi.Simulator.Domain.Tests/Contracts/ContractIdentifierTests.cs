using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class ContractIdentifierTests
{
    [Fact]
    public void GeneratedIdentifierHasFixedBlocksAndUsesProvidedClock()
    {
        // Arrange
        var now = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        // Act
        var identifier = ContractIdentifierGenerator.Create(now);

        // Assert
        identifier.Should().MatchRegex("^CON/[0-9]{8}/[0-9]{14}/020126/030405$");
        identifier.Should().HaveLength(41);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("CLIENT-123")]
    [InlineData("HTTP-1791216610601-TOTAL")]
    [InlineData("CON/07237373/21892484000109/041125/115951")]
    public async Task IdempotentRetryKeepsIdentifierAndOriginalRequestBeforeAndAfterProcessing(string? provided)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var request = fixture.Contract() with { FinancierContractId = provided };
        var key = ServiceFixture.Key();
        var created = await fixture.ContractService.CreateAsync(request, key, CancellationToken.None);
        var summary = created.Value![0];
        var operation = fixture.Operations.Single(item => item.Kind == "contract");
        var identifier = summary.FinancierContractId;
        var reference = summary.ExternalReference;

        // Act
        var retryBefore = await fixture.ContractService.CreateAsync(request, key, CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var retryAfter = await fixture.ContractService.CreateAsync(request, key, CancellationToken.None);
        var details = await fixture.ContractService.GetAsync(request.ContractorCnpj!, summary.ExternalReference!, CancellationToken.None);

        // Assert
        if (provided is null)
            identifier.Should().Be(reference);
        else
            identifier.Should().Be(provided);

        retryBefore.Value![0].FinancierContractId.Should().Be(identifier);
        retryAfter.Value![0].FinancierContractId.Should().Be(identifier);
        details.Value!.FinancierContractId.Should().Be(identifier);
        reference.Should().MatchRegex("^CON/[0-9]{8}/[0-9]{14}/[0-9]{6}/[0-9]{6}$");
        reference.Should().HaveLength(41);
        retryBefore.Value[0].ExternalReference.Should().Be(reference);
        retryAfter.Value[0].ExternalReference.Should().Be(reference);
        operation.ExternalReference.Should().Be(reference);
        operation.RequestJson.Should().Be(request.ToJson());
        operation.RequestHash.Should().Be(request.Fingerprint());
        fixture.Operations.Count(item => item.Kind == "contract").Should().Be(1);
    }

    [Theory]
    [InlineData(ContractStatusType.Active)]
    [InlineData(ContractStatusType.Cancelled)]
    [InlineData(ContractStatusType.PendingEdit)]
    [InlineData(ContractStatusType.PendingRegistration)]
    [InlineData(ContractStatusType.Settled)]
    [InlineData(ContractStatusType.ContractSimulation)]
    public async Task ProcessingStatesKeepGeneratedIdentifier(ContractStatusType status)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ScenarioService.ConfigureAsync("contract-process", new SimulationScenarioRequest(ContractStatuses: [status]), CancellationToken.None);
        var created = await fixture.ContractService.CreateAsync(fixture.Contract(), ServiceFixture.Key(), CancellationToken.None);
        var original = created.Value![0];

        // Act
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var details = await fixture.ContractService.GetAsync(original.ContractorCnpj!, original.ExternalReference!, CancellationToken.None);

        // Assert
        details.Value!.FinancierContractId.Should().Be(original.FinancierContractId);
        details.Value.Status.Should().Be(status);
        fixture.Operations.Single(item => item.Kind == "contract").ExternalReference.Should().Be(original.ExternalReference);
    }

    [Theory]
    [InlineData(null, null, "SIM-legacy")]
    [InlineData(null, "persisted", "persisted")]
    [InlineData("client", null, "client")]
    public async Task LegacySnapshotPreservesExistingIdentifierFallbacks(string? provided, string? persisted, string expected)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var request = fixture.Contract() with { FinancierContractId = provided };
        await fixture.ContractService.CreateAsync(request, ServiceFixture.Key(), CancellationToken.None);
        var operation = fixture.Operations.Single(item => item.Kind == "contract");
        operation.ExternalReference = "SIM-legacy";
        operation.ResultJson = (operation.ResultJson.FromJson<ContractByExternalReference>()! with { FinancierContractId = persisted }).ToJson();

        // Act
        var before = operation.ToSummary();
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var after = operation.ToSummary();

        // Assert
        before.FinancierContractId.Should().Be(expected);
        after.FinancierContractId.Should().Be(expected);
        operation.ResultJson.FromJson<ContractByExternalReference>()!.FinancierContractId.Should().Be(expected);
        operation.ExternalReference.Should().Be("SIM-legacy");
        var details = await fixture.ContractService.GetAsync(request.ContractorCnpj!, "SIM-legacy", CancellationToken.None);
        details.Value!.FinancierContractId.Should().Be(expected);
    }
}
