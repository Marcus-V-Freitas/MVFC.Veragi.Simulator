using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class ContractAvailabilityConfigurationTests
{
    [Fact]
    public async Task ConfigurationUsesSettingsUntilOverriddenThenUpdatesExistingEntity()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var service = new ContractAvailabilityService(fixture.Store, fixture.Options, fixture.Gate);
        var initial = await service.GetAsync(CancellationToken.None);

        // Act
        var deferred = await service.ConfigureAsync(new ContractAvailabilityRequest(ContractAvailabilityMode.DEFERRED), CancellationToken.None);
        var persisted = await service.GetAsync(CancellationToken.None);
        var immediate = await service.ConfigureAsync(new ContractAvailabilityRequest(ContractAvailabilityMode.IMMEDIATE), CancellationToken.None);
        var mode = await service.GetModeAsync(CancellationToken.None);

        // Assert
        initial.Value!.Mode.Should().Be(ContractAvailabilityMode.IMMEDIATE);
        initial.Value.Persisted.Should().BeFalse();
        deferred.Value!.Mode.Should().Be(ContractAvailabilityMode.DEFERRED);
        persisted.Value!.Persisted.Should().BeTrue();
        immediate.Value!.Mode.Should().Be(mode);
        fixture.AvailabilityConfigurations.Should().ContainSingle().Which.Id.Version.Should().Be(7);
    }

    [Fact]
    public async Task UnknownModeDoesNotPersistConfiguration()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var service = new ContractAvailabilityService(fixture.Store, fixture.Options, fixture.Gate);

        // Act
        var result = await service.ConfigureAsync(new ContractAvailabilityRequest((ContractAvailabilityMode)999), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.AvailabilityConfigurations.Should().BeEmpty();
    }
}
