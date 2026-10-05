using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class ContractAnticipationCreateRequestValidator : AbstractValidator<ContractAnticipationCreateRequest>
{
    public ContractAnticipationCreateRequestValidator()
    {
        RuleFor(x => x.FinancierContractId).Must(value => value is null || (value.Length >= 1 && value.Length <= 45)).WithMessage("invalid string length");
        RuleFor(x => x.ContractorCnpj).NotNull().WithMessage("is required");
        RuleFor(x => x.ContractorCnpj).Must(value => value is null || InputFormats.IsCnpj(value)).WithMessage("invalid format");
        RuleFor(x => x.Wallet).Must(value => value is null || (value.Length >= 1 && value.Length <= 256)).WithMessage("invalid string length");
        RuleFor(x => x.SignatureDate).NotNull().WithMessage("is required");
        RuleFor(x => x.SignatureDate).Must(value => value is null || InputFormats.IsDate(value)).WithMessage("must be an ISO date");
        RuleFor(x => x.RequestedAmount).NotNull().WithMessage("is required");
        RuleFor(x => x.RequestedAmount).Must(value => value is null || (value >= 0.01m)).WithMessage("outside allowed range");
        RuleFor(x => x.Guarantees).NotNull().WithMessage("is required");
        RuleFor(x => x.Guarantees).Must(value => value is null || (value.Count >= 1)).WithMessage("invalid number of items");
        RuleForEach(x => x.Guarantees).NotNull().WithMessage("must not be null").SetValidator(new ContractAnticipationGuaranteeCreateValidator());
        RuleFor(x => x.SettlementBankAccount).SetValidator(new SettlementAccountContractValidator()!);
    }
}
