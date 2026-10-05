using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Contracts;

public sealed class ContractAvailabilityService(
    ISimulatorStore store,
    SimulatorOptions options,
    SimulationGate gate
)
{
    private readonly ISimulatorStore _store = store;
    private readonly SimulatorOptions _options = options;
    private readonly SimulationGate _gate = gate;

    public async Task<Result<ContractAvailabilityResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var configuration = await _store.GetContractAvailabilityAsync(cancellationToken);

        return new ContractAvailabilityResponse(configuration?.Mode ?? _options.ContractAvailabilityMode, configuration is not null);
    }

    public async Task<ContractAvailabilityMode> GetModeAsync(CancellationToken cancellationToken) => (await _store.GetContractAvailabilityAsync(cancellationToken))?.Mode ?? _options.ContractAvailabilityMode;

    public async Task<Result<ContractAvailabilityResponse>> ConfigureAsync(
        ContractAvailabilityRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Enum.IsDefined(request.Mode))
            return Failures.Validation("Unknown contract availability mode", "mode");

        using var lease = await _gate.EnterAsync(cancellationToken);
        var configuration = await _store.GetContractAvailabilityAsync(cancellationToken);

        if (configuration is null)
        {
            configuration = new ContractAvailabilityConfigurationEntity();
            _store.AddContractAvailability(configuration);
        }

        configuration.Mode = request.Mode;
        await _store.SaveAsync(cancellationToken);

        return new ContractAvailabilityResponse(configuration.Mode, Persisted: true);
    }
}
