namespace MVFC.Veragi.Simulator.Shareable.Configuration;

public sealed record WorkerOptions(
    int QueueCapacity,
    int MaxEvents);
