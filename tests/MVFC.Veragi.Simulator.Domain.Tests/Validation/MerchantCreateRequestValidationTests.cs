using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class MerchantCreateRequestValidationTests
{
    public static TheoryData<MerchantCreateRequest> InvalidRequests =>
    [
        MockEntities.FullMerchantCreateRequest()with
        {
            CorporateName = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            CorporateName = ""
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            CorporateName = new string ('a', 101)
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            Cnpj = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            Cnpj = "!invalid!"
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            Email = new string ('a', 101)
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            Email = "invalid"
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            MobilePhone = "!invalid!"
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            ReceivablesScheduleConfig = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            CreditConfigurations = []
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            CreditConfigurations = [CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital]
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            LimitType = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            LimitType = (LimitType)999
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            LimitAmount = -1m
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            ReleaseAccounts = [MockEntities.FullReleaseAccountCreateRequest(), MockEntities.FullReleaseAccountCreateRequest(), MockEntities.FullReleaseAccountCreateRequest(), MockEntities.FullReleaseAccountCreateRequest()]
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            ReleaseAccounts = [null !]
        },
    ];

    public static TheoryData<MerchantCreateRequest> ValidRequests =>
    [
        MockEntities.FullMerchantCreateRequest(),
        MockEntities.FullMerchantCreateRequest()with
        {
            Email = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            MobilePhone = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            CreditConfigurations = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            LimitAmount = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            ReleaseAccounts = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            CreditSettlementAccount = null
        },
        MockEntities.FullMerchantCreateRequest()with
        {
            AnticipationSettlementAccount = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(MerchantCreateRequest request)
    {
        // Arrange
        var validator = new MerchantCreateRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(MerchantCreateRequest request)
    {
        // Arrange
        var validator = new MerchantCreateRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
