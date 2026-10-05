using MVFC.Veragi.Simulator.Domain.Services.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Reconciliation;

public sealed class ExternalAnticipationTests
{
    [Theory]
    [InlineData("cnpj")]
    [InlineData("key")]
    [InlineData("acquirer")]
    [InlineData("arrangement")]
    [InlineData("date")]
    [InlineData("amount")]
    [InlineData("precision")]
    [InlineData("missing")]
    [InlineData("deleted")]
    [InlineData("excess")]
    public async Task InvalidExternalAnticipationDoesNotDebitBalance(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var service = new ExternalAnticipationService(fixture.Store, fixture.Gate);
        var request = new ExternalAnticipationRequest(MockEntities.AcquirerCnpj, MockEntities.ArrangementCode, fixture.Contract().Guarantees![0].SettlementDate, 100);
        var cnpj = MockEntities.MerchantCnpj;
        var key = ServiceFixture.Key();
        switch (failure)
        {
            case "cnpj":
                cnpj = "invalid";
                break;
            case "key":
                key = "invalid";
                break;
            case "acquirer":
                request = request with
                {
                    AcquirerCnpj = null
                };
                break;
            case "arrangement":
                request = request with
                {
                    PaymentArrangementCode = null
                };
                break;
            case "date":
                request = request with
                {
                    SettlementDate = "invalid"
                };
                break;
            case "amount":
                request = request with
                {
                    Amount = 0
                };
                break;
            case "precision":
                request = request with
                {
                    Amount = 0.001m
                };
                break;
            case "missing":
                fixture.Merchants.Clear();
                break;
            case "deleted":
                fixture.Merchants[0].IsDeleted = true;
                break;
            case "excess":
                request = request with
                {
                    Amount = 1001
                };
                break;
        }

        fixture.Store.ClearReceivedCalls();

        // Act
        var result = await service.CreateAsync(cnpj, request, key, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Store.DidNotReceive().AddExternalAnticipation(Arg.Any<ExternalAnticipationEntity>());
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExternalAnticipationIsIdempotentAndChangedPayloadConflicts()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var service = new ExternalAnticipationService(fixture.Store, fixture.Gate);
        var request = new ExternalAnticipationRequest(MockEntities.AcquirerCnpj, MockEntities.ArrangementCode, fixture.Contract().Guarantees![0].SettlementDate, 100);
        var key = ServiceFixture.Key();
        var first = await service.CreateAsync(MockEntities.MerchantCnpj, request, key, CancellationToken.None);
        var duplicate = await service.CreateAsync(MockEntities.MerchantCnpj, request, key, CancellationToken.None);

        // Act
        var conflict = await service.CreateAsync(MockEntities.MerchantCnpj, request with { Amount = 200 }, key, CancellationToken.None);

        // Assert
        duplicate.Value.Should().Be(first.Value);
        conflict.IsSuccess.Should().BeFalse();
        fixture.ExternalAnticipations.Should().ContainSingle();
    }
}
