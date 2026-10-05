using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using FluentValidation;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class BankReconciliationEntryValidator : AbstractValidator<BankReconciliationEntry>
{
    public BankReconciliationEntryValidator()
    {
        RuleFor(x => x.CreatedDate).Must(value => value is null || InputFormats.IsTimestamp(value)).WithMessage("must be a timestamp");
        RuleFor(x => x.LastModifiedDate).Must(value => value is null || InputFormats.IsTimestamp(value)).WithMessage("must be a timestamp");
        RuleFor(x => x.EntryId).NotNull().WithMessage("is required");
        RuleFor(x => x.EntryId).Must(value => value is null || Guid.TryParse(value, out _)).WithMessage("must be a UUID");
        RuleFor(x => x.ReferenceDate).NotNull().WithMessage("is required");
        RuleFor(x => x.ReferenceDate).Must(value => value is null || InputFormats.IsDate(value)).WithMessage("must be an ISO date");
        RuleFor(x => x.Merchant).NotNull().WithMessage("is required");
        RuleFor(x => x.Acquirer).NotNull().WithMessage("is required");
        RuleFor(x => x.BankAccount).NotNull().WithMessage("is required");
        RuleFor(x => x.BankAccount).Must(value => value is null || (value.Length <= 40)).WithMessage("invalid string length");
        RuleFor(x => x.Value).NotNull().WithMessage("is required");
    }
}
