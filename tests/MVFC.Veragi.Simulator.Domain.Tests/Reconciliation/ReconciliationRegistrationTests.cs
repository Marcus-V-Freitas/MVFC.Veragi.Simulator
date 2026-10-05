using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Reconciliation;

public sealed class ReconciliationRegistrationTests
{
    [Theory]
    [InlineData("schema")]
    [InlineData("merchant-format")]
    [InlineData("acquirer-format")]
    [InlineData("date")]
    [InlineData("amount")]
    [InlineData("account-empty")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("deleted")]
    [InlineData("unmatched-account")]
    [InlineData("contract-status")]
    [InlineData("contract-processing")]
    [InlineData("missing-contract")]
    public async Task InvalidReconciliationDoesNotWriteEntry(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(100), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var request = MockEntities.ReconciliationRequest(fixture.Calendar.Today);
        var contract = fixture.Operations.Single(operation => operation.Kind == "contract");

        switch (failure)
        {
            case "schema": request = request with { EntryId = null }; break;
            case "merchant-format": request = request with { Merchant = "invalid" }; break;
            case "acquirer-format": request = request with { Acquirer = "invalid" }; break;
            case "date": request = request with { ReferenceDate = "2026-10-01" }; break;
            case "amount": request = request with { Value = -1 }; break;
            case "account-empty": request = request with { BankAccount = " " }; break;
            case "duplicate": fixture.Entries.Add(new ReconciliationEntity { EntryId = Guid.Parse(request.EntryId!) }); break;
            case "missing": fixture.Merchants.Clear(); break;
            case "deleted": fixture.Merchants[0].IsDeleted = true; break;
            case "unmatched-account": request = request with { BankAccount = "8888" }; break;
            case "contract-status":
                contract.ResultJson = (contract.ResultJson.FromJson<ContractByExternalReference>()! with { Status = ContractStatusType.Settled }).ToJson();
                break;
            case "contract-processing": contract.Status = ScheduleQueryStatusType.PROCESSING; break;
            case "missing-contract": fixture.Operations.Remove(contract); break;
        }

        fixture.Store.ClearReceivedCalls();

        // Act
        var result = await fixture.ReconciliationService.CreateAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Store.DidNotReceive().AddEntry(Arg.Any<ReconciliationEntity>());
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(50, 50, 0)]
    [InlineData(100, 100, 0)]
    [InlineData(101, 100, 1)]
    public async Task ValidEntryIsRegisteredAndOnlyAvailableValueIsAllocated(decimal value, decimal allocated, decimal unmatched)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(100), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var request = MockEntities.ReconciliationRequest(fixture.Calendar.Today, value);

        // Act
        var result = await fixture.ReconciliationService.CreateAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var entry = fixture.Entries.Should().ContainSingle().Subject;
        entry.Payload.FromJson<Shareable.Requests.Reconciliation.BankReconciliationEntry>().Should().Be(request);
        entry.AllocationsJson.FromJson<List<ReconciledReceivableResponse>>()!.Sum(unit => unit.Amount).Should().Be(allocated);
        entry.UnallocatedAmount.Should().Be(unmatched);
        entry.Id.Version.Should().Be(7);
        entry.Id.Should().NotBe(entry.EntryId);
    }

    [Fact]
    public async Task DifferentEntryIdsOnSameAccountAreRegisteredAfterBalanceIsConsumed()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(100), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        await fixture.ReconciliationService.CreateAsync(MockEntities.ReconciliationRequest(fixture.Calendar.Today, 100), CancellationToken.None);
        var request = MockEntities.ReconciliationRequest(fixture.Calendar.Today, 50);

        // Act
        var result = await fixture.ReconciliationService.CreateAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.Entries.Should().HaveCount(2);
        fixture.Entries[1].AllocationsJson.FromJson<List<ReconciledReceivableResponse>>().Should().BeEmpty();
        fixture.Entries[1].UnallocatedAmount.Should().Be(50);
    }

    [Fact]
    public async Task ValidAcquirerWithoutMatchingReceivablesLeavesEntryUnallocated()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(100), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var request = MockEntities.ReconciliationRequest(fixture.Calendar.Today) with { Acquirer = "01027058000191" };

        // Act
        var result = await fixture.ReconciliationService.CreateAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.Entries.Single().UnallocatedAmount.Should().Be(100);
        fixture.Entries.Single().AllocationsJson.FromJson<List<ReconciledReceivableResponse>>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContractAccountIsIndependentOfLaterMerchantChanges(bool explicitAccount)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var request = fixture.Contract(100);

        if (explicitAccount)
            request = request with { SettlementBankAccount = new SettlementAccountContract(Document: MockEntities.MerchantCnpj, AccountType: AccountType.CONTA_DEPOSITO_A_VISTA, Compe: "341", Ispb: "60701190", Branch: "1234", Account: "555") };

        await fixture.ContractService.CreateAsync(request, ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var merchant = fixture.Merchants.Single().Payload.FromJson<Merchant>()!;
        fixture.Merchants.Single().Payload = (merchant with { AnticipationSettlementAccount = merchant.AnticipationSettlementAccount! with { Account = "8888" } }).ToJson();
        var entry = MockEntities.ReconciliationRequest(fixture.Calendar.Today);

        // Act
        var result = await fixture.ReconciliationService.CreateAsync(entry, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.Operations.Single(operation => operation.Kind == "contract").SettlementBankAccountCode.Should().Be("555");
        fixture.Operations.Single(operation => operation.Kind == "contract").RequestJson.FromJson<Shareable.Requests.Contracts.ContractAnticipationCreateRequest>().Should().BeEquivalentTo(request);
        fixture.Entries.Single().UnallocatedAmount.Should().Be(0);
    }
}
