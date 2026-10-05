using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class ContractDebtTests
{
    [Theory]
    [InlineData(0, 100)]
    [InlineData(30, 70)]
    [InlineData(100, 0)]
    [InlineData(101, 0)]
    public async Task OnlyAllocatedPaymentsForSameMerchantAndContractReduceDebt(decimal paid, decimal expected)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var registered = new ContractByExternalReference(Status: ContractStatusType.Active, DebtBalanceAmount: 100);
        fixture.Entries.Add(new ReconciliationEntity
        {
            MerchantCnpj = MockEntities.MerchantCnpj,
            UnallocatedAmount = 500,
            AllocationsJson = new[]
            {
                new ReconciledReceivableResponse("contract", MockEntities.MerchantCnpj, MockEntities.AcquirerCnpj, MockEntities.ArrangementCode, "2026-10-09", paid),
                new ReconciledReceivableResponse("other", MockEntities.MerchantCnpj, MockEntities.AcquirerCnpj, MockEntities.ArrangementCode, "2026-10-09", 100)
            }.ToJson()
        });
        fixture.Entries.Add(new ReconciliationEntity
        {
            MerchantCnpj = "44185894000174",
            AllocationsJson = new[] { new ReconciledReceivableResponse("contract", "44185894000174", MockEntities.AcquirerCnpj, MockEntities.ArrangementCode, "2026-10-09", 100) }.ToJson()
        });
        var service = new ContractDebtService(fixture.Store);

        // Act
        var result = await service.ProjectAsync(MockEntities.MerchantCnpj, "contract", registered, CancellationToken.None);

        // Assert
        result.DebtBalanceAmount.Should().Be(expected);
        result.Status.Should().Be(ContractStatusType.Active);
        registered.DebtBalanceAmount.Should().Be(100);
        await fixture.Store.Received(1).GetEntriesAsync(MockEntities.MerchantCnpj, CancellationToken.None);
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Fact]
    public async Task MultipleLedgerEntriesAccumulateWithoutChangingRegisteredGuarantees()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var created = await fixture.ContractService.CreateAsync(fixture.Contract(100), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        var operation = fixture.Operations.Single(item => item.Kind == "contract");
        var snapshot = operation.ResultJson;
        await fixture.ReconciliationService.CreateAsync(MockEntities.ReconciliationRequest(fixture.Calendar.Today, 30), CancellationToken.None);
        await fixture.ReconciliationService.CreateAsync(MockEntities.ReconciliationRequest(fixture.Calendar.Today, 20), CancellationToken.None);

        // Act
        var result = await fixture.ContractService.GetAsync(MockEntities.MerchantCnpj, created.Value![0].ExternalReference!, CancellationToken.None);
        var repeated = await fixture.ContractService.GetAsync(MockEntities.MerchantCnpj, created.Value[0].ExternalReference!, CancellationToken.None);

        // Assert
        result.Value!.DebtBalanceAmount.Should().Be(50);
        repeated.Value!.DebtBalanceAmount.Should().Be(50);
        result.Value.ReachedGuarantees.Should().BeEquivalentTo(snapshot.FromJson<ContractByExternalReference>()!.ReachedGuarantees);
        operation.ResultJson.Should().Be(snapshot);
        result.Value.Status.Should().Be(ContractStatusType.Active);
    }

    [Theory]
    [InlineData(ContractStatusType.Settled, 0)]
    [InlineData(ContractStatusType.Cancelled, 0)]
    [InlineData(ContractStatusType.PendingEdit, 0)]
    [InlineData(ContractStatusType.PendingRegistration, 0)]
    [InlineData(ContractStatusType.ContractSimulation, 0)]
    [InlineData(ContractStatusType.Active, null)]
    [InlineData(null, null)]
    public async Task OtherStatesAndLegacyMissingDebtKeepSnapshotWithoutReadingLedger(ContractStatusType? status, int? balance)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var registered = new ContractByExternalReference(Status: status, DebtBalanceAmount: balance);
        var service = new ContractDebtService(fixture.Store);

        // Act
        var result = await service.ProjectAsync(MockEntities.MerchantCnpj, "contract", registered, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(registered);
        await fixture.Store.DidNotReceive().GetEntriesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ContractWithoutLedgerEntriesKeepsInitialDebt()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var registered = new ContractByExternalReference(Status: ContractStatusType.Active, DebtBalanceAmount: 100);

        // Act
        var result = await new ContractDebtService(fixture.Store).ProjectAsync(MockEntities.MerchantCnpj, "contract", registered, CancellationToken.None);

        // Assert
        result.Should().Be(registered);
    }
}
