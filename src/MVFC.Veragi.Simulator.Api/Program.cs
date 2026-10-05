using MVFC.Veragi.Simulator.Api.Errors;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using MVFC.Veragi.Simulator.Api.Endpoints;
using MVFC.Veragi.Simulator.Api.Hosting;
using MVFC.Veragi.Simulator.IoC;
using MVFC.Veragi.Simulator.Data;
using MVFC.Veragi.Simulator.Shareable.Configuration;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.Services.AddSimulator(builder.Configuration);
builder.Services.AddExceptionHandler<SimulatorExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
foreach (var kind in new[]
{
    "schedule",
    "contract",
    "delivery",
}

)
{
    builder.Services.AddSingleton<IHostedService>(services => new SimulatorScheduler(kind, services.GetRequiredService<IServiceScopeFactory>(), services.GetRequiredService<SimulatorOptions>(), services.GetRequiredService<ILogger<SimulatorScheduler>>()));
}

var app = builder.Build();
app.UseExceptionHandler();
app.MapProviderEndpoints();
app.MapSimulationEndpoints();
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<SimulatorDbContext>().Database.EnsureCreatedAsync();
}

await app.RunAsync();
