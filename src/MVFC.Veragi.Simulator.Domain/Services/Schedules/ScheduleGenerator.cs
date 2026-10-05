using System.Globalization;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Domain.Services.Receivables;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;

namespace MVFC.Veragi.Simulator.Domain.Services.Schedules;

public sealed class ScheduleGenerator(BusinessCalendar calendar)
{
    private readonly BusinessCalendar _calendar = calendar;

    public Schedule Generate(ScheduledOperationEntity operation, Merchant merchant, IReadOnlyList<ScheduledOperationEntity> contracts, IReadOnlyList<SimulatedSaleEntity> sales, DateTime now, IReadOnlyList<ExternalAnticipationEntity>? external = null)
    {
        var config = merchant.ReceivablesScheduleConfig!;
        DateOnly horizon;

        if (config.QueryWindow == QueryWindowType.P1Y)
        {
            horizon = _calendar.Today.AddMonths(12);
        }
        else
        {
            horizon = _calendar.Today.AddMonths(config.QueryWindow == QueryWindowType.P6M ? 6 : 24);
        }

        var reservations = ReceivableBalances.Commitments(contracts, includePending: true);

        foreach (var item in external ?? [])
            ReceivableBalances.Add(reservations, ReceivableBalances.Key(item.MerchantCnpj, item.AcquirerCnpj, item.PaymentArrangementCode, item.SettlementDate), item.Amount);

        var scoped = sales.Where(x => x.MerchantCnpj == merchant.Cnpj &&
                                      DateOnly.ParseExact(x.SettlementDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) >= _calendar.Today &&
                                      DateOnly.ParseExact(x.SettlementDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) <= horizon &&
                                      (config.AcquirerCnpjs!.Contains("99999999000199") || config.AcquirerCnpjs.Contains(x.AcquirerCnpj)) &&
                                      (config.ArrangementCodes!.Contains("999") || config.ArrangementCodes.Contains(x.PaymentArrangementCode)));

        return new Schedule(RequestId: operation.Id.ToString(), OriginType: ScheduleQueryOriginType.ONLINE, UpdatedAt: now.ToString("O"), MerchantCnpj: merchant.Cnpj, Acquirers: operation.Outcome == ScheduleQueryStatusType.ERROR ? null : [.. scoped.GroupBy(x => x.AcquirerCnpj).OrderBy(x => x.Key).Select(acquirer => new SchedulePaymentAcquirer(Cnpj: acquirer.Key, PaymentArrangements: [.. acquirer.GroupBy(x => x.PaymentArrangementCode).OrderBy(x => x.Key).Select(arrangement => new SchedulePaymentArrangement(Code: arrangement.Key, ReceivableUnits: [.. arrangement.GroupBy(x => x.SettlementDate).OrderBy(x => x.Key).Select(date =>
        {
            var total = date.Sum(x => x.Amount);
            var key = string.Join('|', merchant.Cnpj, acquirer.Key, arrangement.Key, date.Key);

            return new ScheduleReceivableUnit(SettlementDate: date.Key, TotalAmount: total, FreeAmount: Math.Max(0, total - reservations.GetValueOrDefault(key)), HolderCnpj: merchant.Cnpj);
        })]))]))]);
    }
}
