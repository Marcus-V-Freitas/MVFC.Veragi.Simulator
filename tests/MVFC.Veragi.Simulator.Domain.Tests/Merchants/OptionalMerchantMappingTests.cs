using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Merchants;

public sealed class OptionalMerchantMappingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MerchantMappingPreservesProvidedAccountsAndOmittedOptionalAccounts(bool includeAccounts)
    {
        // Arrange
        var full = MockEntities.FullMerchantCreateRequest();
        var request = includeAccounts ? full : full with
        {
            ReleaseAccounts = null,
            CreditSettlementAccount = null,
            AnticipationSettlementAccount = null
        };

        // Act
        var merchant = request.ToMerchant();

        // Assert
        merchant.Cnpj.Should().Be(request.Cnpj);
        merchant.CorporateName.Should().Be(request.CorporateName);

        if (includeAccounts)
        {
            merchant.CreditSettlementAccount!.CnpjRecipient.Should().Be(full.CreditSettlementAccount!.CnpjRecipient);
            merchant.AnticipationSettlementAccount!.CnpjRecipient.Should().Be(full.AnticipationSettlementAccount!.CnpjRecipient);
            var account = merchant.ReleaseAccounts.Should().ContainSingle().Which;
            account.Account.Should().Be(full.ReleaseAccounts![0].Account);
            Guid.Parse(account.Id!).Version.Should().Be(7);
        }
        else
        {
            merchant.ReleaseAccounts.Should().BeNull();
            merchant.CreditSettlementAccount.Should().BeNull();
            merchant.AnticipationSettlementAccount.Should().BeNull();
        }
    }
}
