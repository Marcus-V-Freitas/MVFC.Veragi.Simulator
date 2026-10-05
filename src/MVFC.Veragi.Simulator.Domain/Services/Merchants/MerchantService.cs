using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Domain.Validation;
using OperationResult;
using System.Text.Json.Nodes;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Merchants;

public sealed class MerchantService(ISimulatorStore store, RequestValidator validator, SimulationGate gate)
{
    private readonly ISimulatorStore _store = store;
    private readonly RequestValidator _validator = validator;
    private readonly SimulationGate _gate = gate;

    public async Task<Result<Merchant>> CreateAsync(
        MerchantCreateRequest request,
        CancellationToken cancellationToken
    )
    {
        using var lease = await _gate.EnterAsync(cancellationToken);
        var valid = _validator.Validate(request);

        if (!valid.IsSuccess)
            return valid.Exception!;

        var merchant = request.ToMerchant();
        var business = ValidateMerchant(merchant);

        if (!business.IsSuccess)
            return business.Exception!;

        if (await _store.GetMerchantAsync(request.Cnpj!, cancellationToken) is not null)
            return Failures.Conflict("CNPJ already registered");

        _store.AddMerchant(new MerchantEntity { Cnpj = merchant.Cnpj!, Payload = merchant.ToJson() });
        await _store.SaveAsync(cancellationToken);

        return merchant;
    }

    public async Task<Result<Merchant>> GetAsync(string cnpj, CancellationToken cancellationToken)
    {
        if (!CatalogService.IsCnpj(cnpj))
            return Failures.Validation("Invalid CNPJ", "cnpj");

        var state = await _store.GetMerchantAsync(cnpj, cancellationToken);

        return state?.IsDeleted != false ? Failures.Missing("Merchant not found") : Result.Success(state.Payload.FromJson<Merchant>()!);
    }

    public async Task<Result<bool>> DeleteAsync(string cnpj, CancellationToken cancellationToken)
    {
        using var lease = await _gate.EnterAsync(cancellationToken);

        if (!CatalogService.IsCnpj(cnpj))
            return Failures.Validation("Invalid CNPJ", "cnpj");

        var state = await _store.GetMerchantAsync(cnpj, cancellationToken);

        if (state is null)
            return Failures.Missing("Merchant not found");

        state.IsDeleted = true;
        await _store.SaveAsync(cancellationToken);

        return true;
    }

    public async Task<Result<Merchant>> PatchAsync(
        string cnpj,
        MerchantPatchRequest request,
        CancellationToken cancellationToken
    )
    {
        using var lease = await _gate.EnterAsync(cancellationToken);

        if (!CatalogService.IsCnpj(cnpj))
            return Failures.Validation("Invalid CNPJ", "cnpj");

        var valid = _validator.Validate(request);

        if (!valid.IsSuccess)
            return valid.Exception!;

        var changes = JsonNode.Parse(request.ToJson())!.AsObject();

        if (changes.Count == 0)
            return Failures.Validation("At least one field is required");

        var state = await _store.GetMerchantAsync(cnpj, cancellationToken);

        if (state?.IsDeleted != false)
            return Failures.Missing("Merchant not found");

        var merged = JsonNode.Parse(state.Payload)!.AsObject();

        foreach (var change in changes.Where(x => x.Key != "releaseAccounts"))
            merged[change.Key] = change.Value?.DeepClone();

        if (request.LimitType is LimitType.Transactional or LimitType.NoLimit && request.LimitAmount is null)
            merged.Remove("limitAmount");

        var accounts = merged["releaseAccounts"]?.AsArray() ?? [];
        var accountChanges = ApplyReleaseAccountChanges(accounts, request);

        if (!accountChanges.IsSuccess)
            return accountChanges.Exception!;

        merged["releaseAccounts"] = accounts.DeepClone();
        var merchant = merged.ToJsonString().FromJson<Merchant>()!;
        var mergedValidation = _validator.Validate(merchant);

        if (!mergedValidation.IsSuccess)
            return mergedValidation.Exception!;

        var business = ValidateMerchant(merchant);

        if (!business.IsSuccess)
            return business.Exception!;

        state.Payload = merchant.ToJson();
        await _store.SaveAsync(cancellationToken);

        return merchant;
    }

    private Result ApplyReleaseAccountChanges(JsonArray accounts, MerchantPatchRequest request)
    {
        foreach (var operation in request.ReleaseAccounts ?? [])
        {
            var index = accounts.Select((node, i) => (node, i)).Where(x => x.node?["id"]?.GetValue<string>() == operation.Id).Select(x => x.i).DefaultIfEmpty(-1).First();

            if (operation.OperationType == OperationType.ADD)
            {
                if (operation.Id is not null)
                    return Failures.Validation("ADD must omit id", "releaseAccounts");

                var account = operation.ToJson().FromJson<ReleaseAccountCreateRequest>()!;
                var accountValidation = _validator.Validate(account);

                if (!accountValidation.IsSuccess)
                    return accountValidation.Exception!;

                accounts.Add(JsonNode.Parse(account.ToReleaseAccount().ToJson()));
            }
            else
            {
                if (operation.Id is null || index < 0)
                    return Failures.Validation("Account id does not exist", "releaseAccounts");

                if (operation.OperationType == OperationType.REMOVE)
                {
                    accounts.RemoveAt(index);
                }
                else
                {
                    foreach (var field in JsonNode.Parse(operation.ToJson())!.AsObject().Where(x => x.Key is not "id" and not "operationType"))
                        accounts[index]![field.Key] = field.Value?.DeepClone();
                }
            }
        }

        return Result.Success();
    }

    public static Result ValidateMerchant(Merchant merchant)
    {
        var limitValidation = ValidateLimit(merchant);

        if (!limitValidation.IsSuccess)
            return limitValidation;

        var creditConfigurationValidation = ValidateCreditConfigurations(merchant);

        if (!creditConfigurationValidation.IsSuccess)
            return creditConfigurationValidation;

        var scheduleConfigValidation = ValidateReceivablesScheduleConfig(merchant);

        if (!scheduleConfigValidation.IsSuccess)
            return scheduleConfigValidation;

        return ValidateReleaseAccounts(merchant);
    }

    private static Result ValidateLimit(Merchant merchant)
    {
        if (merchant.LimitType == LimitType.Global && merchant.LimitAmount is null)
            return Failures.Validation("limitAmount required for GLOBAL", "limitAmount");

        if (merchant.LimitType != LimitType.Global && merchant.LimitAmount is not null)
            return Failures.Validation("limitAmount only allowed for GLOBAL", "limitAmount");

        return Result.Success();
    }

    private static Result ValidateCreditConfigurations(Merchant merchant)
    {
        if (merchant.CreditConfigurations?.Exists(x => x is < CreditConfigurationType.CreditSecuredWorkingCapital or > CreditConfigurationType.AutomaticAnticipation) == true)
            return Failures.Validation("Invalid credit configuration", "creditConfigurations");

        return Result.Success();
    }

    private static Result ValidateReceivablesScheduleConfig(Merchant merchant)
    {
        var config = merchant.ReceivablesScheduleConfig!;

        if (!ExclusiveUnique(config.AcquirerCnpjs!, "99999999000199") || !ExclusiveUnique(config.ArrangementCodes!, "999"))
            return Failures.Validation("Wildcard must be exclusive and items unique", "receivablesScheduleConfig");

        return Result.Success();
    }

    private static Result ValidateReleaseAccounts(Merchant merchant)
    {
        var accounts = merchant.ReleaseAccounts ?? [];

        if (accounts.Count > 1 && accounts.Exists(x => string.IsNullOrWhiteSpace(x.ExternalId)))
            return Failures.Validation("externalId required for multiple accounts", "releaseAccounts");

        var externalIds = accounts.Where(x => x.ExternalId is not null).Select(x => x.ExternalId);

        if (externalIds.Distinct().Count() != externalIds.Count())
            return Failures.Validation("externalId must be unique", "releaseAccounts");

        if (accounts.Select(x => (x.AccountType, x.BankCode, x.Branch, x.Account)).Distinct().Count() != accounts.Count)
            return Failures.Validation("Account tuple must be unique", "releaseAccounts");

        return Result.Success();
    }

    private static bool ExclusiveUnique(List<string> values, string wildcard) => values.Count == values.Distinct(StringComparer.Ordinal).Count() && (!values.Contains(wildcard) || values.Count == 1);
}
