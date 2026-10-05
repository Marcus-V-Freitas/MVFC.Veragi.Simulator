using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class ReleaseAccountPatchRequestValidator : AbstractValidator<ReleaseAccountPatchRequest>
{
    public ReleaseAccountPatchRequestValidator()
    {
        RuleFor(x => x.OperationType).NotNull().WithMessage("is required");
        RuleFor(x => x.OperationType).IsInEnum().WithMessage("unsupported value");
        RuleFor(x => x.Id).Must(value => value is null || Guid.TryParse(value, out _)).WithMessage("must be a UUID");
        RuleFor(x => x.ExternalId).Must(value => value is null || (value.Length <= 40)).WithMessage("invalid string length");
        RuleFor(x => x.AccountType).IsInEnum().WithMessage("unsupported value");
        RuleFor(x => x.BankCode).Must(value => value is null || InputFormats.IsDigits(value, 3, 3)).WithMessage("invalid format");
        RuleFor(x => x.Ispb).Must(value => value is null || InputFormats.IsDigits(value, 8, 8)).WithMessage("invalid format");
        RuleFor(x => x.Branch).Must(value => value is null || InputFormats.IsDigits(value, 4, 4)).WithMessage("invalid format");
        RuleFor(x => x.Account).Must(value => value is null || InputFormats.IsDigits(value, 1, 20)).WithMessage("invalid format");
    }
}
