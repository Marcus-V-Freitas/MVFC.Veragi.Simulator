using Microsoft.Extensions.Configuration;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Extensions;

public static class ConfigurationExtensions
{
    public static SimulatorOptions LoadSimulatorOptions(this IConfiguration configuration)
    {
        var section = configuration.GetSection("Simulator");

        return new SimulatorOptions(
            section.GetValue<ContractAvailabilityMode>(nameof(SimulatorOptions.ContractAvailabilityMode)),
            section.GetValue<ScheduleWebhookSchema>(nameof(SimulatorOptions.ScheduleWebhookSchema)),
            section.GetValue<string>(nameof(SimulatorOptions.TimeZone)) ?? string.Empty,
            section.GetValue<int>(nameof(SimulatorOptions.ProcessingDelaySeconds)),
            section.GetValue<int>(nameof(SimulatorOptions.PollIntervalSeconds)),
            section.GetValue<int>(nameof(SimulatorOptions.MinimumBusinessDays)),
            section.GetSection(nameof(SimulatorOptions.Holidays)).Get<List<string>>() ?? [],
            section.GetValue<int>(nameof(SimulatorOptions.MaxDeliveryAttempts)),
            section.GetValue<int>(nameof(SimulatorOptions.RetryBaseSeconds)),
            section.GetValue<int>(nameof(SimulatorOptions.WebhookTimeoutSeconds)),
            section.GetValue<bool>(nameof(SimulatorOptions.EnableControlEndpoints)),
            section.GetValue<bool>(nameof(SimulatorOptions.SchedulersEnabled))
        );
    }
}
