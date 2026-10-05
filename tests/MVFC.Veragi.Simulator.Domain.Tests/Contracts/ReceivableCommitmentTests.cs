using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Services.Receivables;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using FluentAssertions;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class ReceivableCommitmentTests
{
    [Theory]
    [InlineData(ContractStatusType.Active, 100)]
    [InlineData(ContractStatusType.Cancelled, 0)]
    [InlineData(ContractStatusType.Settled, 100)]
    [InlineData(ContractStatusType.ContractSimulation, 0)]
    [InlineData(ContractStatusType.PendingEdit, 0)]
    [InlineData(ContractStatusType.PendingRegistration, 0)]
    public void OnlyRegisteredAndSettledContractsCommitTheirReachedAmount(ContractStatusType status, decimal expected)
    {
        // Arrange
        var unit = new ContractByExternalReferenceReceivableUnit(SettlementDate: "2026-10-30", RequestedAmount: 150, ReachedAmount: 100);
        var details = new ContractByExternalReference(Status: status, ReachedGuarantees: [new ContractByExternalReferenceGuarantee(MockEntities.MerchantCnpj, [new ContractByExternalReferenceAcquirer(MockEntities.AcquirerCnpj, [new ContractByExternalReferencePaymentArrangement(MockEntities.ArrangementCode, [unit])])])]);
        var operation = MockEntities.Operation("contract", MockEntities.MerchantCnpj, DateTime.UtcNow, ScheduleQueryStatusType.PROCESSED);
        operation.ResultJson = details.ToJson();
        var key = ReceivableBalances.Key(MockEntities.MerchantCnpj, MockEntities.AcquirerCnpj, MockEntities.ArrangementCode, "2026-10-30");

        // Act
        var commitments = ReceivableBalances.Commitments([operation], includePending: false);

        // Assert
        commitments.GetValueOrDefault(key).Should().Be(expected);
        commitments.Should().HaveCount(expected == 0 ? 0 : 1);
    }

    [Theory]
    [InlineData(true, 100)]
    [InlineData(false, 0)]
    public void PendingContractsReserveRequestedGuaranteesOnlyWhenExplicitlyIncluded(bool includePending, decimal expected)
    {
        // Arrange
        var request = MockEntities.Contract(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 30), 100);
        var operation = MockEntities.Operation("contract", MockEntities.MerchantCnpj, DateTime.UtcNow);
        operation.RequestJson = request.ToJson();
        var key = ContractService.UnitKey(request.Guarantees![0]);

        // Act
        var commitments = ReceivableBalances.Commitments([operation], includePending);

        // Assert
        commitments.GetValueOrDefault(key).Should().Be(expected);
        commitments.Should().HaveCount(includePending ? 1 : 0);
    }

    [Fact]
    public void FailedOperationsAndRegisteredContractsWithoutReachedGuaranteesDoNotReduceFreeBalance()
    {
        // Arrange
        var failed = MockEntities.Operation("contract", MockEntities.MerchantCnpj, DateTime.UtcNow, ScheduleQueryStatusType.ERROR);
        failed.ResultJson = "invalid-json";
        var registered = MockEntities.Operation("contract", MockEntities.MerchantCnpj, DateTime.UtcNow, ScheduleQueryStatusType.PROCESSED);
        registered.ResultJson = new ContractByExternalReference(Status: ContractStatusType.Active).ToJson();

        // Act
        var commitments = ReceivableBalances.Commitments([failed, registered], includePending: true);

        // Assert
        commitments.Should().BeEmpty();
    }
}
