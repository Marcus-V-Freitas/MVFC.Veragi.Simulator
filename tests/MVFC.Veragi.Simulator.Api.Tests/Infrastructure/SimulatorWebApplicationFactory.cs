using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MVFC.Veragi.Simulator.Api;

namespace MVFC.Veragi.Simulator.Api.Tests.Infrastructure;

internal sealed class SimulatorWebApplicationFactory(string connectionString, string databaseName) : WebApplicationFactory<IApiEntryPoint>
{
    private readonly string _connectionString = connectionString;
    private readonly string _databaseName = databaseName;

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:simulator"] = _connectionString,
        ["Mongo:Database"] = _databaseName,
        ["Simulator:SchedulersEnabled"] = "false",
        ["Simulator:ScheduleWebhook:Url"] = "http://127.0.0.1:1/webhooks/schedule",
        ["Simulator:ContractWebhook:Url"] = "http://127.0.0.1:1/webhooks/contract",
        ["Simulator:WebhookTimeoutSeconds"] = "1",
        ["Simulator:RetryBaseSeconds"] = "1",
        ["Simulator:MaxDeliveryAttempts"] = "2"
    }));
}
