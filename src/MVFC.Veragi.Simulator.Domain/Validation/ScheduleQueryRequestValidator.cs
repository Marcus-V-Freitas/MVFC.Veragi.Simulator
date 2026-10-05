using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class ScheduleQueryRequestValidator : AbstractValidator<ScheduleQueryRequest>
{
    public ScheduleQueryRequestValidator()
    {
        RuleFor(x => x.MerchantCnpj).NotNull().WithMessage("is required");
        RuleFor(x => x.MerchantCnpj).Must(value => value is null || InputFormats.IsCnpj(value)).WithMessage("invalid format");
        RuleFor(x => x.QueryType).NotNull().WithMessage("is required");
        RuleFor(x => x.QueryType).IsInEnum().WithMessage("unsupported value");
    }
}
