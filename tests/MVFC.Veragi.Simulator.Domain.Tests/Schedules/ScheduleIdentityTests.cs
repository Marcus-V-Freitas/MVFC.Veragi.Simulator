using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Schedules;

public sealed class ScheduleIdentityTests
{
    [Theory]
    [InlineData("11111111-1111-4111-8111-111111111111")]
    [InlineData("019a0123-4567-789a-8abc-123456789012")]
    public async Task ExistingVersionFourAndVersionSevenIdsResolveTheSameScheduleContract(string uuid)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var operation = MockEntities.Operation("schedule", MockEntities.MerchantCnpj, fixture.Clock.GetUtcNow().UtcDateTime);
        operation.Id = Guid.Parse(uuid);
        operation.ResultJson = new ScheduleQuery(Status: ScheduleQueryStatusType.PROCESSING, ScheduleQueryData: new Schedule(RequestId: uuid)).ToJson();
        fixture.Operations.Add(operation);

        // Act
        var result = await fixture.ScheduleService.GetAsync(uuid, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ScheduleQueryStatusType.PROCESSING);
        result.Value.ScheduleQueryData!.RequestId.Should().Be(uuid);
        await fixture.Store.Received(1).GetOperationAsync(operation.Id, CancellationToken.None);
    }

    [Theory]
    [InlineData("11111111-1111-1111-8111-111111111111")]
    [InlineData("11111111-1111-5111-8111-111111111111")]
    [InlineData("11111111-1111-8111-8111-111111111111")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("11111111111141118111111111111111")]
    [InlineData("{11111111-1111-4111-8111-111111111111}")]
    public async Task UnsupportedVersionsAndNonCanonicalIdsAreRejectedBeforePersistenceLookup(string uuid)
    {
        // Arrange
        using var fixture = new ServiceFixture();

        // Act
        var result = await fixture.ScheduleService.GetAsync(uuid, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        ((SimulationFailureException)result.Exception!).StatusCode.Should().Be(400);
        await fixture.Store.DidNotReceive().GetOperationAsync(Arg.Any<Guid>(), CancellationToken.None);
    }
}
