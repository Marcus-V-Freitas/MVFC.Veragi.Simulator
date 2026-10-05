using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MVFC.Veragi.Simulator.Domain.Services.Sales;
using MVFC.Veragi.Simulator.Domain.Services.Schedules;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Services.Reconciliation;
using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Behaviors;
using MVFC.Veragi.Simulator.Domain.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MVFC.Veragi.Simulator.Data;
using MVFC.Veragi.Simulator.IoC.Integrations;
using MongoDB.Driver;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using FluentValidation;
using MVFC.Veragi.Simulator.Domain;
using MVFC.Veragi.Simulator.Shareable;

namespace MVFC.Veragi.Simulator.IoC;

public static class DependencyInjection
{
    public static IServiceCollection AddSimulator(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton(_ => configuration.LoadSimulatorOptions());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<SimulationGate>();
        services.AddSingleton<DeliveryGate>();
        services.AddValidatorsFromAssemblyContaining<IDomainEntryPoint>();
        services.AddScoped<RequestValidator>();
        services.AddSingleton<CatalogService>();
        services.AddSingleton<BusinessCalendar>();
        services.AddSingleton<ContractRules>();
        services.AddSingleton<ScheduleGenerator>();
        services.AddSingleton<IMongoDatabase>(_ => new MongoClient(configuration.GetConnectionString("simulator")!).GetDatabase(configuration["Mongo:Database"]!));
        services.AddDbContext<SimulatorDbContext>((provider, db) =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            db.UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName);
        });
        services.AddScoped<ISimulatorStore, MongoSimulatorStore>();
        services.AddScoped<SalesService>();
        services.AddScoped<WebhookRoutingService>();
        services.AddScoped<ScenarioService>();
        services.AddScoped<EnvironmentService>();
        services.AddScoped<ExternalAnticipationService>();
        services.AddScoped<ContractRegistrationService>();
        services.AddScoped<ContractBalanceService>();
        services.AddScoped<ContractAvailabilityService>();
        services.AddScoped<MerchantService>();
        services.AddScoped<ScheduleService>();
        services.AddScoped<ContractService>();
        services.AddScoped<ReconciliationService>();
        services.AddScoped<ReconciliationAllocationService>();
        services.AddScoped<OperationProcessor>();
        services.AddScoped<WebhookDispatcher>();
        services.AddScoped<ControlService>();
        services.AddScoped<InspectionService>();
        services.AddScoped<SimulationProcessingService>();
        services.AddHttpClient<IWebhookSender, HttpWebhookSender>((provider, client) => client.Timeout = TimeSpan.FromSeconds(provider.GetRequiredService<SimulatorOptions>().WebhookTimeoutSeconds));
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(typeof(IDomainEntryPoint).Assembly, typeof(IShareableEntryPoint).Assembly);
            config.AddOpenBehavior(typeof(SimulationBehavior<,>));
        });

        return services;
    }
}
