using MVFC.Veragi.Simulator.Domain.Services.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Requests.Simulation;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Contracts;

public sealed class ContractProcessingTests
{
    [Theory]
    [InlineData(ContractStatusType.Active, 100, 900, ScheduleQueryStatusType.PROCESSED)]
    [InlineData(ContractStatusType.Settled, 100, 900, ScheduleQueryStatusType.PROCESSED)]
    [InlineData(ContractStatusType.Cancelled, 0, 1000, ScheduleQueryStatusType.ERROR)]
    [InlineData(ContractStatusType.ContractSimulation, 0, 1000, ScheduleQueryStatusType.PROCESSED)]
    [InlineData(ContractStatusType.PendingEdit, 0, 1000, ScheduleQueryStatusType.PROCESSING)]
    [InlineData(ContractStatusType.PendingRegistration, 0, 1000, ScheduleQueryStatusType.PROCESSING)]
    public async Task StatusDeterminesReachedAmountAndAutomaticAgendaUpdate(
        ContractStatusType status,
        decimal reached,
        decimal free,
        ScheduleQueryStatusType operationStatus
    )
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var scheduleId = await fixture.PrepareAsync();
        await fixture.ScenarioService.ConfigureAsync("contract-process", new SimulationScenarioRequest(ContractStatuses: [status]), CancellationToken.None);
        await fixture.ContractService.CreateAsync(fixture.Contract(), ServiceFixture.Key(), CancellationToken.None);

        // Act
        var result = await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var contract = fixture.Operations.Single(x => x.Kind == "contract");
        contract.Status.Should().Be(operationStatus);
        contract.ResultJson.FromJson<ContractByExternalReference>()!.ReachedTotal().Should().Be(reached);
        var schedule = (await fixture.ScheduleService.GetAsync(scheduleId.ToString(), CancellationToken.None)).Value;
        schedule!.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].FreeAmount.Should().Be(free);
        fixture.Deliveries.Count(x => x.Kind == "schedule").Should().Be(reached > 0 ? 2 : 1);
        fixture.Deliveries.Select(x => x.Id.Version).Should().OnlyContain(x => x == 7);
    }

    [Fact]
    public async Task RegistrationUsesAvailableBalanceAfterExternalAnticipation()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var scheduleId = await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(1000), ServiceFixture.Key(), CancellationToken.None);
        var date = fixture.Contract().Guarantees![0].SettlementDate;
        var external = new ExternalAnticipationService(fixture.Store, fixture.Gate);
        await external.CreateAsync(MockEntities.MerchantCnpj, new ExternalAnticipationRequest(MockEntities.AcquirerCnpj, MockEntities.ArrangementCode, date, 750), ServiceFixture.Key(), CancellationToken.None);

        // Act
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        fixture.Operations.Single(x => x.Kind == "contract").ResultJson.FromJson<ContractByExternalReference>()!.ReachedTotal().Should().Be(250);
        var schedule = (await fixture.ScheduleService.GetAsync(scheduleId.ToString(), CancellationToken.None)).Value;
        schedule!.ScheduleQueryData!.Acquirers![0].PaymentArrangements![0].ReceivableUnits![0].FreeAmount.Should().Be(0);
    }

    [Theory]
    [InlineData("validation")]
    [InlineData("idempotency")]
    [InlineData("missing")]
    [InlineData("deleted")]
    [InlineData("sum")]
    [InlineData("credit")]
    [InlineData("account")]
    [InlineData("agenda")]
    [InlineData("unit")]
    public async Task InvalidContractDoesNotCreateAnOperation(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var request = fixture.Contract();
        var key = ServiceFixture.Key();
        var state = fixture.Merchants.Single();
        switch (failure)
        {
            case "validation":
                request = request with
                {
                    Guarantees = null
                };
                break;
            case "idempotency":
                key = "invalid";
                break;
            case "missing":
                fixture.Merchants.Clear();
                break;
            case "deleted":
                state.IsDeleted = true;
                break;
            case "sum":
                request = request with
                {
                    RequestedAmount = 200
                };
                break;
            case "credit":
                state.Payload = (state.Payload.FromJson<Merchant>()! with
                {
                    CreditConfigurations = [CreditConfigurationType.AutomaticAnticipation]
                }

                ).ToJson();
                break;
            case "account":
                state.Payload = (state.Payload.FromJson<Merchant>()! with
                {
                    AnticipationSettlementAccount = null
                }

                ).ToJson();
                break;
            case "agenda":
                fixture.Operations.Clear();
                break;
            case "unit":
                request = request with
                {
                    Guarantees = [request.Guarantees![0] with
                    {
                        PaymentArrangementCode = "MCC"
                    }

                    ]
                };
                break;
        }

        fixture.Store.ClearReceivedCalls();

        // Act
        var result = await fixture.ContractService.CreateAsync(request, key, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Store.DidNotReceive().AddOperation(Arg.Any<ScheduledOperationEntity>());
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("acquirers")]
    [InlineData("acquirer-mismatch")]
    [InlineData("arrangements")]
    [InlineData("units")]
    [InlineData("holder")]
    public async Task ContractCreationRejectsProcessedScheduleWithoutCompleteReceivableHierarchy(string shape)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        var operation = fixture.Operations.Single(x => x.Kind == "schedule");
        var current = operation.ResultJson.FromJson<ScheduleQuery>()!;
        var contract = fixture.Contract();
        var guarantee = contract.Guarantees!.Single();
        var scheduleData = shape switch
        {
            "acquirers" => current.ScheduleQueryData! with
            {
                Acquirers = null
            },
            "acquirer-mismatch" => current.ScheduleQueryData! with
            {
                Acquirers = new List<SchedulePaymentAcquirer>
                {
                    new(Cnpj: "33185894000174", PaymentArrangements: new List<SchedulePaymentArrangement>
                    {
                        new(Code: guarantee.PaymentArrangementCode, ReceivableUnits: new List<ScheduleReceivableUnit>
                        {
                            new(SettlementDate: guarantee.SettlementDate, HolderCnpj: guarantee.ReceivableUnitHolderCnpj, TotalAmount: 100, FreeAmount: 100)
                        })
                    })
                }
            },
            "arrangements" => current.ScheduleQueryData! with
            {
                Acquirers = new List<SchedulePaymentAcquirer>
                {
                    new(Cnpj: guarantee.AcquirerCnpj, PaymentArrangements: null)
                }
            },
            "holder" => current.ScheduleQueryData! with
            {
                Acquirers = new List<SchedulePaymentAcquirer>
                {
                    new(Cnpj: guarantee.AcquirerCnpj, PaymentArrangements: new List<SchedulePaymentArrangement>
                    {
                        new(Code: guarantee.PaymentArrangementCode, ReceivableUnits: new List<ScheduleReceivableUnit>
                        {
                            new(SettlementDate: guarantee.SettlementDate, HolderCnpj: "33185894000174", TotalAmount: 100, FreeAmount: 100)
                        })
                    })
                }
            },
            _ => current.ScheduleQueryData! with
            {
                Acquirers = new List<SchedulePaymentAcquirer>
                {
                    new(Cnpj: guarantee.AcquirerCnpj, PaymentArrangements: new List<SchedulePaymentArrangement>
                    {
                        new(Code: guarantee.PaymentArrangementCode, ReceivableUnits: null)
                    })
                }
            }
        };
        operation.ResultJson = (current with
        {
            ScheduleQueryData = scheduleData
        }).ToJson();
        fixture.Store.ClearReceivedCalls();

        // Act
        var result = await fixture.ContractService.CreateAsync(contract, ServiceFixture.Key(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        ((SimulationFailureException)result.Exception!).Message.Should().Contain("Guarantee must reference a receivable unit");
        fixture.Store.DidNotReceive().AddOperation(Arg.Any<ScheduledOperationEntity>());
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DeletedMerchantCancelsPendingOperationsAndDoesNotDebitAgenda()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ContractService.CreateAsync(fixture.Contract(), ServiceFixture.Key(), CancellationToken.None);
        fixture.Merchants.Single().IsDeleted = true;

        // Act
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        fixture.Operations.Single(x => x.Kind == "contract").Status.Should().Be(ScheduleQueryStatusType.ERROR);
        fixture.Operations.Single(x => x.Kind == "contract").ResultJson.FromJson<ContractByExternalReference>()!.Status.Should().Be(ContractStatusType.Cancelled);
        fixture.Deliveries.Count(x => x.Kind == "schedule").Should().Be(1);
    }

    [Fact]
    public async Task RepeatedPendingStatusDoesNotCreateAnotherEventAndCanBeReleased()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        await fixture.PrepareAsync();
        await fixture.ScenarioService.ConfigureAsync("contract-process", new SimulationScenarioRequest(HoldProcessing: true), CancellationToken.None);
        await fixture.ContractService.CreateAsync(fixture.Contract(), ServiceFixture.Key(), CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Act
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);

        // Assert
        fixture.Deliveries.Count(x => x.Kind == "contract").Should().Be(1);
        await fixture.ScenarioService.ClearAsync("contract-process", null, CancellationToken.None);
        await fixture.Processor.ProcessAsync("contract", true, CancellationToken.None);
        fixture.Deliveries.Count(x => x.Kind == "contract").Should().Be(2);
        fixture.Operations.Single(x => x.Kind == "contract").Status.Should().Be(ScheduleQueryStatusType.PROCESSED);
    }
}
