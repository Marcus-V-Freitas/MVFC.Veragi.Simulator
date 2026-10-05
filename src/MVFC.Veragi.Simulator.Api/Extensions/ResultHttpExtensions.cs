using MVFC.Veragi.Simulator.Shareable.Responses.Common;
using OperationResult;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Api.Extensions;

public static class ResultHttpExtensions
{
    public static IResult ToHttp<T>(
        this Result<T> result,
        HttpContext context,
        int status = 200,
        bool noBody = false
    )
    {
        if (!result.IsSuccess)
            return Error(result.Exception!, context);

        if (noBody)
            return Results.StatusCode(status);

        var traceId = EnsureTraceId(context);
        var response = new ApiResponse<T>("Request processed", DateTime.UtcNow, traceId, result.Value, new ApiMetadataResponse("v1", 0));

        return Results.Json(response, statusCode: status);
    }

    public static IResult Error(Exception error, HttpContext context)
    {
        var failure = error as SimulationFailureException;
        var status = failure?.StatusCode ?? 500;
        var title = status switch
        {
            400 => "Validation Error",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            _ => "Internal Server Error",
        };
        var traceId = EnsureTraceId(context);
        var problem = new ProblemDetails(Type: "https://veragi.accesstage.com.br/problems/" + title.ToLowerInvariant().Replace(' ', '-'), Title: title, Status: status, Detail: failure?.Message ?? "Unexpected internal error", Instance: context.Request.Path, Timestamp: DateTime.UtcNow.ToString("O"), TraceId: traceId, ErrorCode: failure?.Code ?? "INTERNAL_ERROR", Violations: failure?.Violations.ToList());

        return Results.Json(problem, contentType: "application/problem+json", statusCode: status);
    }

    public static string EnsureTraceId(HttpContext context)
    {
        if (Guid.TryParse(context.TraceIdentifier, out var guid))
        {
            var formatted = guid.ToString();
            context.TraceIdentifier = formatted;
            return formatted;
        }

        var newGuid = Guid.NewGuid().ToString();
        context.TraceIdentifier = newGuid;
        return newGuid;
    }
}
