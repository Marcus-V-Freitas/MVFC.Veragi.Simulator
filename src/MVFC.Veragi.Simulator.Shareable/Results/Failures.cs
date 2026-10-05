using MVFC.Veragi.Simulator.Shareable.Responses.Common;

namespace MVFC.Veragi.Simulator.Shareable.Results;

public static class Failures
{
    public static SimulationFailureException Validation(
        string message,
        string field = "body") =>
        new(400, "VALIDATION_FAILED", message, [new ViolationItem(Code: "VALIDATION_ERROR", Name: field, Reason: message, Location: "body", Path: "$." + field)]);

    public static SimulationFailureException Missing(string message) =>
        new(404, "NOT_FOUND", message);

    public static SimulationFailureException Conflict(string message) =>
        new(409, "CONFLICT", message);

    public static SimulationFailureException Unprocessable(string message) =>
        new(422, "UNPROCESSABLE_ENTITY", message);
}
