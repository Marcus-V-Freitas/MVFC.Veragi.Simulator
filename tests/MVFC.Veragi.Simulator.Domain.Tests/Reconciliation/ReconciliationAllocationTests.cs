using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Domain.Services.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Reconciliation;

public sealed class ReconciliationAllocationTests
{
    [Fact]
    public async Task PaymentAllocatesAcrossContractsAndNeverExceedsEachReachedBalance()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(100), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        await fixture.ContractService.CreateAsync(fixture.Contract(100), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        await fixture.ReconciliationService.CreateAsync(MockEntities.ReconciliationRequest(fixture.Calendar.Today, 150), CancellationToken.None);
        var entry = MockEntities.ReconciliationRequest(fixture.Calendar.Today, 51);

        // Act
        var result = await fixture.ReconciliationService.CreateAsync(entry, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var allocations = fixture.Entries.SelectMany(item => item.AllocationsJson.FromJson<List<ReconciledReceivableResponse>>()!).ToArray();
        allocations.Sum(unit => unit.Amount).Should().Be(200);
        allocations.GroupBy(unit => unit.ExternalReference).Should().HaveCount(2).And.AllSatisfy(group => group.Sum(unit => unit.Amount).Should().Be(100));
        fixture.Entries[1].UnallocatedAmount.Should().Be(1);
    }

    [Fact]
    public async Task EarlierMaturityTakesPriorityOverEarlierContractAndAmountsCanSpanInstallments()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var older = MockEntities.ReconciliationContract(MockEntities.Contract(fixture.Calendar.Today, fixture.Calendar.Today.AddDays(12), 100).ToDetails("older", ContractStatusType.Active), "older", new DateTime(2026, 10, 1));
        var newer = MockEntities.ReconciliationContract(MockEntities.Contract(fixture.Calendar.Today, fixture.Calendar.Today.AddDays(8), 100).ToDetails("newer", ContractStatusType.Active), "newer", new DateTime(2026, 10, 2));
        var service = new ReconciliationAllocationService(fixture.Store);

        // Act
        var allocated = await service.AllocateAsync(MockEntities.MerchantCnpj, MockEntities.AcquirerCnpj, 150, [older, newer], CancellationToken.None);

        // Assert
        allocated.Select(unit => unit.ExternalReference).Should().Equal("newer", "older");
        allocated.Select(unit => unit.Amount).Should().Equal(100, 50);
        allocated.Should().OnlyContain(unit => unit.HolderCnpj == MockEntities.MerchantCnpj);
    }

    [Fact]
    public async Task PreviouslyPaidHolderDoesNotConsumeAnotherHolderWithSameAcquirerArrangementAndDate()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var details = MockEntities.Contract(fixture.Calendar.Today, fixture.Calendar.Today.AddDays(8), 100).ToDetails("contract", ContractStatusType.Active);
        var originalHolder = details.ReachedGuarantees![0];
        details = details with { ReachedGuarantees = [originalHolder, originalHolder with { ReceivableUnitHolderCnpj = "44185894000174" }] };
        var contract = MockEntities.ReconciliationContract(details, "contract", new DateTime(2026, 10, 1));
        var previous = MockEntities.Entry(MockEntities.MerchantCnpj, Guid.CreateVersion7(DateTimeOffset.UtcNow));
        previous.AllocationsJson = new[] { contract.ToReceivables()[0] }.ToJson();
        fixture.Entries.Add(previous);
        var service = new ReconciliationAllocationService(fixture.Store);

        // Act
        var allocated = await service.AllocateAsync(MockEntities.MerchantCnpj, MockEntities.AcquirerCnpj, 100, [contract], CancellationToken.None);

        // Assert
        allocated.Should().ContainSingle();
        allocated[0].HolderCnpj.Should().Be("44185894000174");
        allocated[0].Amount.Should().Be(100);
    }

    [Fact]
    public async Task SinglePaymentCanCoverMultipleInstallmentsOfSameContract()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var details = MockEntities.Contract(fixture.Calendar.Today, fixture.Calendar.Today.AddDays(8), 100).ToDetails("contract", ContractStatusType.Active);
        var holder = details.ReachedGuarantees![0];
        var acquirer = holder.Acquirers![0];
        var arrangement = acquirer.PaymentArrangements![0];
        var earlier = arrangement.ReceivableUnits![0];
        var later = earlier with { SettlementDate = fixture.Calendar.Today.AddDays(12).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) };
        details = details with { ReachedGuarantees = [holder with { Acquirers = [acquirer with { PaymentArrangements = [arrangement with { ReceivableUnits = [later, earlier] }] }] }] };
        var contract = MockEntities.ReconciliationContract(details, "contract", new DateTime(2026, 10, 1));
        var service = new ReconciliationAllocationService(fixture.Store);

        // Act
        var allocated = await service.AllocateAsync(MockEntities.MerchantCnpj, MockEntities.AcquirerCnpj, 150, [contract], CancellationToken.None);

        // Assert
        allocated.Should().HaveCount(2);
        allocated.Select(unit => unit.SettlementDate).Should().Equal(earlier.SettlementDate, later.SettlementDate);
        allocated.Select(unit => unit.Amount).Should().Equal(100, 50);
        allocated.Should().OnlyContain(unit => unit.ExternalReference == "contract");
    }

    [Theory]
    [InlineData("guarantees")]
    [InlineData("acquirers")]
    [InlineData("arrangements")]
    [InlineData("units")]
    [InlineData("amount")]
    public async Task MissingReachedCollectionsOrAmountsDoNotProduceAllocations(string omitted)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var details = MockEntities.Contract(fixture.Calendar.Today, fixture.Calendar.Today.AddDays(8), 100).ToDetails("contract", ContractStatusType.Active);
        var holder = details.ReachedGuarantees![0];
        var acquirer = holder.Acquirers![0];
        var arrangement = acquirer.PaymentArrangements![0];

        switch (omitted)
        {
            case "guarantees": details = details with { ReachedGuarantees = null }; break;
            case "acquirers": details = details with { ReachedGuarantees = [holder with { Acquirers = null }] }; break;
            case "arrangements": details = details with { ReachedGuarantees = [holder with { Acquirers = [acquirer with { PaymentArrangements = null }] }] }; break;
            case "units": details = details with { ReachedGuarantees = [holder with { Acquirers = [acquirer with { PaymentArrangements = [arrangement with { ReceivableUnits = null }] }] }] }; break;
            case "amount": details = details with { ReachedGuarantees = [holder with { Acquirers = [acquirer with { PaymentArrangements = [arrangement with { ReceivableUnits = [arrangement.ReceivableUnits![0] with { ReachedAmount = null }] }] }] }] }; break;
        }

        var contract = MockEntities.ReconciliationContract(details, "contract", new DateTime(2026, 10, 1));
        var service = new ReconciliationAllocationService(fixture.Store);

        // Act
        var allocated = await service.AllocateAsync(MockEntities.MerchantCnpj, MockEntities.AcquirerCnpj, 100, [contract], CancellationToken.None);

        // Assert
        allocated.Should().BeEmpty();
    }
}
