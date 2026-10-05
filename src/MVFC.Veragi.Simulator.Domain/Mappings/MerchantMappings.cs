using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Extensions;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class MerchantMappings
{
    public static Merchant ToMerchant(this MerchantCreateRequest request) => new()
    {
        Cnpj = request.Cnpj,
        CorporateName = request.CorporateName,
        Email = request.Email,
        MobilePhone = request.MobilePhone,
        ReceivablesScheduleConfig = request.ReceivablesScheduleConfig,
        CreditConfigurations = request.CreditConfigurations,
        LimitType = request.LimitType,
        LimitAmount = request.LimitAmount,
        ReleaseAccounts = request.ReleaseAccounts?.Select(ToReleaseAccount).ToList(),
        CreditSettlementAccount = request.CreditSettlementAccount?.ToJson().FromJson<SettlementAccount>(),
        AnticipationSettlementAccount = request.AnticipationSettlementAccount?.ToJson().FromJson<SettlementAccount>(),
    };

    public static ReleaseAccount ToReleaseAccount(this ReleaseAccountCreateRequest request) => new()
    {
        Id = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(),
        AccountType = request.AccountType,
        Account = request.Account,
        Branch = request.Branch,
        BankCode = request.BankCode,
        Ispb = request.Ispb,
        ExternalId = request.ExternalId,
    };
}
