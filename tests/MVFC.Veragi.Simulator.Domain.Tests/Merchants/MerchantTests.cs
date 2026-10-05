using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Merchants;

public sealed class MerchantTests
{
    [Theory]
    [InlineData("invalid")]
    [InlineData("missing")]
    [InlineData("deleted")]
    public async Task MerchantLookupReturnsExpectedFailure(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();

        if (failure == "deleted")
            fixture.Merchants.Add(new MerchantEntity { Cnpj = MockEntities.MerchantCnpj, IsDeleted = true });

        // Act
        var result = await fixture.MerchantService.GetAsync(failure == "invalid" ? "invalid" : MockEntities.MerchantCnpj, CancellationToken.None);

        // Assert
        ((SimulationFailureException)result.Exception!).StatusCode.Should().Be(failure == "invalid" ? 400 : 404);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("missing")]
    public async Task InvalidDeletionDoesNotWrite(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();

        // Act
        var result = await fixture.MerchantService.DeleteAsync(failure == "invalid" ? "invalid" : MockEntities.MerchantCnpj, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("wildcard")]
    [InlineData("duplicate")]
    public async Task InvalidOrDuplicateRegistrationDoesNotCreateMerchant(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var request = MockEntities.MerchantRequest();

        if (failure == "schema")
            request = request with
            {
                Cnpj = null
            };

        if (failure == "wildcard")
            request = request with
            {
                ReceivablesScheduleConfig = request.ReceivablesScheduleConfig! with
                {
                    ArrangementCodes = ["999", "VCC"]
                }
            };

        if (failure == "duplicate")
            fixture.Merchants.Add(MockEntities.MerchantEntity());

        // Act
        var result = await fixture.MerchantService.CreateAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Store.DidNotReceive().AddMerchant(Arg.Any<MerchantEntity>());
    }

    [Theory]
    [InlineData("credit")]
    [InlineData("external-required")]
    [InlineData("external-duplicate")]
    [InlineData("arrangement-duplicate")]
    [InlineData("account-duplicate")]
    public void MerchantRejectsInconsistentConfiguration(string failure)
    {
        // Arrange
        var merchant = MockEntities.Merchant();
        var account = new ReleaseAccount(AccountType: ReleaseAccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Branch: "1234", Account: "9999", ExternalId: "one");
        merchant = failure switch
        {
            "credit" => merchant with
            {
                CreditConfigurations = [(CreditConfigurationType)99]
            },
            "external-required" => merchant with
            {
                ReleaseAccounts = [account, account with
                {
                    ExternalId = null,
                    Account = "8888"
                }

                ]
            },
            "external-duplicate" => merchant with
            {
                ReleaseAccounts = [account, account with
                {
                    Account = "8888"
                }

                ]
            },
            "arrangement-duplicate" => merchant with
            {
                ReceivablesScheduleConfig = merchant.ReceivablesScheduleConfig! with
                {
                    ArrangementCodes = ["VCC", "VCC"]
                }
            },
            _ => merchant with
            {
                ReleaseAccounts = [account, account with
                {
                    ExternalId = "two"
                }

                ]
            }
        };

        // Act
        var result = MerchantService.ValidateMerchant(merchant);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AccountCanBeAddedModifiedAndRemovedAndLimitAmountIsClearedWhenLimitChanges()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        fixture.Merchants.Add(MockEntities.MerchantEntity());
        var add = new ReleaseAccountPatchRequest(OperationType: OperationType.ADD, AccountType: ReleaseAccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Ispb: "60701190", Branch: "1234", Account: "9999", ExternalId: "release-1");
        var added = await fixture.MerchantService.PatchAsync(MockEntities.MerchantCnpj, new MerchantPatchRequest(LimitType: LimitType.Global, LimitAmount: 1000, ReleaseAccounts: [add]), CancellationToken.None);
        var account = added.Value!.ReleaseAccounts!.Single();
        var modified = await fixture.MerchantService.PatchAsync(MockEntities.MerchantCnpj, new MerchantPatchRequest(LimitType: LimitType.NoLimit, ReleaseAccounts: [new ReleaseAccountPatchRequest(Id: account.Id, OperationType: OperationType.MODIFY, Account: "8888")]), CancellationToken.None);

        // Act
        var removed = await fixture.MerchantService.PatchAsync(MockEntities.MerchantCnpj, new MerchantPatchRequest(ReleaseAccounts: [new ReleaseAccountPatchRequest(Id: account.Id, OperationType: OperationType.REMOVE)]), CancellationToken.None);

        // Assert
        Guid.Parse(account.Id!).Version.Should().Be(7);
        modified.Value!.LimitAmount.Should().BeNull();
        modified.Value.ReleaseAccounts!.Single().Account.Should().Be("8888");
        removed.Value!.ReleaseAccounts.Should().BeEmpty();
    }

    [Theory]
    [InlineData("cnpj")]
    [InlineData("schema")]
    [InlineData("empty")]
    [InlineData("missing")]
    [InlineData("deleted")]
    [InlineData("add-id")]
    [InlineData("add-schema")]
    [InlineData("modify-id")]
    [InlineData("remove-id")]
    [InlineData("merged-schema")]
    [InlineData("merged-business")]
    public async Task RejectedPatchKeepsPersistedPayload(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var entity = MockEntities.MerchantEntity();
        fixture.Merchants.Add(entity);
        var original = entity.Payload;
        var cnpj = MockEntities.MerchantCnpj;
        var request = new MerchantPatchRequest(CorporateName: "Updated name");
        switch (failure)
        {
            case "cnpj":
                cnpj = "invalid";
                break;
            case "schema":
                request = request with
                {
                    CorporateName = ""
                };
                break;
            case "empty":
                request = new MerchantPatchRequest();
                break;
            case "missing":
                fixture.Merchants.Clear();
                break;
            case "deleted":
                entity.IsDeleted = true;
                break;
            case "add-id":
                request = request with
                {
                    ReleaseAccounts = [new ReleaseAccountPatchRequest(Id: ServiceFixture.Key(), OperationType: OperationType.ADD)]
                };
                break;
            case "add-schema":
                request = request with
                {
                    ReleaseAccounts = [new ReleaseAccountPatchRequest(OperationType: OperationType.ADD)]
                };
                break;
            case "modify-id":
                request = request with
                {
                    ReleaseAccounts = [new ReleaseAccountPatchRequest(OperationType: OperationType.MODIFY)]
                };
                break;
            case "remove-id":
                request = request with
                {
                    ReleaseAccounts = [new ReleaseAccountPatchRequest(Id: ServiceFixture.Key(), OperationType: OperationType.REMOVE)]
                };
                break;
            case "merged-schema":
                entity.Payload = (MockEntities.Merchant() with
                {
                    CorporateName = null
                }

                ).ToJson();
                original = entity.Payload;
                request = new MerchantPatchRequest(Email: "test@example.com");
                break;
            case "merged-business":
                request = request with
                {
                    LimitType = LimitType.Global
                };
                break;
        }

        // Act
        var result = await fixture.MerchantService.PatchAsync(cnpj, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        entity.Payload.Should().Be(original);
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }
}
