using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Simulation;

public sealed class ScenarioTests
{
    [Theory]
    [InlineData("target")]
    [InlineData("cnpj")]
    [InlineData("failures-negative")]
    [InlineData("failures-large")]
    [InlineData("status-small")]
    [InlineData("status-large")]
    [InlineData("outcome-count")]
    [InlineData("contract-count")]
    [InlineData("outcome-value")]
    [InlineData("contract-value")]
    [InlineData("outcome-target")]
    [InlineData("contract-target")]
    [InlineData("hold-target")]
    public async Task InvalidScenarioIsNotSaved(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var target = "schedule-process";
        var request = new SimulationScenarioRequest();
        switch (failure)
        {
            case "target":
                target = "unknown";
                break;
            case "cnpj":
                request = request with
                {
                    MerchantCnpj = "invalid"
                };
                break;
            case "failures-negative":
                request = request with
                {
                    FailuresRemaining = -1
                };
                break;
            case "failures-large":
                request = request with
                {
                    FailuresRemaining = 10001
                };
                break;
            case "status-small":
                request = request with
                {
                    FailureStatusCode = 399
                };
                break;
            case "status-large":
                request = request with
                {
                    FailureStatusCode = 600
                };
                break;
            case "outcome-count":
                request = request with
                {
                    ProcessingOutcomes = [.. Enumerable.Repeat(ScheduleQueryStatusType.PROCESSED, 101)]
                };
                break;
            case "contract-count":
                request = request with
                {
                    ContractStatuses = [.. Enumerable.Repeat(ContractStatusType.Active, 101)]
                };
                break;
            case "outcome-value":
                request = request with
                {
                    ProcessingOutcomes = [(ScheduleQueryStatusType)99]
                };
                break;
            case "contract-value":
                request = request with
                {
                    ContractStatuses = [(ContractStatusType)99]
                };
                break;
            case "outcome-target":
                target = "contract-process";
                request = request with
                {
                    ProcessingOutcomes = [ScheduleQueryStatusType.PROCESSED]
                };
                break;
            case "contract-target":
                request = request with
                {
                    ContractStatuses = [ContractStatusType.Active]
                };
                break;
            case "hold-target":
                target = "schedule-submit";
                request = request with
                {
                    HoldProcessing = true
                };
                break;
        }

        // Act
        var result = await fixture.ScenarioService.ConfigureAsync(target, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Scenarios.Should().BeEmpty();
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Fact]
    public async Task MerchantOverrideConsumesFailuresThenFallsBackToGlobalAfterRemoval()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.ScenarioService.ConfigureAsync("merchant-create", new SimulationScenarioRequest(FailuresRemaining: 1, FailureStatusCode: 502), CancellationToken.None);
        await fixture.ScenarioService.ConfigureAsync("merchant-create", new SimulationScenarioRequest(MerchantCnpj: MockEntities.MerchantCnpj, FailuresRemaining: 2, FailureStatusCode: 503), CancellationToken.None);
        var first = await fixture.ScenarioService.TakeFailureAsync("merchant-create", MockEntities.MerchantCnpj, CancellationToken.None);
        var second = await fixture.ScenarioService.TakeFailureAsync("merchant-create", MockEntities.MerchantCnpj, CancellationToken.None);
        var success = await fixture.ScenarioService.TakeFailureAsync("merchant-create", MockEntities.MerchantCnpj, CancellationToken.None);
        await fixture.ScenarioService.ClearAsync("merchant-create", MockEntities.MerchantCnpj, CancellationToken.None);

        // Act
        var global = await fixture.ScenarioService.TakeFailureAsync("merchant-create", MockEntities.MerchantCnpj, CancellationToken.None);

        // Assert
        new[]
        {
            first,
            second,
            success,
            global
        }.Should().Equal(503, 503, null, 502);
        var listed = await fixture.ScenarioService.ListAsync(CancellationToken.None);
        listed.Should().ContainSingle().Which.Calls.Should().Be(1);
    }

    [Fact]
    public async Task UpdatingScenarioResetsCountersAndEmptySequencesDefaultToSuccess()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.ScenarioService.ConfigureAsync("schedule-process", new SimulationScenarioRequest(FailuresRemaining: 1), CancellationToken.None);
        var error = await fixture.ScenarioService.NextScheduleStatusAsync(MockEntities.MerchantCnpj, CancellationToken.None);
        await fixture.ScenarioService.ConfigureAsync("schedule-process", new SimulationScenarioRequest(), CancellationToken.None);
        await fixture.ScenarioService.ConfigureAsync("contract-process", new SimulationScenarioRequest(FailuresRemaining: 1), CancellationToken.None);
        var cancelled = await fixture.ScenarioService.NextContractStatusAsync(MockEntities.MerchantCnpj, CancellationToken.None);
        var schedule = await fixture.ScenarioService.NextScheduleStatusAsync(MockEntities.MerchantCnpj, CancellationToken.None);

        // Act
        var contract = await fixture.ScenarioService.NextContractStatusAsync(MockEntities.MerchantCnpj, CancellationToken.None);

        // Assert
        error.Should().Be(ScheduleQueryStatusType.ERROR);
        cancelled.Should().Be(ContractStatusType.Cancelled);
        schedule.Should().Be(ScheduleQueryStatusType.PROCESSED);
        contract.Should().Be(ContractStatusType.Active);
        fixture.Scenarios.Single(x => x.Target == "schedule-process").Calls.Should().Be(1);
    }

    [Theory]
    [InlineData("unknown", null)]
    [InlineData("merchant-create", "invalid")]
    public async Task ClearRejectsInvalidScope(string target, string? cnpj)
    {
        // Arrange
        using var fixture = new ServiceFixture();

        // Act
        var result = await fixture.ScenarioService.ClearAsync(target, cnpj, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ClearingAbsentScenarioSucceedsWithoutWriting()
    {
        // Arrange
        using var fixture = new ServiceFixture();

        // Act
        var result = await fixture.ScenarioService.ClearAsync("merchant-create", null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }
}
