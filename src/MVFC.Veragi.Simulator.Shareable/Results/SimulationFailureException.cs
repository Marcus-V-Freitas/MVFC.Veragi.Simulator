using MVFC.Veragi.Simulator.Shareable.Responses.Common;

namespace MVFC.Veragi.Simulator.Shareable.Results;

public sealed class SimulationFailureException(
    int statusCode,
    string code,
    string message,
    IReadOnlyList<ViolationItem>? violations = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string Code { get; } = code;

    public IReadOnlyList<ViolationItem> Violations { get; } = violations ?? [];
}
