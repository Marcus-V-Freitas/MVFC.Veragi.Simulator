using Bogus;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;

namespace MVFC.Veragi.Simulator.TestHelpers;

public static partial class MockEntities
{
    public static ReleaseAccount FullReleaseAccount() => new Faker<ReleaseAccount>().UseSeed(42).CustomInstantiator(_ => new ReleaseAccount(Id: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(), AccountType: ReleaseAccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Ispb: "60701190", Branch: "1234", Account: "123456", ExternalId: "Valid value")).Generate();

    public static ScheduleQueryRequest FullScheduleQueryRequest() => new Faker<ScheduleQueryRequest>().UseSeed(42).CustomInstantiator(_ => new ScheduleQueryRequest(MerchantCnpj: MerchantCnpj, QueryType: ScheduleQueryType.STANDARD)).Generate();

    public static SettlementAccount FullSettlementAccount() => new Faker<SettlementAccount>().UseSeed(42).CustomInstantiator(_ => new SettlementAccount(CnpjRecipient: MerchantCnpj, CorporateNameRecipient: "Valid value", AccountType: AccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Ispb: "60701190", Branch: "1234", Account: "123456")).Generate();

    public static SettlementAccountCreateRequest FullSettlementAccountCreateRequest() => new Faker<SettlementAccountCreateRequest>().UseSeed(42).CustomInstantiator(_ => new SettlementAccountCreateRequest(CnpjRecipient: MerchantCnpj, CorporateNameRecipient: "Valid value", AccountType: AccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Ispb: "60701190", Branch: "1234", Account: "123456")).Generate();

    public static ContractAnticipationCreateRequest FullContractAnticipationCreateRequest() => new Faker<ContractAnticipationCreateRequest>().UseSeed(42).CustomInstantiator(_ => new ContractAnticipationCreateRequest(FinancierContractId: "Valid value", ContractorCnpj: MerchantCnpj, Wallet: "Valid value", SignatureDate: "2026-10-09", RequestedAmount: 0.01m, Guarantees: [FullContractAnticipationGuaranteeCreate()], SettlementBankAccount: FullSettlementAccountContract())).Generate();

    public static BankReconciliationEntry FullBankReconciliationEntry() => new Faker<BankReconciliationEntry>().UseSeed(42).CustomInstantiator(_ => new BankReconciliationEntry(CreatedBy: "Valid value", CreatedDate: "2026-10-02T15:00:00Z", LastModifiedBy: "Valid value", LastModifiedDate: "2026-10-02T15:00:00Z", Id: 1, EntryId: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(), ReferenceDate: "2026-10-09", Merchant: MerchantCnpj, Acquirer: MerchantCnpj, BankAccount: "Valid value", Value: 1m)).Generate();

    public static SettlementAccountPatchRequest FullSettlementAccountPatchRequest() => new Faker<SettlementAccountPatchRequest>().UseSeed(42).CustomInstantiator(_ => new SettlementAccountPatchRequest(CnpjRecipient: MerchantCnpj, CorporateNameRecipient: "Valid value", AccountType: AccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Ispb: "60701190", Branch: "1234", Account: "123456")).Generate();

    public static ReceivablesScheduleConfig FullReceivablesScheduleConfig() => new Faker<ReceivablesScheduleConfig>().UseSeed(42).CustomInstantiator(_ => new ReceivablesScheduleConfig(QueryWindow: QueryWindowType.P6M, AcquirerCnpjs: [MerchantCnpj], ArrangementCodes: ["VCC"])).Generate();

    public static ReleaseAccountCreateRequest FullReleaseAccountCreateRequest() => new Faker<ReleaseAccountCreateRequest>().UseSeed(42).CustomInstantiator(_ => new ReleaseAccountCreateRequest(AccountType: ReleaseAccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Ispb: "60701190", Branch: "1234", Account: "123456", ExternalId: "Valid value")).Generate();

    public static SettlementAccountContract FullSettlementAccountContract() => new Faker<SettlementAccountContract>().UseSeed(42).CustomInstantiator(_ => new SettlementAccountContract(Document: MerchantCnpj, AccountType: AccountType.CONTA_DEPOSITO_A_VISTA, Compe: "341", Ispb: "60701190", Branch: "1234", Account: "123456")).Generate();

    public static MerchantPatchRequest FullMerchantPatchRequest() => new Faker<MerchantPatchRequest>().UseSeed(42).CustomInstantiator(_ => new MerchantPatchRequest(CorporateName: "Valid value", Email: "Valid value", MobilePhone: "11999999999", ReceivablesScheduleConfig: FullReceivablesScheduleConfig(), CreditConfigurations: [CreditConfigurationType.CreditSecuredWorkingCapital], LimitType: LimitType.Transactional, LimitAmount: 0m, ReleaseAccounts: [FullReleaseAccountPatchRequest()], CreditSettlementAccount: FullSettlementAccountPatchRequest(), AnticipationSettlementAccount: FullSettlementAccountPatchRequest())).Generate();

    public static Merchant FullMerchant() => new Faker<Merchant>().UseSeed(42).CustomInstantiator(_ => new Merchant(CorporateName: "Valid value", Cnpj: MerchantCnpj, Email: "valid@example.com", MobilePhone: "11999999999", ReceivablesScheduleConfig: FullReceivablesScheduleConfig(), CreditConfigurations: [CreditConfigurationType.CreditSecuredWorkingCapital], LimitType: LimitType.Transactional, LimitAmount: 0m, ReleaseAccounts: [FullReleaseAccount()], CreditSettlementAccount: FullSettlementAccount(), AnticipationSettlementAccount: FullSettlementAccount())).Generate();

    public static ReleaseAccountPatchRequest FullReleaseAccountPatchRequest() => new Faker<ReleaseAccountPatchRequest>().UseSeed(42).CustomInstantiator(_ => new ReleaseAccountPatchRequest(OperationType: OperationType.ADD, Id: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(), ExternalId: "Valid value", AccountType: ReleaseAccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Ispb: "60701190", Branch: "1234", Account: "123456")).Generate();

    public static MerchantCreateRequest FullMerchantCreateRequest() => new Faker<MerchantCreateRequest>().UseSeed(42).CustomInstantiator(_ => new MerchantCreateRequest(CorporateName: "Valid value", Cnpj: MerchantCnpj, Email: "valid@example.com", MobilePhone: "11999999999", ReceivablesScheduleConfig: FullReceivablesScheduleConfig(), CreditConfigurations: [CreditConfigurationType.CreditSecuredWorkingCapital], LimitType: LimitType.Transactional, LimitAmount: 0m, ReleaseAccounts: [FullReleaseAccountCreateRequest()], CreditSettlementAccount: FullSettlementAccountCreateRequest(), AnticipationSettlementAccount: FullSettlementAccountCreateRequest())).Generate();

    public static ContractAnticipationGuaranteeCreate FullContractAnticipationGuaranteeCreate() => new Faker<ContractAnticipationGuaranteeCreate>().UseSeed(42).CustomInstantiator(_ => new ContractAnticipationGuaranteeCreate(ReceivableUnitHolderCnpj: MerchantCnpj, FinalUserReceiverCnpj: MerchantCnpj, AcquirerCnpj: MerchantCnpj, PaymentArrangementCode: "VCC", SettlementDate: "2026-10-09", DefinedAmount: 0.01m)).Generate();
}
