using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MVFC.Veragi.Simulator.Domain.Services.Reconciliation;
using MVFC.Veragi.Simulator.Domain.Services.Sales;
using MVFC.Veragi.Simulator.Domain.Services.Schedules;
using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Domain.Services.Webhooks;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.TestHelpers;

namespace MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

public sealed class ServiceFixture : IDisposable
{
    public ISimulatorStore Store { get; } = Substitute.For<ISimulatorStore>();

    public SimulationGate Gate { get; } = new();

    public DeliveryGate DeliveryGate { get; } = new();

    public TimeProvider Clock { get; } = Substitute.For<TimeProvider>();

    private SimulatorOptions _options = MockEntities.Options();

    public SimulatorOptions Options
    {
        get => _options;
        set
        {
            _options = value;
            InitializeServices();
        }
    }

    public List<MerchantEntity> Merchants { get; } = [];

    public List<ScheduledOperationEntity> Operations { get; } = [];

    public List<SimulatedSaleEntity> Sales { get; } = [];

    public List<SalesBatchEntity> Batches { get; } = [];

    public List<SimulationScenarioEntity> Scenarios { get; } = [];

    public List<WebhookDeliveryEntity> Deliveries { get; } = [];

    public List<WebhookReceiptEntity> Receipts { get; } = [];

    public List<ReconciliationEntity> Entries { get; } = [];

    public List<ExternalAnticipationEntity> ExternalAnticipations { get; } = [];

    public List<WebhookRoutingEntity> Routing { get; } = [];

    public List<ContractAvailabilityConfigurationEntity> AvailabilityConfigurations { get; } = [];

    public BusinessCalendar Calendar { get; private set; } = null!;

    public MerchantService MerchantService { get; }

    public SalesService SalesService { get; private set; } = null!;

    public ScheduleService ScheduleService { get; private set; } = null!;

    public ContractService ContractService { get; private set; } = null!;

    public ScenarioService ScenarioService { get; }

    public ReconciliationService ReconciliationService { get; private set; } = null!;

    public OperationProcessor Processor { get; private set; } = null!;

    public WebhookRoutingService RoutingService { get; private set; } = null!;

    public ControlService ControlService { get; }

    public ServiceFixture(SimulatorOptions? options = null)
    {
        _options = options ?? MockEntities.Options();
        Clock.GetUtcNow().Returns(new DateTimeOffset(2026, 10, 2, 15, 0, 0, TimeSpan.Zero));
        var validator = new RequestValidator();
        MerchantService = new(Store, validator, Gate);
        ScenarioService = new(Store, Gate);
        ControlService = new(Store, Gate, DeliveryGate, Clock);
        InitializeServices();
        Store.GetMerchantAsync(Arg.Is<string>(x => x != null), CancellationToken.None).Returns(call => Merchants.SingleOrDefault(x => x.Cnpj == call.ArgAt<string>(0)));
        Store.GetOperationAsync(Arg.Is<Guid>(x => x != Guid.Empty), CancellationToken.None).Returns(call => Operations.SingleOrDefault(x => x.Id == call.ArgAt<Guid>(0)));
        Store.GetOperationsAsync(Arg.Is<string>(x => x != null), Arg.Is<string?>(x => x == null || CatalogService.IsCnpj(x)), CancellationToken.None).Returns(call => [.. Operations.Where(x => x.Kind == call.ArgAt<string>(0) && (call.ArgAt<string?>(1) is null || x.MerchantCnpj == call.ArgAt<string?>(1)))]);
        Store.GetDueOperationsAsync(Arg.Is<string>(x => x != null), Arg.Is<DateTime>(x => x > DateTime.MinValue), CancellationToken.None).Returns(call => [.. Operations.Where(x => x.Kind == call.ArgAt<string>(0) && x.Status == ScheduleQueryStatusType.PROCESSING && x.DueAt <= call.ArgAt<DateTime>(1))]);
        Store.GetSalesAsync(Arg.Is<string>(x => x != null), CancellationToken.None).Returns(call => [.. Sales.Where(x => x.MerchantCnpj == call.ArgAt<string>(0))]);
        Store.GetSalesBatchAsync(Arg.Is<string>(x => x != null), CancellationToken.None).Returns(call => Batches.SingleOrDefault(x => x.BatchKey == call.ArgAt<string>(0)));
        Store.GetScenarioAsync(Arg.Is<string>(x => x != null), CancellationToken.None).Returns(call => Scenarios.SingleOrDefault(x => x.ScopeKey == call.ArgAt<string>(0)));
        Store.GetScenariosAsync(CancellationToken.None).Returns(_ => [.. Scenarios]);
        Store.GetPendingDeliveriesAsync(Arg.Is<DateTime>(x => x > DateTime.MinValue), CancellationToken.None).Returns(call => [.. Deliveries.Where(x => !x.Delivered && !x.DeadLetter && x.NextAttemptAt <= call.ArgAt<DateTime>(0))]);
        Store.GetDeliveryAsync(Arg.Is<string>(x => x != null), CancellationToken.None).Returns(call => Deliveries.SingleOrDefault(x => x.Id.ToString() == call.ArgAt<string>(0)));
        Store.GetDeliveryByPayloadKeyAsync(Arg.Is<string>(x => x != null), CancellationToken.None).Returns(call => Deliveries.SingleOrDefault(x => x.PayloadKey == call.ArgAt<string>(0)));
        Store.GetReceiptAsync(Arg.Is<string>(x => x != null), CancellationToken.None).Returns(call => Receipts.SingleOrDefault(x => x.EventKey == call.ArgAt<string>(0)));
        Store.GetEntryAsync(Arg.Is<Guid>(x => x != Guid.Empty), CancellationToken.None).Returns(call => Entries.SingleOrDefault(x => x.EntryId == call.ArgAt<Guid>(0)));
        Store.GetEntriesAsync(Arg.Is<string?>(x => x == null || CatalogService.IsCnpj(x)), CancellationToken.None).Returns(call => [.. Entries.Where(x => call.ArgAt<string?>(0) is null || x.MerchantCnpj == call.ArgAt<string?>(0))]);
        Store.GetExternalAnticipationsAsync(Arg.Is<string>(x => x != null), CancellationToken.None).Returns(call => [.. ExternalAnticipations.Where(x => x.MerchantCnpj == call.ArgAt<string>(0))]);
        Store.GetWebhookRoutingAsync(CancellationToken.None).Returns(_ => Routing.SingleOrDefault());
        Store.GetContractAvailabilityAsync(CancellationToken.None).Returns(_ => AvailabilityConfigurations.SingleOrDefault());
        Store.GetDeliveriesAsync(CancellationToken.None).Returns(_ => [.. Deliveries]);
        Store.GetReceiptsAsync(CancellationToken.None).Returns(_ => [.. Receipts]);
        Store.SaveAsync(CancellationToken.None).Returns(Task.CompletedTask);
        Store.When(x => x.AddMerchant(Arg.Is<MerchantEntity>(v => v != null))).Do(call => Merchants.Add(call.Arg<MerchantEntity>()));
        Store.When(x => x.AddOperation(Arg.Is<ScheduledOperationEntity>(v => v != null))).Do(call => Operations.Add(call.Arg<ScheduledOperationEntity>()));
        Store.When(x => x.AddSale(Arg.Is<SimulatedSaleEntity>(v => v != null))).Do(call => Sales.Add(call.Arg<SimulatedSaleEntity>()));
        Store.When(x => x.AddSalesBatch(Arg.Is<SalesBatchEntity>(v => v != null))).Do(call => Batches.Add(call.Arg<SalesBatchEntity>()));
        Store.When(x => x.AddScenario(Arg.Is<SimulationScenarioEntity>(v => v != null))).Do(call => Scenarios.Add(call.Arg<SimulationScenarioEntity>()));
        Store.When(x => x.RemoveScenario(Arg.Is<SimulationScenarioEntity>(v => v != null))).Do(call => Scenarios.Remove(call.Arg<SimulationScenarioEntity>()));
        Store.When(x => x.AddDelivery(Arg.Is<WebhookDeliveryEntity>(v => v != null))).Do(call => Deliveries.Add(call.Arg<WebhookDeliveryEntity>()));
        Store.When(x => x.AddReceipt(Arg.Is<WebhookReceiptEntity>(v => v != null))).Do(call => Receipts.Add(call.Arg<WebhookReceiptEntity>()));
        Store.When(x => x.AddEntry(Arg.Is<ReconciliationEntity>(v => v != null))).Do(call => Entries.Add(call.Arg<ReconciliationEntity>()));
        Store.When(x => x.AddExternalAnticipation(Arg.Is<ExternalAnticipationEntity>(v => v != null))).Do(call => ExternalAnticipations.Add(call.Arg<ExternalAnticipationEntity>()));
        Store.When(x => x.AddContractAvailability(Arg.Is<ContractAvailabilityConfigurationEntity>(value => value != null))).Do(call => AvailabilityConfigurations.Add(call.Arg<ContractAvailabilityConfigurationEntity>()));
        Store.When(x => x.AddWebhookRouting(Arg.Is<WebhookRoutingEntity>(v => v != null))).Do(call => Routing.Add(call.Arg<WebhookRoutingEntity>()));
    }

    private void InitializeServices()
    {
        var validator = new RequestValidator();
        var catalog = new CatalogService();
        Calendar = new BusinessCalendar(Clock, _options);
        SalesService = new(Store, Gate, catalog, Calendar, _options, Clock);
        ScheduleService = new(Store, validator, Gate, _options, Clock);
        ContractService = new(Store, validator, Gate, new ContractRules(Calendar, _options), _options, Clock, new ContractAvailabilityService(Store, _options, Gate), new ContractBalanceService(Store));
        ReconciliationService = new(Store, validator, Gate, Calendar, new ReconciliationAllocationService(Store));
        RoutingService = new(Store, Gate, DeliveryGate);
        Processor = new(Store, Gate, new ScheduleGenerator(Calendar), Clock, _options, ScenarioService, new ContractRegistrationService(new ContractBalanceService(Store)));
    }

    public async Task<Guid> PrepareAsync(bool generateSales = true)
    {
        await MerchantService.CreateAsync(MockEntities.MerchantRequest(), CancellationToken.None);

        if (generateSales)
            await SalesService.GenerateAsync(MockEntities.MerchantCnpj, new GenerateSalesRequest(Days: 1, SalesPerDay: 1, AmountPerSale: 1000), Key(), CancellationToken.None);

        var schedule = await ScheduleService.SubmitAsync(new ScheduleQueryRequest(MerchantCnpj: MockEntities.MerchantCnpj, QueryType: ScheduleQueryType.STANDARD), Key(), CancellationToken.None);
        await Processor.ProcessAsync("schedule", true, CancellationToken.None);

        return Guid.Parse(schedule.Value!.RequestId!);
    }

    public ContractAnticipationCreateRequest Contract(decimal amount = 100) => MockEntities.Contract(Calendar.Today, Calendar.AddBusinessDays(Calendar.Today, Options.MinimumBusinessDays), amount);

    public static string Key() => Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();

    public void Dispose() => Gate.Dispose();
}
