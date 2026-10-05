using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Simulation;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Simulation;

public sealed class ControlAndScheduleTests
{
    [Theory]
    [InlineData("schema")]
    [InlineData("key")]
    [InlineData("exploratory")]
    [InlineData("missing")]
    [InlineData("deleted")]
    public async Task InvalidSubmissionCreatesNoScheduledOperation(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        fixture.Merchants.Add(MockEntities.MerchantEntity());
        var request = new ScheduleQueryRequest(MockEntities.MerchantCnpj, ScheduleQueryType.STANDARD);
        var key = ServiceFixture.Key();

        if (failure == "schema")
            request = request with
            {
                MerchantCnpj = null
            };

        if (failure == "key")
            key = "invalid";

        if (failure == "exploratory")
            request = request with
            {
                QueryType = ScheduleQueryType.EXPLORATORY
            };

        if (failure == "missing")
            fixture.Merchants.Clear();

        if (failure == "deleted")
            fixture.Merchants[0].IsDeleted = true;

        // Act
        var result = await fixture.ScheduleService.SubmitAsync(request, key, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Store.DidNotReceive().AddOperation(Arg.Any<ScheduledOperationEntity>());
    }

    [Fact]
    public async Task DueTimeIsRespectedAndMissingMerchantProducesErrorSchedule()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        fixture.Merchants.Add(MockEntities.MerchantEntity());

        // Act
        var submitted = await fixture.ScheduleService.SubmitAsync(new ScheduleQueryRequest(MockEntities.MerchantCnpj, ScheduleQueryType.STANDARD), ServiceFixture.Key(), CancellationToken.None);
        var early = await fixture.Processor.ProcessAsync("schedule", false, CancellationToken.None);
        fixture.Merchants.Clear();

        fixture.Clock.GetUtcNow()
                     .Returns(fixture.Clock.GetUtcNow().AddSeconds(5));

        var processed = await fixture.Processor.ProcessAsync("schedule", false, CancellationToken.None);

        // Assert
        early.Value.Should().Be(0);
        processed.Value.Should().Be(1);
        var schedule = (await fixture.ScheduleService.GetAsync(submitted.Value!.RequestId!, CancellationToken.None)).Value!;
        schedule.Status.Should().Be(ScheduleQueryStatusType.ERROR);
        schedule.ScheduleQueryData!.Acquirers.Should().BeNull();
        fixture.Deliveries.Should().ContainSingle();
    }

    [Theory]
    [InlineData("format")]
    [InlineData("missing")]
    [InlineData("kind")]
    public async Task InvalidScheduleLookupReturnsFailure(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var id = Guid.CreateVersion7(DateTimeOffset.UtcNow);

        if (failure == "kind")
            fixture.Operations.Add(new ScheduledOperationEntity { Id = id, Kind = "contract" });

        // Act
        var result = await fixture.ScheduleService.GetAsync(failure == "format" ? "invalid" : id.ToString(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("kind")]
    [InlineData("pending")]
    [InlineData("merchant")]
    [InlineData("deleted")]
    public async Task InvalidRefreshDoesNotPublishEvent(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var id = await fixture.PrepareAsync();
        var operation = fixture.Operations.Single();

        if (failure == "missing")
            fixture.Operations.Clear();

        if (failure == "kind")
            operation.Kind = "contract";

        if (failure == "pending")
            operation.Status = ScheduleQueryStatusType.PROCESSING;

        if (failure == "merchant")
            fixture.Merchants.Clear();

        if (failure == "deleted")
            fixture.Merchants[0].IsDeleted = true;

        var count = fixture.Deliveries.Count;

        // Act
        var result = await fixture.Processor.RepublishScheduleAsync(id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Deliveries.Should().HaveCount(count);
    }

    [Fact]
    public async Task RefreshUsesContractReceivablesSchemaAndDoesNotDuplicateIdenticalSnapshot()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        fixture.Options = fixture.Options with { ScheduleWebhookSchema = ScheduleWebhookSchema.ContractReceivables };
        var id = await fixture.PrepareAsync();

        // Act
        var result = await fixture.Processor.RepublishScheduleAsync(id, CancellationToken.None);
        await fixture.Processor.RepublishScheduleAsync(id, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.Deliveries.Should().HaveCount(2);
        fixture.Deliveries.Should().OnlyContain(x => x.Payload.Contains("scheduleQueryData"));
    }

    [Fact]
    public async Task ReplayKeepsIdentityAndPayloadWhileResettingDeliveryState()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var delivery = fixture.Deliveries.Single();
        delivery.Delivered = true;
        delivery.DeadLetter = true;
        delivery.Attempts = 5;
        var payload = delivery.Payload;
        var id = delivery.Id;

        // Act
        var result = await fixture.ControlService.ReplayAsync(id.ToString(), CancellationToken.None);
        var missing = await fixture.ControlService.ReplayAsync(ServiceFixture.Key(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        missing.IsSuccess.Should().BeFalse();
        delivery.Id.Should().Be(id);
        delivery.Payload.Should().Be(payload);
        delivery.Delivered.Should().BeFalse();
        delivery.DeadLetter.Should().BeFalse();
        delivery.Attempts.Should().Be(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InternalReceiverDeduplicatesByBusinessEventKeyWithIndependentStorageId(bool contractReceivables)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var operation = fixture.Operations.Single();
        var query = operation.ResultJson.FromJson<ScheduleQuery>()!;
        var payload = contractReceivables ? query.ToContractReceivablesWebhook().ToJson() : query.ToScheduleWebhook().ToJson();
        var key = ServiceFixture.Key();

        // Act
        var received = await fixture.ControlService.ReceiveAsync("schedule", key, payload, CancellationToken.None);
        var duplicate = await fixture.ControlService.ReceiveAsync("schedule", key, payload, CancellationToken.None);
        var conflict = await fixture.ControlService.ReceiveAsync("schedule", key, "{}", CancellationToken.None);
        var invalid = await fixture.ControlService.ReceiveAsync("schedule", "invalid", payload, CancellationToken.None);

        // Assert
        received.IsSuccess.Should().BeTrue();
        duplicate.IsSuccess.Should().BeTrue();
        conflict.IsSuccess.Should().BeFalse();
        invalid.IsSuccess.Should().BeFalse();
        var receipt = fixture.Receipts.Should().ContainSingle().Which;
        receipt.Id.Version.Should().Be(7);
        receipt.Id.ToString().Should().NotBe(key);
        receipt.RequestId.Should().Be(operation.Id.ToString());
        receipt.MerchantCnpj.Should().Be(MockEntities.MerchantCnpj);
    }

    [Fact]
    public async Task EnvironmentResetReturnsDocumentCountAndReleasesBothGates()
    {
        // Arrange
        using var fixture = new ServiceFixture();

        fixture.Store.ResetAsync(CancellationToken.None)
                     .Returns(10);

        var service = new EnvironmentService(fixture.Store, fixture.Gate, fixture.DeliveryGate);

        // Act
        var result = await service.ResetAsync(CancellationToken.None);
        using var lease = await fixture.Gate.EnterAsync(CancellationToken.None);
        lease.Dispose();
        lease.Dispose();
        await fixture.DeliveryGate.EnterAsync(CancellationToken.None);
        fixture.DeliveryGate.Exit();

        // Assert
        result.Value!.RemovedDocuments.Should().Be(10);
        await fixture.Store.Received(1).ResetAsync(CancellationToken.None);
    }

    [Fact]
    public void CatalogFiltersValidateFormatsAndUnknownValuesReturnEmptyLists()
    {
        // Arrange
        var catalog = new CatalogService();
        var invalid = catalog.GetArrangements("invalid");
        var unknown = catalog.GetArrangements("XXX");

        // Act
        var acquirers = catalog.GetAcquirers("33185894000174");

        // Assert
        invalid.IsSuccess.Should().BeFalse();
        unknown.Value.Should().BeEmpty();
        acquirers.Value.Should().BeEmpty();
        catalog.GetAcquirers(null).Value.Should().HaveCount(3);
        catalog.GetArrangements(null).Value.Should().HaveCount(4);
    }
}
