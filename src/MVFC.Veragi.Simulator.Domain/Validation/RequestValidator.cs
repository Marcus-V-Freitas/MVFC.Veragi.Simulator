using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Common;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using FluentValidation;
using OperationResult;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Validation;

public sealed class RequestValidator(IServiceProvider? provider = null)
{
    private readonly IServiceProvider? _provider = provider;

    public Result Validate(MerchantCreateRequest request) =>
        Validate(request, GetValidator<MerchantCreateRequest>(() => new MerchantCreateRequestValidator()));

    public Result Validate(MerchantPatchRequest request) =>
        Validate(request, GetValidator<MerchantPatchRequest>(() => new MerchantPatchRequestValidator()));

    public Result Validate(Merchant request) =>
        Validate(request, GetValidator<Merchant>(() => new MerchantValidator()));

    public Result Validate(ReleaseAccountCreateRequest request) =>
        Validate(request, GetValidator<ReleaseAccountCreateRequest>(() => new ReleaseAccountCreateRequestValidator()));

    public Result Validate(ScheduleQueryRequest request) =>
        Validate(request, GetValidator<ScheduleQueryRequest>(() => new ScheduleQueryRequestValidator()));

    public Result Validate(ContractAnticipationCreateRequest request) =>
        Validate(request, GetValidator<ContractAnticipationCreateRequest>(() => new ContractAnticipationCreateRequestValidator()));

    public Result Validate(BankReconciliationEntry request) =>
        Validate(request, GetValidator<BankReconciliationEntry>(() => new BankReconciliationEntryValidator()));

    private IValidator<T> GetValidator<T>(Func<IValidator<T>> fallbackFactory) where T : class =>
        _provider is not null
            ? (IValidator<T>?)_provider.GetService(typeof(IValidator<T>)) ?? fallbackFactory()
            : fallbackFactory();

    public static Result Validate<T>(T request, IValidator<T> validator)
    {
        var result = validator.Validate(request);

        return result.IsValid ?
            Result.Success() :
            new SimulationFailureException(400, "VALIDATION_FAILED", "Invalid request payload", [.. result.Errors.Select(error => new ViolationItem(Code: "SCHEMA_VALIDATION", Name: error.PropertyName, Reason: error.ErrorMessage, Location: "body", Path: "$." + char.ToLowerInvariant(error.PropertyName[0]) + error.PropertyName[1..]))]);
    }
}
