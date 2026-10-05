using Bogus;
using System.Globalization;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

namespace MVFC.Veragi.Simulator.TestHelpers;

public static partial class MockEntities
{
    public static BankReconciliationEntry ReconciliationRequest(DateOnly today, decimal value = 100) =>
        new Faker<BankReconciliationEntry>().UseSeed(42)
            .CustomInstantiator(_ => new BankReconciliationEntry(
                EntryId: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(),
                ReferenceDate: today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Merchant: MerchantCnpj, Acquirer: AcquirerCnpj, BankAccount: "555", Value: value))
            .Generate();

    public static ScheduledOperationEntity ReconciliationContract(
        ContractByExternalReference details,
        string reference,
        DateTime createdAt) => new Faker<ScheduledOperationEntity>().UseSeed(42)
            .CustomInstantiator(_ => new ScheduledOperationEntity
            {
                Kind = "contract",
                MerchantCnpj = MerchantCnpj,
                SettlementBankAccountCode = "555",
                Status = ScheduleQueryStatusType.PROCESSED,
                CreatedAt = createdAt,
                ExternalReference = reference,
                ResultJson = details.ToJson()
            }).Generate();
}
