using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class ContractMappings
{
    public static ContractByContractorAndSituation ToSummary(this ScheduledOperationEntity operation)
    {
        var request = operation.RequestJson.FromJson<ContractAnticipationCreateRequest>()!;
        var details = operation.ResultJson.FromJson<ContractByExternalReference>()!;

        return new ContractByContractorAndSituation(ExternalReference: operation.ExternalReference, FinancierContractId: request.FinancierContractId ?? operation.ExternalReference, Status: details.Status, ContractorCnpj: request.ContractorCnpj, EffectType: EffectType.OwnershipTransfer, SignatureDate: request.SignatureDate, DueDate: request.Guarantees!.Max(x => x.SettlementDate), GuaranteedLimitAmount: request.RequestedAmount, ReachedAmount: details.ReachedTotal(), UpdatedAmount: details.ReachedTotal());
    }

    public static decimal ReachedTotal(this ContractByExternalReference details) => (details.ReachedGuarantees ?? []).SelectMany(x => x.Acquirers ?? []).SelectMany(x => x.PaymentArrangements ?? []).SelectMany(x => x.ReceivableUnits ?? []).Sum(x => x.ReachedAmount ?? 0);

    public static ContractWebhookNotification ToContractWebhook(
        this ScheduledOperationEntity operation,
        DateTime now
    )
    {
        var details = operation.ResultJson.FromJson<ContractByExternalReference>()!;
        ContractWebhookAcquirer[]? acquirers = operation.Status == ScheduleQueryStatusType.ERROR ? null : [.. (details.ReachedGuarantees ?? []).SelectMany(x => x.Acquirers ?? []).GroupBy(x => x.Cnpj).Select(acquirer => new ContractWebhookAcquirer(acquirer.Key!, [.. acquirer.SelectMany(x => x.PaymentArrangements ?? []).GroupBy(x => x.Code).Select(arrangement => new ContractWebhookArrangement(arrangement.Key!, [.. arrangement.SelectMany(x => x.ReceivableUnits ?? []).GroupBy(x => x.SettlementDate).Select(unit => new ContractWebhookUnit(unit.Key!, unit.Sum(x => x.ReachedAmount ?? 0), 0))]))]))];

        return new ContractWebhookNotification(operation.Status, "Contract anticipated schedule; reached amounts, contract status " + details.Status, new ContractWebhookData(operation.Id.ToString(), ScheduleQueryOriginType.ONLINE, now.ToString("O"), operation.MerchantCnpj, acquirers));
    }

    public static ContractByExternalReference ToDetails(
        this ContractAnticipationCreateRequest request,
        string reference,
        ContractStatusType status,
        IReadOnlyDictionary<string,
        decimal>? reached = null
    ) => new()
    {
        FinancierContractId = request.FinancierContractId ?? reference,
        ContractorCnpj = request.ContractorCnpj,
        Status = status,
        EffectType = EffectType.OwnershipTransfer,
        SignatureDate = request.SignatureDate,
        DebtBalanceAmount = status == ContractStatusType.Active ? reached?.Values.Sum() ?? request.RequestedAmount : 0,
        GuaranteedLimitAmount = request.RequestedAmount,
        MinimumBalanceAmount = request.RequestedAmount,
        ReachedGuarantees = [.. request.Guarantees!.GroupBy(x => x.ReceivableUnitHolderCnpj).Select(holder => new ContractByExternalReferenceGuarantee(ReceivableUnitHolderCnpj: holder.Key, Acquirers: [.. holder.GroupBy(x => x.AcquirerCnpj).Select(acquirer => new ContractByExternalReferenceAcquirer(Cnpj: acquirer.Key, PaymentArrangements: [.. acquirer.GroupBy(x => x.PaymentArrangementCode).Select(arrangement => new ContractByExternalReferencePaymentArrangement(Code: arrangement.Key, ReceivableUnits: [.. arrangement.GroupBy(ContractService.UnitKey).Select(group => new ContractByExternalReferenceReceivableUnit(SettlementDate: group.First().SettlementDate, DivisionRule: DivisionRuleType.FixedValue, RequestedAmount: group.Sum(x => x.DefinedAmount), ReachedAmount: status is ContractStatusType.Active or ContractStatusType.Settled ? reached is null ? group.Sum(x => x.DefinedAmount) : reached.GetValueOrDefault(group.Key) : 0))]))]))]))],
    };
}
