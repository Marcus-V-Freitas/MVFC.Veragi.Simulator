using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class ContractAnticipationGuaranteeCreateValidationTests
{
    public static TheoryData<ContractAnticipationGuaranteeCreate> InvalidRequests =>
    [
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            ReceivableUnitHolderCnpj = null
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            ReceivableUnitHolderCnpj = "!invalid!"
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            FinalUserReceiverCnpj = null
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            FinalUserReceiverCnpj = "!invalid!"
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            AcquirerCnpj = null
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            AcquirerCnpj = "!invalid!"
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            PaymentArrangementCode = null
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            PaymentArrangementCode = "!invalid!"
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            SettlementDate = null
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            SettlementDate = "invalid"
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            DefinedAmount = null
        },
        MockEntities.FullContractAnticipationGuaranteeCreate()with
        {
            DefinedAmount = -0.99m
        },
    ];

    public static TheoryData<ContractAnticipationGuaranteeCreate> ValidRequests =>
    [
        MockEntities.FullContractAnticipationGuaranteeCreate(),
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(ContractAnticipationGuaranteeCreate request)
    {
        // Arrange
        var validator = new ContractAnticipationGuaranteeCreateValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(ContractAnticipationGuaranteeCreate request)
    {
        // Arrange
        var validator = new ContractAnticipationGuaranteeCreateValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
