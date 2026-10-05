using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using Xunit;

namespace MVFC.Veragi.Simulator.Shareable.Tests.Extensions;

public sealed class ConfigurationExtensionsTests
{
    [Fact]
    public void LoadSimulatorOptionsWithFullConfigurationLoadsAllProperties()
    {
        // Arrange
        var inMemory = new Dictionary<string, string?>
        {
            ["Simulator:ContractAvailabilityMode"] = "DEFERRED",
            ["Simulator:ScheduleWebhookSchema"] = "Schedule",
            ["Simulator:TimeZone"] = "America/Sao_Paulo",
            ["Simulator:ProcessingDelaySeconds"] = "5",
            ["Simulator:PollIntervalSeconds"] = "2",
            ["Simulator:MinimumBusinessDays"] = "3",
            ["Simulator:Holidays:0"] = "2026-12-25",
            ["Simulator:MaxDeliveryAttempts"] = "7",
            ["Simulator:RetryBaseSeconds"] = "4",
            ["Simulator:WebhookTimeoutSeconds"] = "15",
            ["Simulator:EnableControlEndpoints"] = "true",
            ["Simulator:SchedulersEnabled"] = "true",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        // Act
        var options = configuration.LoadSimulatorOptions();

        // Assert
        options.ContractAvailabilityMode.Should().Be(ContractAvailabilityMode.DEFERRED);
        options.ScheduleWebhookSchema.Should().Be(ScheduleWebhookSchema.Schedule);
        options.TimeZone.Should().Be("America/Sao_Paulo");
        options.ProcessingDelaySeconds.Should().Be(5);
        options.PollIntervalSeconds.Should().Be(2);
        options.MinimumBusinessDays.Should().Be(3);
        options.Holidays.Should().ContainSingle().Which.Should().Be("2026-12-25");
        options.MaxDeliveryAttempts.Should().Be(7);
        options.RetryBaseSeconds.Should().Be(4);
        options.WebhookTimeoutSeconds.Should().Be(15);
        options.EnableControlEndpoints.Should().BeTrue();
        options.SchedulersEnabled.Should().BeTrue();
    }

    [Fact]
    public void LoadSimulatorOptionsWithEmptyConfigurationUsesFallbacks()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        // Act
        var options = configuration.LoadSimulatorOptions();

        // Assert
        options.TimeZone.Should().BeEmpty();
        options.Holidays.Should().BeEmpty();
        options.EnableControlEndpoints.Should().BeFalse();
        options.SchedulersEnabled.Should().BeFalse();
    }
}
