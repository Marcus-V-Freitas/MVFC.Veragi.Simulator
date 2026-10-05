using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MVFC.Veragi.Simulator.Domain.Services.Schedules;
using MVFC.Veragi.Simulator.Domain.Services.Contracts;
using MVFC.Veragi.Simulator.Domain.Services.Common;
using MVFC.Veragi.Simulator.Shareable.Requests.Contracts;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Domain.Validation;
using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Configuration;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Validation;

public sealed class BusinessRulesTests
{
    private readonly SimulatorOptions options = TestOptions();

    private static SimulatorOptions TestOptions() =>
        MockEntities.Options() with
        {
            MinimumBusinessDays = 2,
            Holidays = ["2026-10-05"]
        };

    [Fact]
    public void MinimumSettlementDateSkipsWeekendAndConfiguredHoliday()
    {
        // Arrange
        // Act
        var calendar = new BusinessCalendar(new FixedClock(), options);

        // Assert
        (calendar.AddBusinessDays(calendar.Today, 2)).Should().Be(new DateOnly(2026, 10, 7));
    }

    [Fact]
    public void RequiredScheduleQueryFieldsProduceResultFailure()
    {
        // Arrange
        // Act
        var result = new RequestValidator().Validate(new ScheduleQueryRequest(QueryType: ScheduleQueryType.STANDARD));

        // Assert
        (result.IsSuccess).Should().BeFalse();
        ((SimulationFailureException)result.Exception!).Violations.Should().Contain(x => x.Path == "$.merchantCnpj");
    }

    [Theory]
    [InlineData("22ABC894000174", true)]
    [InlineData("22185894000174", true)]
    [InlineData("22.185.894/0001-74", false)]
    [InlineData("2218589400017A", false)]
    public void CnpjAcceptsNumericAndAlphanumericWireFormats(string cnpj, bool expected)
    {
        // Arrange
        // Act
        var actual = CatalogService.IsCnpj(cnpj);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void WildcardCombinedWithAnotherAcquirerIsRejected()
    {
        // Arrange
        // Act
        var merchant = Merchant() with
        {
            ReceivablesScheduleConfig = new ReceivablesScheduleConfig(QueryWindow: QueryWindowType.P6M, AcquirerCnpjs: ["99999999000199", "82413081000116"], ArrangementCodes: ["VCC"])
        };

        // Assert
        (MerchantService.ValidateMerchant(merchant).IsSuccess).Should().BeFalse();
    }

    [Fact]
    public void GlobalLimitRequiresAmountAndOtherLimitsRejectAmount()
    {
        // Arrange
        // Act
        // Assert
        (MerchantService.ValidateMerchant(Merchant() with { LimitType = LimitType.Global }).IsSuccess).Should().BeFalse();
        (MerchantService.ValidateMerchant(Merchant() with { LimitAmount = 100 }).IsSuccess).Should().BeFalse();
        (MerchantService.ValidateMerchant(Merchant() with { LimitType = LimitType.Global, LimitAmount = 100 }).IsSuccess).Should().BeTrue();
    }

    [Fact]
    public void DuplicateReleaseAccountTupleIsRejected()
    {
        // Arrange
        // Act
        var account = new ReleaseAccount(AccountType: ReleaseAccountType.CONTA_DEPOSITO_A_VISTA, BankCode: "341", Branch: "1234", Account: "555", ExternalId: "one");

        // Assert
        (MerchantService.ValidateMerchant(Merchant() with { ReleaseAccounts = [account, account with { ExternalId = "two" }] }).IsSuccess).Should().BeFalse();
    }

    [Fact]
    public void ContractWithDifferentSumIsRejected()
    {
        // Arrange
        // Act
        var rules = new ContractRules(new BusinessCalendar(new FixedClock(), options), options);

        // Assert
        (rules.Validate(Contract() with { RequestedAmount = 200 }).IsSuccess).Should().BeFalse();
    }

    [Fact]
    public void ContractWithDifferentRecipientRootIsRejected()
    {
        // Arrange
        // Act
        var rules = new ContractRules(new BusinessCalendar(new FixedClock(), options), options);

        // Assert
        (rules.Validate(Contract() with { Guarantees = [Contract().Guarantees![0] with { FinalUserReceiverCnpj = "82413081000116" }] }).IsSuccess).Should().BeFalse();
    }

    [Fact]
    public void ContractCannotSettleBeforeMinimumBusinessDays()
    {
        // Arrange
        // Act
        var rules = new ContractRules(new BusinessCalendar(new FixedClock(), options), options);

        // Assert
        (rules.Validate(Contract() with { Guarantees = [Contract().Guarantees![0] with { SettlementDate = "2026-10-06" }] }).IsSuccess).Should().BeFalse();
        (rules.Validate(Contract()).IsSuccess).Should().BeTrue();
    }

    [Fact]
    public void ContractSettlementCannotPrecedeSignatureDate()
    {
        // Arrange
        // Act
        var rules = new ContractRules(new BusinessCalendar(new FixedClock(), options), options);

        // Assert
        (rules.Validate(Contract() with { SignatureDate = "2026-10-08" }).IsSuccess).Should().BeFalse();
    }

    [Fact]
    public void ScheduleWebhookUsesPortugueseNamesAndPreservesAmounts()
    {
        // Arrange
        var query = new ScheduleQuery(Status: ScheduleQueryStatusType.PROCESSED, Detail: "done", ScheduleQueryData: new Schedule(RequestId: Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(), MerchantCnpj: "22185894000174", OriginType: ScheduleQueryOriginType.ONLINE, UpdatedAt: "2026-10-02T15:00:00Z", Acquirers: [new SchedulePaymentAcquirer(Cnpj: "82413081000116", PaymentArrangements: [new SchedulePaymentArrangement(Code: "VCC", ReceivableUnits: [new ScheduleReceivableUnit(HolderCnpj: "22185894000174", SettlementDate: "2026-10-07", TotalAmount: 1234.56m, FreeAmount: 123.45m)])])]));
        var webhook = query.ToScheduleWebhook();

        // Act
        var unit = webhook.DadosConsultaAgenda!.Credenciadoras![0].ArranjosPagamento![0].UnidadesRecebiveis![0];

        // Assert
        (unit.ValorTotal).Should().Be(1234.56m);
        (unit.ValorLivre).Should().Be(123.45m);
        (webhook.ToJson()).Should().Contain("dadosConsultaAgenda");
        (webhook.ToJson()).Should().NotContain("scheduleQueryData");
    }

    [Fact]
    public void FingerprintIgnoresObjectPropertyOrder()
    {
        // Arrange
        var first = System.Text.Json.Nodes.JsonNode.Parse("{\"a\":1,\"b\":2}");

        // Act
        var second = System.Text.Json.Nodes.JsonNode.Parse("{\"b\":2,\"a\":1}");

        // Assert
        (second.Fingerprint()).Should().Be(first.Fingerprint());
    }

    [Fact]
    public void ScheduleGroupsPersistedSalesAndSubtractsPendingReservations()
    {
        // Arrange
        var operation = new ScheduledOperationEntity();
        var pending = new ScheduledOperationEntity
        {
            Status = ScheduleQueryStatusType.PROCESSING,
            RequestJson = Contract().ToJson()
        };
        var sales = new List<SimulatedSaleEntity>
        {
            new()
            {
                MerchantCnpj = "22185894000174",
                AcquirerCnpj = "82413081000116",
                PaymentArrangementCode = "VCC",
                SettlementDate = "2026-10-07",
                Amount = 123.45m
            },
            new()
            {
                MerchantCnpj = "22185894000174",
                AcquirerCnpj = "82413081000116",
                PaymentArrangementCode = "VCC",
                SettlementDate = "2026-10-07",
                Amount = 100m
            },
            new()
            {
                MerchantCnpj = "33185894000174",
                AcquirerCnpj = "82413081000116",
                PaymentArrangementCode = "VCC",
                SettlementDate = "2026-10-07",
                Amount = 999m
            },
            new()
            {
                MerchantCnpj = "22185894000174",
                AcquirerCnpj = "82413081000116",
                PaymentArrangementCode = "VCC",
                SettlementDate = "2026-10-01",
                Amount = 999m
            },
            new()
            {
                MerchantCnpj = "22185894000174",
                AcquirerCnpj = "82413081000116",
                PaymentArrangementCode = "MCC",
                SettlementDate = "2026-10-07",
                Amount = 999m
            }
        };

        // Act
        var agenda = new ScheduleGenerator(new BusinessCalendar(new FixedClock(), options)).Generate(operation, Merchant(), [pending], sales, new FixedClock().GetUtcNow().UtcDateTime);

        // Assert
        var unit = (((agenda.Acquirers!).Should().ContainSingle().Which.PaymentArrangements!).Should().ContainSingle().Which.ReceivableUnits!).Should().ContainSingle().Which;
        (unit.TotalAmount).Should().Be(223.45m);
        (unit.FreeAmount).Should().Be(123.45m);
    }

    [Fact]
    public void ScheduleWithoutSalesHasNoReceivableUnits()
    {
        // Arrange
        // Act
        var agenda = new ScheduleGenerator(new BusinessCalendar(new FixedClock(), options)).Generate(new ScheduledOperationEntity(), Merchant(), [], [], new FixedClock().GetUtcNow().UtcDateTime);

        // Assert
        (agenda.Acquirers!).Should().BeEmpty();
    }

    private static Merchant Merchant() => new()
    {
        Cnpj = "22185894000174",
        CorporateName = "Test",
        LimitType = LimitType.Transactional,
        ReceivablesScheduleConfig = new ReceivablesScheduleConfig(QueryWindow: QueryWindowType.P6M, AcquirerCnpjs: ["82413081000116"], ArrangementCodes: ["VCC"])
    };

    private static ContractAnticipationCreateRequest Contract() => new()
    {
        ContractorCnpj = "22185894000174",
        RequestedAmount = 100,
        SignatureDate = "2026-10-02",
        Guarantees = [new ContractAnticipationGuaranteeCreate(DefinedAmount: 100, FinalUserReceiverCnpj: "22185894000174", ReceivableUnitHolderCnpj: "22185894000174", AcquirerCnpj: "82413081000116", PaymentArrangementCode: "VCC", SettlementDate: "2026-10-07")]
    };
}
