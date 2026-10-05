using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class ReceivablesScheduleConfigValidator : AbstractValidator<ReceivablesScheduleConfig>
{
    public ReceivablesScheduleConfigValidator()
    {
        RuleFor(x => x.QueryWindow).NotNull().WithMessage("is required");
        RuleFor(x => x.QueryWindow).IsInEnum().WithMessage("unsupported value");
        RuleFor(x => x.AcquirerCnpjs).NotNull().WithMessage("is required");
        RuleFor(x => x.AcquirerCnpjs).Must(value => value is null || (value.Count >= 1)).WithMessage("invalid number of items");
        RuleForEach(x => x.AcquirerCnpjs).NotNull().WithMessage("must not be null");
        RuleForEach(x => x.AcquirerCnpjs).Must(value => value is null || InputFormats.IsCnpj(value)).WithMessage("invalid format");
        RuleFor(x => x.ArrangementCodes).NotNull().WithMessage("is required");
        RuleFor(x => x.ArrangementCodes).Must(value => value is null || (value.Count >= 1)).WithMessage("invalid number of items");
        RuleForEach(x => x.ArrangementCodes).NotNull().WithMessage("must not be null");
        RuleForEach(x => x.ArrangementCodes).Must(value => value is null || InputFormats.IsArrangement(value, allowWildcard: true)).WithMessage("invalid format");
    }
}
