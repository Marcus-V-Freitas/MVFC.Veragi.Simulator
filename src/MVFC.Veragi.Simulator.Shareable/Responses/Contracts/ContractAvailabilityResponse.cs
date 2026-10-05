using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

public sealed record ContractAvailabilityResponse(ContractAvailabilityMode Mode, bool Persisted);
