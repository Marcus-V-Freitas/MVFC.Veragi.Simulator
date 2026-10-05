using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class MerchantCreateRequestValidator : AbstractValidator<MerchantCreateRequest>
{
    public MerchantCreateRequestValidator()
    {
        RuleFor(x => x.CorporateName).NotNull().WithMessage("is required");
        RuleFor(x => x.CorporateName).Must(value => value is null || (value.Length >= 2 && value.Length <= 100)).WithMessage("invalid string length");
        RuleFor(x => x.Cnpj).NotNull().WithMessage("is required");
        RuleFor(x => x.Cnpj).Must(value => value is null || InputFormats.IsCnpj(value)).WithMessage("invalid format");
        RuleFor(x => x.Email).Must(value => value is null || (value.Length <= 100)).WithMessage("invalid string length");
        RuleFor(x => x.Email).Must(value => value is null || System.Net.Mail.MailAddress.TryCreate(value, out _)).WithMessage("must be an email address");
        RuleFor(x => x.MobilePhone).Must(value => value is null || InputFormats.IsDigits(value, 11, 11)).WithMessage("invalid format");
        RuleFor(x => x.ReceivablesScheduleConfig).NotNull().WithMessage("is required");
        RuleFor(x => x.ReceivablesScheduleConfig).SetValidator(new ReceivablesScheduleConfigValidator()!);
        RuleForEach(x => x.CreditConfigurations).IsInEnum().WithMessage("unsupported value");
        RuleFor(x => x.CreditConfigurations).Must(value => value is null || (value.Count >= 1 && value.Count <= 3)).WithMessage("invalid number of items");
        RuleForEach(x => x.CreditConfigurations).NotNull().WithMessage("must not be null");
        RuleFor(x => x.LimitType).NotNull().WithMessage("is required");
        RuleFor(x => x.LimitType).IsInEnum().WithMessage("unsupported value");
        RuleFor(x => x.LimitAmount).Must(value => value is null || (value >= 0m)).WithMessage("outside allowed range");
        RuleFor(x => x.ReleaseAccounts).Must(value => value is null || (value.Count <= 3)).WithMessage("invalid number of items");
        RuleForEach(x => x.ReleaseAccounts).NotNull().WithMessage("must not be null").SetValidator(new ReleaseAccountCreateRequestValidator());
        RuleFor(x => x.CreditSettlementAccount).SetValidator(new SettlementAccountCreateRequestValidator()!);
        RuleFor(x => x.AnticipationSettlementAccount).SetValidator(new SettlementAccountCreateRequestValidator()!);
    }
}
