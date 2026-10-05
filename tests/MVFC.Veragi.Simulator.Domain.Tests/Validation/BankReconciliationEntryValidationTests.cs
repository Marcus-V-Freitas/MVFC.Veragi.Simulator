using FluentAssertions;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Validation;
using MVFC.Veragi.Simulator.TestHelpers;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class BankReconciliationEntryValidationTests
{
    public static TheoryData<BankReconciliationEntry> InvalidRequests =>
    [
        MockEntities.FullBankReconciliationEntry()with
        {
            CreatedDate = "invalid"
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            LastModifiedDate = "invalid"
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            EntryId = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            EntryId = "invalid"
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            ReferenceDate = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            ReferenceDate = "invalid"
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            Merchant = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            Acquirer = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            BankAccount = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            BankAccount = new string ('a', 41)
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            Value = null
        },
    ];

    public static TheoryData<BankReconciliationEntry> ValidRequests =>
    [
        MockEntities.FullBankReconciliationEntry(),
        MockEntities.FullBankReconciliationEntry()with
        {
            CreatedBy = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            CreatedDate = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            LastModifiedBy = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            LastModifiedDate = null
        },
        MockEntities.FullBankReconciliationEntry()with
        {
            Id = null
        },
    ];

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void InvalidFieldProducesValidationFailure(BankReconciliationEntry request)
    {
        // Arrange
        var validator = new BankReconciliationEntryValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void ValidAndOmittedOptionalFieldsAreAccepted(BankReconciliationEntry request)
    {
        // Arrange
        var validator = new BankReconciliationEntryValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
