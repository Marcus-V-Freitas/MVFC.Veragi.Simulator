using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using AutoBogus;
using Bogus;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;

namespace MVFC.Veragi.Simulator.TestHelpers;

public static partial class MockEntities
{
    public const string MerchantCnpj = "22185894000174";
    public const string AcquirerCnpj = "82413081000116";
    public const string ArrangementCode = "VCC";

    public static SimulatorOptions Options() => 
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "api-appsettings.json"))
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "api-appsettings.Development.json"), optional: true)
            .Build()
            .LoadSimulatorOptions();

    public static MerchantCreateRequest MerchantRequest() => new Faker<MerchantCreateRequest>("pt_BR").UseSeed(42).CustomInstantiator(faker => new MerchantCreateRequest(CorporateName: faker.Company.CompanyName(), Cnpj: MerchantCnpj, Email: faker.Internet.Email(), MobilePhone: "11987654321", ReceivablesScheduleConfig: new ReceivablesScheduleConfig(QueryWindowType.P6M, [AcquirerCnpj], [ArrangementCode]), CreditConfigurations: [CreditConfigurationType.OccasionalAnticipation], LimitType: LimitType.Transactional, AnticipationSettlementAccount: new SettlementAccountCreateRequest(CnpjRecipient: MerchantCnpj, CorporateNameRecipient: "Estabelecimento", AccountType: AccountType.CONTA_DEPOSITO_A_VISTA, Account: "555", Branch: "1234", BankCode: "341", Ispb: "60701190"))).Generate();

    public static Merchant Merchant() => MerchantRequest().ToMerchant();

    public static ScheduledOperationEntity Operation(
        string kind,
        string cnpj,
        DateTime dueAt,
        ScheduleQueryStatusType status = ScheduleQueryStatusType.PROCESSING
    ) => new AutoFaker<ScheduledOperationEntity>().UseSeed(42).RuleFor(x => x.Id, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow)).RuleFor(x => x.Kind, kind).RuleFor(x => x.MerchantCnpj, cnpj).RuleFor(x => x.DueAt, dueAt).RuleFor(x => x.CreatedAt, dueAt).RuleFor(x => x.Status, status).RuleFor(x => x.Outcome, ScheduleQueryStatusType.PROCESSED).RuleFor(x => x.RequestJson, "{}").RuleFor(x => x.ResultJson, "{}").Generate();

    public static WebhookDeliveryEntity Delivery(
        Guid operationId,
        DateTime nextAttemptAt,
        bool delivered = false,
        bool deadLetter = false
    ) => new AutoFaker<WebhookDeliveryEntity>().UseSeed(42).RuleFor(x => x.Id, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow)).RuleFor(x => x.OperationId, operationId).RuleFor(x => x.Kind, "schedule").RuleFor(x => x.MerchantCnpj, MerchantCnpj).RuleFor(x => x.Payload, "{}").RuleFor(x => x.PayloadKey, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString()).RuleFor(x => x.Attempts, 0).RuleFor(x => x.Delivered, delivered).RuleFor(x => x.DeadLetter, deadLetter).RuleFor(x => x.NextAttemptAt, nextAttemptAt).RuleFor(x => x.LastHttpStatus, _ => null).Generate();

    public static ReconciliationEntity Entry(
        string cnpj,
        Guid entryId
    ) => new AutoFaker<ReconciliationEntity>().UseSeed(42).RuleFor(x => x.Id, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow)).RuleFor(x => x.EntryId, entryId).RuleFor(x => x.MerchantCnpj, cnpj).RuleFor(x => x.AllocationsJson, "[]").RuleFor(x => x.UnallocatedAmount, 0m).RuleFor(x => x.Payload, "{}").Generate();

    public static WebhookReceiptEntity Receipt(
        string eventKey,
        Guid requestId,
        DateTime receivedAt
    ) => new AutoFaker<WebhookReceiptEntity>().UseSeed(42).RuleFor(x => x.Id, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow)).RuleFor(x => x.Kind, "schedule").RuleFor(x => x.EventKey, eventKey).RuleFor(x => x.MerchantCnpj, MerchantCnpj).RuleFor(x => x.RequestId, requestId.ToString()).RuleFor(x => x.Payload, "{}").RuleFor(x => x.ReceivedAt, receivedAt).Generate();

    public static MerchantEntity MerchantEntity() => new AutoFaker<MerchantEntity>().UseSeed(42).RuleFor(x => x.Id, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow)).RuleFor(x => x.Cnpj, MerchantCnpj).RuleFor(x => x.Payload, _ => Merchant().ToJson()).RuleFor(x => x.IsDeleted, false).Generate();

    public static ContractAnticipationCreateRequest Contract(
        DateOnly signature,
        DateOnly settlement,
        decimal amount = 100m
    ) => new(ContractorCnpj: MerchantCnpj, RequestedAmount: amount, SignatureDate: signature.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Guarantees: [new ContractAnticipationGuaranteeCreate(DefinedAmount: amount, FinalUserReceiverCnpj: MerchantCnpj, ReceivableUnitHolderCnpj: MerchantCnpj, AcquirerCnpj: AcquirerCnpj, PaymentArrangementCode: ArrangementCode, SettlementDate: settlement.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))]);

    public static SimulatedSaleEntity Sale(
        string date,
        decimal amount = 100m
    ) => new AutoFaker<SimulatedSaleEntity>().UseSeed(42).RuleFor(x => x.Id, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow)).RuleFor(x => x.SaleId, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow)).RuleFor(x => x.BatchId, _ => Guid.CreateVersion7(DateTimeOffset.UtcNow)).RuleFor(x => x.MerchantCnpj, MerchantCnpj).RuleFor(x => x.AcquirerCnpj, AcquirerCnpj).RuleFor(x => x.PaymentArrangementCode, ArrangementCode).RuleFor(x => x.SettlementDate, date).RuleFor(x => x.Amount, amount).RuleFor(x => x.InstallmentNumber, 1).Generate();
}
