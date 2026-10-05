using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class MerchantPatchRequestValidationTests
{
    public static TheoryData<MerchantPatchRequest> InvalidRequests =>
    [
        MockEntities.FullMerchantPatchRequest()with
        {
            CorporateName = ""
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            CorporateName = new string ('a', 101)
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            Email = new string ('a', 101)
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            MobilePhone = "!invalid!"
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            CreditConfigurations = []
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            CreditConfigurations = [CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital, CreditConfigurationType.CreditSecuredWorkingCapital]
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            LimitType = (LimitType)999
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            LimitAmount = -1m
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            ReleaseAccounts = [null !]
        },
    ];

    public static TheoryData<MerchantPatchRequest> ValidRequests =>
    [
        MockEntities.FullMerchantPatchRequest(),
        MockEntities.FullMerchantPatchRequest()with
        {
            CorporateName = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            Email = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            MobilePhone = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            ReceivablesScheduleConfig = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            CreditConfigurations = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            LimitType = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            LimitAmount = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            ReleaseAccounts = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            CreditSettlementAccount = null
        },
        MockEntities.FullMerchantPatchRequest()with
        {
            AnticipationSettlementAccount = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(MerchantPatchRequest request)
    {
        // Arrange
        var validator = new MerchantPatchRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(MerchantPatchRequest request)
    {
        // Arrange
        var validator = new MerchantPatchRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
