using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MVFC.Veragi.Simulator.Data.Tests.Infrastructure;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Data.Tests.Persistence;

[Collection(DataCollectionDefinition.Name)]
public sealed class MongoStoreQueryTests(DataFixture fixture) : IClassFixture<DataFixture>
{
    private readonly DataFixture _fixture = fixture;

    [Fact]
    public async Task SchedulerQueriesSelectDueOperationsAndRetryableDeliveriesFromStoredState()
    {
        // Arrange
        var store = _fixture.Store;
        var context = _fixture.DbContext;
        var ct = CancellationToken.None;
        await store.ResetAsync(ct);
        var now = new DateTime(2026, 10, 2, 15, 0, 0, DateTimeKind.Utc);
        var due = MockEntities.Operation("schedule", MockEntities.MerchantCnpj, now);
        var future = MockEntities.Operation("schedule", MockEntities.MerchantCnpj, now.AddSeconds(1));
        var completed = MockEntities.Operation("schedule", MockEntities.MerchantCnpj, now, ScheduleQueryStatusType.PROCESSED);
        var otherMerchant = MockEntities.Operation("contract", "33185894000174", now);
        var pending = MockEntities.Delivery(due.Id, now);
        var delayed = MockEntities.Delivery(due.Id, now.AddSeconds(1));
        var delivered = MockEntities.Delivery(due.Id, now, delivered: true);
        var deadLetter = MockEntities.Delivery(due.Id, now, deadLetter: true);

        foreach (var operation in new[] { due, future, completed, otherMerchant })
            store.AddOperation(operation);

        foreach (var delivery in new[] { pending, delayed, delivered, deadLetter })
            store.AddDelivery(delivery);

        await store.SaveAsync(ct);
        context.ChangeTracker.Clear();
        var dueSchedules = await store.GetDueOperationsAsync("schedule", now, ct);
        var dueContracts = await store.GetDueOperationsAsync("contract", now, ct);
        var retryable = await store.GetPendingDeliveriesAsync(now, ct);
        var filtered = await store.GetOperationsAsync("contract", MockEntities.MerchantCnpj, ct);
        var foundDelivery = await store.GetDeliveryAsync(pending.Id.ToString(), ct);
        var invalidDelivery = await store.GetDeliveryAsync("invalid", ct);

        // Act
        var missingDelivery = await store.GetDeliveryAsync(Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(), ct);

        // Assert
        dueSchedules.Should().ContainSingle().Which.Id.Should().Be(due.Id);
        dueContracts.Should().ContainSingle().Which.Id.Should().Be(otherMerchant.Id);
        retryable.Should().ContainSingle().Which.Id.Should().Be(pending.Id);
        filtered.Should().BeEmpty();
        foundDelivery!.PayloadKey.Should().Be(pending.PayloadKey);
        invalidDelivery.Should().BeNull();
        missingDelivery.Should().BeNull();
        (await store.GetOperationsAsync("schedule", null, ct)).Should().HaveCount(3);
        (await store.GetDeliveryByPayloadKeyAsync(pending.PayloadKey, ct))!.Id.Should().Be(pending.Id);
        (await store.GetDeliveriesAsync(ct)).Should().HaveCount(4);
        await store.ResetAsync(ct);
    }

    [Fact]
    public async Task ReceiptsAndReconciliationEntriesUseBusinessKeysRatherThanMongoIds()
    {
        // Arrange
        var store = _fixture.Store;
        var context = _fixture.DbContext;
        var ct = CancellationToken.None;
        await store.ResetAsync(ct);
        var businessId = Guid.CreateVersion7(DateTimeOffset.UtcNow);
        var entry = MockEntities.Entry(MockEntities.MerchantCnpj, businessId);
        var otherEntry = MockEntities.Entry("33185894000174", Guid.CreateVersion7(DateTimeOffset.UtcNow));
        var receipt = MockEntities.Receipt("schedule:" + businessId, businessId, DateTime.UtcNow);
        store.AddEntry(entry);
        store.AddEntry(otherEntry);
        store.AddReceipt(receipt);
        await store.SaveAsync(ct);
        context.ChangeTracker.Clear();
        var foundEntry = await store.GetEntryAsync(businessId, ct);
        var foundReceipt = await store.GetReceiptAsync(receipt.EventKey, ct);
        var merchantEntries = await store.GetEntriesAsync(MockEntities.MerchantCnpj, ct);
        var allEntries = await store.GetEntriesAsync(null, ct);

        // Act
        var receipts = await store.GetReceiptsAsync(ct);

        // Assert
        foundEntry!.Id.Should().Be(entry.Id).And.NotBe(businessId);
        foundReceipt!.Id.Should().Be(receipt.Id);
        foundReceipt.EventKey.Should().Be(receipt.EventKey);
        merchantEntries.Should().ContainSingle().Which.Id.Should().Be(entry.Id);
        allEntries.Should().HaveCount(2);
        receipts.Should().ContainSingle().Which.RequestId.Should().Be(businessId.ToString());
        (await store.GetEntryAsync(entry.Id, ct)).Should().BeNull();
        (await store.GetReceiptAsync(receipt.Id.ToString(), ct)).Should().BeNull();
        (await context.Entries.CountAsync(ct)).Should().Be(2);
        (await context.Receipts.CountAsync(ct)).Should().Be(1);
        await store.ResetAsync(ct);
    }

    [Fact]
    public async Task SchedulerAndDispatcherBoundEachPersistedBatchToOneHundredItems()
    {
        // Arrange
        var store = _fixture.Store;
        var context = _fixture.DbContext;
        var ct = CancellationToken.None;
        await store.ResetAsync(ct);
        var now = new DateTime(2026, 10, 2, 15, 0, 0, DateTimeKind.Utc);
        for (var index = 0; index < 101; index++)
        {
            var operation = MockEntities.Operation("schedule", MockEntities.MerchantCnpj, now);
            store.AddOperation(operation);
            store.AddDelivery(MockEntities.Delivery(operation.Id, now));
        }

        await store.SaveAsync(ct);
        context.ChangeTracker.Clear();
        var operations = await store.GetDueOperationsAsync("schedule", now, ct);

        // Act
        var deliveries = await store.GetPendingDeliveriesAsync(now, ct);

        // Assert
        operations.Should().HaveCount(100);
        deliveries.Should().HaveCount(100);
        (await context.Operations.CountAsync(ct)).Should().Be(101);
        (await context.Deliveries.CountAsync(ct)).Should().Be(101);
        await store.ResetAsync(ct);
    }
}
