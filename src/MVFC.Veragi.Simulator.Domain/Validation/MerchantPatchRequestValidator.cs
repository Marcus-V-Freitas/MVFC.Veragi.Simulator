using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class MerchantPatchRequestValidator : AbstractValidator<MerchantPatchRequest>
{
    public MerchantPatchRequestValidator()
    {
        RuleFor(x => x.CorporateName).Must(value => value is null || (value.Length >= 2 && value.Length <= 100)).WithMessage("invalid string length");
        RuleFor(x => x.Email).Must(value => value is null || (value.Length <= 100)).WithMessage("invalid string length");
        RuleFor(x => x.MobilePhone).Must(value => value is null || InputFormats.IsDigits(value, 11, 11)).WithMessage("invalid format");
        RuleFor(x => x.ReceivablesScheduleConfig).SetValidator(new ReceivablesScheduleConfigValidator()!);
        RuleForEach(x => x.CreditConfigurations).IsInEnum().WithMessage("unsupported value");
        RuleFor(x => x.CreditConfigurations).Must(value => value is null || (value.Count >= 1 && value.Count <= 3)).WithMessage("invalid number of items");
        RuleForEach(x => x.CreditConfigurations).NotNull().WithMessage("must not be null");
        RuleFor(x => x.LimitType).IsInEnum().WithMessage("unsupported value");
        RuleFor(x => x.LimitAmount).Must(value => value is null || (value >= 0m)).WithMessage("outside allowed range");
        RuleForEach(x => x.ReleaseAccounts).NotNull().WithMessage("must not be null").SetValidator(new ReleaseAccountPatchRequestValidator());
        RuleFor(x => x.CreditSettlementAccount).SetValidator(new SettlementAccountPatchRequestValidator()!);
        RuleFor(x => x.AnticipationSettlementAccount).SetValidator(new SettlementAccountPatchRequestValidator()!);
    }
}
