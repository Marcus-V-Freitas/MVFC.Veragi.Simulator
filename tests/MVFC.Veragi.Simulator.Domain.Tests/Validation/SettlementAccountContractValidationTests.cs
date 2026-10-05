using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class SettlementAccountContractValidationTests
{
    public static TheoryData<SettlementAccountContract> InvalidRequests =>
    [
        MockEntities.FullSettlementAccountContract()with
        {
            Document = null
        },
        MockEntities.FullSettlementAccountContract()with
        {
            Document = "!invalid!"
        },
        MockEntities.FullSettlementAccountContract()with
        {
            AccountType = null
        },
        MockEntities.FullSettlementAccountContract()with
        {
            AccountType = (AccountType)999
        },
        MockEntities.FullSettlementAccountContract()with
        {
            Compe = "!invalid!"
        },
        MockEntities.FullSettlementAccountContract()with
        {
            Ispb = null
        },
        MockEntities.FullSettlementAccountContract()with
        {
            Ispb = "!invalid!"
        },
        MockEntities.FullSettlementAccountContract()with
        {
            Branch = null
        },
        MockEntities.FullSettlementAccountContract()with
        {
            Branch = "!invalid!"
        },
        MockEntities.FullSettlementAccountContract()with
        {
            Account = null
        },
        MockEntities.FullSettlementAccountContract()with
        {
            Account = "!invalid!"
        },
    ];

    public static TheoryData<SettlementAccountContract> ValidRequests =>
    [
        MockEntities.FullSettlementAccountContract(),
        MockEntities.FullSettlementAccountContract()with
        {
            Compe = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(SettlementAccountContract request)
    {
        // Arrange
        var validator = new SettlementAccountContractValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(SettlementAccountContract request)
    {
        // Arrange
        var validator = new SettlementAccountContractValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
