using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Nodes;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Merchants.Accounts;

public sealed class AccountContractTests
{
    [Fact]
    public void ContractSettlementAccountPreservesDocumentedFieldNamesAndTextualType()
    {
        // Arrange
        var account = new SettlementAccountContract(MockEntities.MerchantCnpj, AccountType.CONTA_PAGAMENTO_PRE_PAGA, "341", "60701190", "1234", "555");
        var json = JsonNode.Parse(account.ToJson())!;

        // Act
        var roundTrip = json.ToJsonString().FromJson<SettlementAccountContract>();

        // Assert
        json.AsObject().Select(x => x.Key).Should().BeEquivalentTo("document", "accountType", "compe", "ispb", "branch", "account");
        json["accountType"]!.GetValue<string>().Should().Be("CONTA_PAGAMENTO_PRE_PAGA");
        json["document"]!.GetValue<string>().Should().Be(MockEntities.MerchantCnpj);
        roundTrip.Should().Be(account);
    }

    [Fact]
    public async Task SettlementAccountPatchReplacesBankDetailsWithoutChangingMerchantConfiguration()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var original = MockEntities.MerchantRequest();
        await fixture.MerchantService.CreateAsync(original, CancellationToken.None);
        var originalAccount = original.AnticipationSettlementAccount!;
        var accountPatch = new SettlementAccountPatchRequest(originalAccount.CnpjRecipient, originalAccount.CorporateNameRecipient, AccountType.CONTA_POUPANCA, originalAccount.BankCode, originalAccount.Ispb, originalAccount.Branch, "777");
        var request = new MerchantPatchRequest(AnticipationSettlementAccount: accountPatch);
        var restored = request.ToJson().FromJson<MerchantPatchRequest>()!;

        // Act
        var result = await fixture.MerchantService.PatchAsync(MockEntities.MerchantCnpj, restored, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var account = result.Value!.AnticipationSettlementAccount!;
        account.Account.Should().Be("777");
        account.AccountType.Should().Be(AccountType.CONTA_POUPANCA);
        account.Branch.Should().Be(original.AnticipationSettlementAccount!.Branch);
        account.BankCode.Should().Be(original.AnticipationSettlementAccount.BankCode);
        account.CnpjRecipient.Should().Be(original.AnticipationSettlementAccount.CnpjRecipient);
        result.Value.ReceivablesScheduleConfig.Should().BeEquivalentTo(original.ReceivablesScheduleConfig);
        var response = new MerchantPatchResponse(Data: result.Value).ToJson().FromJson<MerchantPatchResponse>()!;
        response.Data!.AnticipationSettlementAccount.Should().BeEquivalentTo(account);
    }

    [Theory]
    [InlineData(typeof(AccountType))]
    [InlineData(typeof(ContractSituationType))]
    [InlineData(typeof(OperationType))]
    [InlineData(typeof(QueryWindowType))]
    [InlineData(typeof(ReleaseAccountType))]
    [InlineData(typeof(ScheduleQueryOriginType))]
    [InlineData(typeof(ScheduleQueryStatusType))]
    [InlineData(typeof(ScheduleQueryType))]
    [InlineData(typeof(ScheduleWebhookSchema))]
    public void TextualEnumsRejectNumbersAndUnknownTextWhilePreservingEverySupportedValue(Type type)
    {
        // Arrange
        Action numeric = () => JsonSerializer.Deserialize("0", type, JsonExtensions.Options);
        Action numericText = () => JsonSerializer.Deserialize("\"0\"", type, JsonExtensions.Options);

        // Act
        Action unsupported = () => JsonSerializer.Deserialize("\"UNSUPPORTED\"", type, JsonExtensions.Options);

        // Assert
        numeric.Should().Throw<JsonException>();
        numericText.Should().Throw<JsonException>();
        unsupported.Should().Throw<JsonException>();

        foreach (var value in Enum.GetValues(type))
        {
            var json = JsonSerializer.Serialize(value, type, JsonExtensions.Options);
            json.Should().Be("\"" + value + "\"");
            JsonSerializer.Deserialize(json, type, JsonExtensions.Options).Should().Be(value);
        }
    }
}
