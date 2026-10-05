namespace MVFC.Veragi.Simulator.Shareable.Responses.Common;

public sealed record ApiResponse<T>(
    string Message,
    DateTime Timestamp,
    string TraceId,
    T Data,
    ApiMetadataResponse Metadata
);
