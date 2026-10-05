using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class SettlementAccountContractValidator : AbstractValidator<SettlementAccountContract>
{
    public SettlementAccountContractValidator()
    {
        RuleFor(x => x.Document).NotNull().WithMessage("is required");
        RuleFor(x => x.Document).Must(value => value is null || InputFormats.IsCnpj(value)).WithMessage("invalid format");
        RuleFor(x => x.AccountType).NotNull().WithMessage("is required");
        RuleFor(x => x.AccountType).Must(value => value is null or AccountType.CONTA_DEPOSITO_A_VISTA or AccountType.CONTA_PAGAMENTO_PRE_PAGA or AccountType.CONTA_POUPANCA).WithMessage("unsupported value");
        RuleFor(x => x.Compe).Must(value => value is null || InputFormats.IsDigits(value, 3, 3)).WithMessage("invalid format");
        RuleFor(x => x.Ispb).NotNull().WithMessage("is required");
        RuleFor(x => x.Ispb).Must(value => value is null || InputFormats.IsDigits(value, 8, 8)).WithMessage("invalid format");
        RuleFor(x => x.Branch).NotNull().WithMessage("is required");
        RuleFor(x => x.Branch).Must(value => value is null || InputFormats.IsAlphaNumeric(value, 1, 4)).WithMessage("invalid format");
        RuleFor(x => x.Account).NotNull().WithMessage("is required");
        RuleFor(x => x.Account).Must(value => value is null || InputFormats.IsAlphaNumeric(value, 1, 20)).WithMessage("invalid format");
    }
}
