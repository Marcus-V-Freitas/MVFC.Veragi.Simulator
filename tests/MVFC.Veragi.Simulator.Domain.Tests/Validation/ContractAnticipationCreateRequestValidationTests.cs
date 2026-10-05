using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class ContractAnticipationCreateRequestValidationTests
{
    public static TheoryData<ContractAnticipationCreateRequest> InvalidRequests =>
    [
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            FinancierContractId = ""
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            FinancierContractId = new string ('a', 46)
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            ContractorCnpj = null
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            ContractorCnpj = "!invalid!"
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            Wallet = ""
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            Wallet = new string ('a', 257)
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            SignatureDate = null
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            SignatureDate = "invalid"
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            RequestedAmount = null
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            RequestedAmount = -0.99m
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            Guarantees = null
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            Guarantees = []
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            Guarantees = [null !]
        },
    ];

    public static TheoryData<ContractAnticipationCreateRequest> ValidRequests =>
    [
        MockEntities.FullContractAnticipationCreateRequest(),
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            FinancierContractId = null
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            Wallet = null
        },
        MockEntities.FullContractAnticipationCreateRequest()with
        {
            SettlementBankAccount = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(ContractAnticipationCreateRequest request)
    {
        // Arrange
        var validator = new ContractAnticipationCreateRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(ContractAnticipationCreateRequest request)
    {
        // Arrange
        var validator = new ContractAnticipationCreateRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
