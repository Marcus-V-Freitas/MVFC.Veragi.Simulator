using MVFC.Veragi.Simulator.Domain.Services.Receivables;
using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class OptionalContractSnapshotTests
{
    public static TheoryData<ContractByExternalReference> SnapshotsWithoutReachedReceivables =>
    [
        new ContractByExternalReference(Status: ContractStatusType.Active),
        new ContractByExternalReference(Status: ContractStatusType.Active, ReachedGuarantees: [new ContractByExternalReferenceGuarantee(MockEntities.MerchantCnpj)]),
        new ContractByExternalReference(Status: ContractStatusType.Active, ReachedGuarantees: [new ContractByExternalReferenceGuarantee(MockEntities.MerchantCnpj, [new ContractByExternalReferenceAcquirer(MockEntities.AcquirerCnpj)])]),
        new ContractByExternalReference(Status: ContractStatusType.Active, ReachedGuarantees: [new ContractByExternalReferenceGuarantee(MockEntities.MerchantCnpj, [new ContractByExternalReferenceAcquirer(MockEntities.AcquirerCnpj, [new ContractByExternalReferencePaymentArrangement(MockEntities.ArrangementCode)])])]),
    ];

    [Theory]
    [MemberData(nameof(SnapshotsWithoutReachedReceivables))]
    public void SnapshotWithoutReachedReceivablesDoesNotReduceMerchantBalance(ContractByExternalReference details)
    {
        // Arrange
        var operation = MockEntities.Operation("contract", MockEntities.MerchantCnpj, DateTime.UtcNow, ScheduleQueryStatusType.PROCESSED);
        operation.ResultJson = details.ToJson();

        // Act
        var commitments = ReceivableBalances.Commitments([operation], includePending: false);
        var reached = details.ReachedTotal();

        // Assert
        commitments.Should().BeEmpty();
        reached.Should().Be(0);
    }

    [Fact]
    public void ContractWithoutReachedGuaranteesPublishesEmptyAnticipatedAgenda()
    {
        // Arrange
        var operation = MockEntities.Operation("contract", MockEntities.MerchantCnpj, DateTime.UtcNow, ScheduleQueryStatusType.PROCESSED);
        operation.ResultJson = new ContractByExternalReference(Status: ContractStatusType.ContractSimulation).ToJson();

        // Act
        var webhook = operation.ToContractWebhook(DateTime.UtcNow);

        // Assert
        webhook.Status.Should().Be(ScheduleQueryStatusType.PROCESSED);
        webhook.ScheduleQueryData.Acquirers.Should().BeEmpty();
        webhook.ScheduleQueryData.RequestId.Should().Be(operation.Id.ToString());
    }

    [Theory]
    [InlineData(ContractStatusType.Active, 150)]
    [InlineData(ContractStatusType.Settled, 0)]
    [InlineData(ContractStatusType.PendingRegistration, 0)]
    public void DetailsWithoutRegistrationAmountsUseRequestedValueOnlyForActiveDebt(ContractStatusType status, decimal expectedDebt)
    {
        // Arrange
        var request = MockEntities.Contract(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 30), 150);

        // Act
        var details = request.ToDetails("SIM-reference", status);

        // Assert
        details.DebtBalanceAmount.Should().Be(expectedDebt);
        details.ReachedTotal().Should().Be(status is ContractStatusType.Active or ContractStatusType.Settled ? 150 : 0);
    }
}
