namespace MVFC.Veragi.Simulator.Domain.Entities;

public sealed class SimulationScenarioEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public string ScopeKey { get; set; } = "";

    public string Target { get; set; } = "";

    public string? MerchantCnpj { get; set; }

    public bool? HoldProcessing { get; set; }

    public int Calls { get; set; }

    public int FailuresRemaining { get; set; }

    public int FailureStatusCode { get; set; } = 503;

    public string ProcessingOutcomesJson { get; set; } = "[]";

    public string ContractStatusesJson { get; set; } = "[]";
}
