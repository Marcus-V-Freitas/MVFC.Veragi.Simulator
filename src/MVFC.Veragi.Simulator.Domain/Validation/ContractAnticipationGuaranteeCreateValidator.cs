using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class ContractAnticipationGuaranteeCreateValidator : AbstractValidator<ContractAnticipationGuaranteeCreate>
{
    public ContractAnticipationGuaranteeCreateValidator()
    {
        RuleFor(x => x.ReceivableUnitHolderCnpj).NotNull().WithMessage("is required");
        RuleFor(x => x.ReceivableUnitHolderCnpj).Must(value => value is null || InputFormats.IsCnpj(value)).WithMessage("invalid format");
        RuleFor(x => x.FinalUserReceiverCnpj).NotNull().WithMessage("is required");
        RuleFor(x => x.FinalUserReceiverCnpj).Must(value => value is null || InputFormats.IsCnpj(value)).WithMessage("invalid format");
        RuleFor(x => x.AcquirerCnpj).NotNull().WithMessage("is required");
        RuleFor(x => x.AcquirerCnpj).Must(value => value is null || InputFormats.IsCnpj(value)).WithMessage("invalid format");
        RuleFor(x => x.PaymentArrangementCode).NotNull().WithMessage("is required");
        RuleFor(x => x.PaymentArrangementCode).Must(value => value is null || InputFormats.IsArrangement(value, allowWildcard: true)).WithMessage("invalid format");
        RuleFor(x => x.SettlementDate).NotNull().WithMessage("is required");
        RuleFor(x => x.SettlementDate).Must(value => value is null || InputFormats.IsDate(value)).WithMessage("must be an ISO date");
        RuleFor(x => x.DefinedAmount).NotNull().WithMessage("is required");
        RuleFor(x => x.DefinedAmount).Must(value => value is null || (value >= 0.01m)).WithMessage("outside allowed range");
    }
}
