using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class ReleaseAccountValidator : AbstractValidator<ReleaseAccount>
{
    public ReleaseAccountValidator()
    {
        RuleFor(x => x.Id).NotNull().WithMessage("is required");
        RuleFor(x => x.Id).Must(value => value is null || Guid.TryParse(value, out _)).WithMessage("must be a UUID");
        RuleFor(x => x.AccountType).NotNull().WithMessage("is required");
        RuleFor(x => x.AccountType).IsInEnum().WithMessage("unsupported value");
        RuleFor(x => x.BankCode).NotNull().WithMessage("is required");
        RuleFor(x => x.BankCode).Must(value => value is null || InputFormats.IsDigits(value, 3, 3)).WithMessage("invalid format");
        RuleFor(x => x.Ispb).NotNull().WithMessage("is required");
        RuleFor(x => x.Ispb).Must(value => value is null || InputFormats.IsDigits(value, 8, 8)).WithMessage("invalid format");
        RuleFor(x => x.Branch).NotNull().WithMessage("is required");
        RuleFor(x => x.Branch).Must(value => value is null || InputFormats.IsDigits(value, 4, 4)).WithMessage("invalid format");
        RuleFor(x => x.Account).NotNull().WithMessage("is required");
        RuleFor(x => x.Account).Must(value => value is null || InputFormats.IsDigits(value, 1, 20)).WithMessage("invalid format");
        RuleFor(x => x.ExternalId).Must(value => value is null || (value.Length <= 40)).WithMessage("invalid string length");
    }
}
