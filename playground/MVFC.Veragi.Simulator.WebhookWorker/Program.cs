using Microsoft.AspNetCore.Mvc;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.WebhookWorker;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "worker-appsettings.json"), optional: true)
                     .AddEnvironmentVariables()
                     .AddCommandLine(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapPost("/webhooks/card-receivables/schedules/updated", (
    [FromBody] ScheduleWebhookNotification notification,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    HttpContext http,
    ILogger<Program> logger) =>
{
    logger.LogSchedulesWebhook(idempotencyKey, notification.DadosConsultaAgenda?.IdRequisicao, notification.Status?.ToString(), http.TraceIdentifier);

    return Results.Ok();
});

app.MapPost("/webhooks/card-receivable/schedules", (
    [FromBody] ContractWebhookNotification notification,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    HttpContext http,
    ILogger<Program> logger) =>
{
    logger.LogContractsWebhook(idempotencyKey, notification.ScheduleQueryData?.RequestId, notification.Status.ToString(), http.TraceIdentifier);

    return Results.Ok();
});

await app.RunAsync();
