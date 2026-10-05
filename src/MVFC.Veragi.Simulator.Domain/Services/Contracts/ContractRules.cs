using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using OperationResult;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Results;
using System.Globalization;

namespace MVFC.Veragi.Simulator.Domain.Services.Contracts;

public sealed class ContractRules(BusinessCalendar calendar, SimulatorOptions options)
{
    private readonly BusinessCalendar _calendar = calendar;
    private readonly SimulatorOptions _options = options;

    public Result Validate(ContractAnticipationCreateRequest request)
    {
        if (request.Guarantees!.Sum(x => x.DefinedAmount) != request.RequestedAmount)
            return Failures.Validation("Sum of definedAmount must equal requestedAmount", "guarantees");

        if (request.Guarantees!.Exists(x => x.FinalUserReceiverCnpj![..8] != request.ContractorCnpj![..8]))
            return Failures.Validation("Contractor root must match final user root", "guarantees");

        var minimum = _calendar.AddBusinessDays(_calendar.Today, _options.MinimumBusinessDays);

        if (request.Guarantees!.Exists(x => DateOnly.ParseExact(x.SettlementDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture) < minimum))
            return Failures.Validation("Settlement date must respect minimum business days", "guarantees");

        if (request.Guarantees!.Exists(x => DateOnly.ParseExact(x.SettlementDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture) < DateOnly.ParseExact(request.SignatureDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture)))
            return Failures.Validation("Settlement date must not precede signatureDate", "guarantees");

        if (request.RequestedAmount != decimal.Round(request.RequestedAmount!.Value, 2, MidpointRounding.ToEven) || request.Guarantees!.Exists(x => x.DefinedAmount != decimal.Round(x.DefinedAmount!.Value, 2, MidpointRounding.ToEven)))
            return Failures.Validation("Amounts must have at most two decimal places");

        return Result.Success();
    }
}
