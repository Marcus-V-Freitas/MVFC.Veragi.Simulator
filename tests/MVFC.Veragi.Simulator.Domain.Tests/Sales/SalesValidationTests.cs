using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Requests.Sales;
using FluentAssertions;
using NSubstitute;
using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;
using MVFC.Veragi.Simulator.Domain.Tests.Infrastructure;

namespace MVFC.Veragi.Simulator.Domain.Tests.Sales;

public sealed class SalesValidationTests
{
    [Theory]
    [InlineData("cnpj")]
    [InlineData("key")]
    [InlineData("missing")]
    [InlineData("deleted")]
    [InlineData("days-small")]
    [InlineData("days-large")]
    [InlineData("sales-small")]
    [InlineData("sales-large")]
    [InlineData("amount-small")]
    [InlineData("amount-large")]
    [InlineData("amount-precision")]
    [InlineData("date-format")]
    [InlineData("weekend")]
    [InlineData("holiday")]
    [InlineData("past")]
    [InlineData("empty-acquirers")]
    [InlineData("empty-arrangements")]
    [InlineData("duplicate-acquirers")]
    [InlineData("duplicate-arrangements")]
    [InlineData("acquirer-scope")]
    [InlineData("arrangement-scope")]
    [InlineData("batch-limit")]
    [InlineData("empty-sales")]
    [InlineData("null-sale")]
    [InlineData("null-installments")]
    [InlineData("empty-installments")]
    [InlineData("invalid-installment-amount")]
    [InlineData("null-installment")]
    [InlineData("explicit-acquirer")]
    [InlineData("explicit-arrangement")]
    [InlineData("external-empty")]
    [InlineData("external-length")]
    [InlineData("external-duplicate")]
    [InlineData("date-both")]
    [InlineData("date-neither")]
    [InlineData("date-invalid")]
    [InlineData("explicit-past")]
    [InlineData("explicit-future")]
    [InlineData("business-negative")]
    [InlineData("business-large")]
    [InlineData("explicit-count")]
    public async Task InvalidBatchDoesNotPersistAnySale(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        fixture.Merchants.Add(MockEntities.MerchantEntity());
        var cnpj = MockEntities.MerchantCnpj;
        var key = ServiceFixture.Key();
        var request = new GenerateSalesRequest(Days: 1, SalesPerDay: 1);
        var installment = new SaleInstallmentRequest(Amount: 100, SettlementBusinessDays: 5);
        var sale = new SaleCreateRequest(MockEntities.AcquirerCnpj, MockEntities.ArrangementCode, "sale-1", [installment]);
        switch (failure)
        {
            case "cnpj":
                cnpj = "invalid";
                break;
            case "key":
                key = "invalid";
                break;
            case "missing":
                fixture.Merchants.Clear();
                break;
            case "deleted":
                fixture.Merchants[0].IsDeleted = true;
                break;
            case "days-small":
                request = request with
                {
                    Days = 0
                };
                break;
            case "days-large":
                request = request with
                {
                    Days = 366
                };
                break;
            case "sales-small":
                request = request with
                {
                    SalesPerDay = 0
                };
                break;
            case "sales-large":
                request = request with
                {
                    SalesPerDay = 1001
                };
                break;
            case "amount-small":
                request = request with
                {
                    AmountPerSale = 0
                };
                break;
            case "amount-large":
                request = request with
                {
                    AmountPerSale = 10000001
                };
                break;
            case "amount-precision":
                request = request with
                {
                    AmountPerSale = 1.001m
                };
                break;
            case "date-format":
                request = request with
                {
                    StartSettlementDate = "invalid"
                };
                break;
            case "weekend":
                request = request with
                {
                    StartSettlementDate = "2026-10-03"
                };
                break;
            case "holiday":
                fixture.Options = fixture.Options with { Holidays = ["2026-10-05"] };
                request = request with
                {
                    StartSettlementDate = "2026-10-05"
                };
                break;
            case "past":
                request = request with
                {
                    StartSettlementDate = "2026-10-01"
                };
                break;
            case "empty-acquirers":
                request = request with
                {
                    AcquirerCnpjs = []
                };
                break;
            case "empty-arrangements":
                request = request with
                {
                    ArrangementCodes = []
                };
                break;
            case "duplicate-acquirers":
                request = request with
                {
                    AcquirerCnpjs = [MockEntities.AcquirerCnpj, MockEntities.AcquirerCnpj]
                };
                break;
            case "duplicate-arrangements":
                request = request with
                {
                    ArrangementCodes = ["VCC", "VCC"]
                };
                break;
            case "acquirer-scope":
                request = request with
                {
                    AcquirerCnpjs = ["01027058000191"]
                };
                break;
            case "arrangement-scope":
                request = request with
                {
                    ArrangementCodes = ["MCC"]
                };
                break;
            case "batch-limit":
                request = request with
                {
                    Days = 10,
                    SalesPerDay = 1000
                };
                break;
            case "empty-sales":
                request = request with
                {
                    Sales = []
                };
                break;
            case "null-sale":
                request = request with
                {
                    Sales = [null!]
                };
                break;
            case "null-installments":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = null
                    }

                    ]
                };
                break;
            case "empty-installments":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = []
                    }, sale with
                    {
                        ExternalId = "sale-2"
                    }

                    ]
                };
                break;
            case "invalid-installment-amount":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [installment with
                        {
                            Amount = 0
                        }

                        ]
                    }

                    ]
                };
                break;
            case "null-installment":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [null !]
                    }

                    ]
                };
                break;
            case "explicit-acquirer":
                request = request with
                {
                    Sales = [sale with
                    {
                        AcquirerCnpj = "01027058000191"
                    }

                    ]
                };
                break;
            case "explicit-arrangement":
                request = request with
                {
                    Sales = [sale with
                    {
                        PaymentArrangementCode = "MCC"
                    }

                    ]
                };
                break;
            case "external-empty":
                request = request with
                {
                    Sales = [sale with
                    {
                        ExternalId = " "
                    }

                    ]
                };
                break;
            case "external-length":
                request = request with
                {
                    Sales = [sale with
                    {
                        ExternalId = new string ('a', 65)
                    }

                    ]
                };
                break;
            case "external-duplicate":
                request = request with
                {
                    Sales = [sale, sale]
                };
                break;
            case "date-both":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [installment with
                        {
                            SettlementDate = "2026-10-09"
                        }

                        ]
                    }

                    ]
                };
                break;
            case "date-neither":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [installment with
                        {
                            SettlementBusinessDays = null
                        }

                        ]
                    }

                    ]
                };
                break;
            case "date-invalid":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [new SaleInstallmentRequest(100, "invalid")]
                    }

                    ]
                };
                break;
            case "explicit-past":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [new SaleInstallmentRequest(100, "2026-10-01")]
                    }

                    ]
                };
                break;
            case "explicit-future":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [new SaleInstallmentRequest(100, "2031-10-03")]
                    }

                    ]
                };
                break;
            case "business-negative":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [installment with
                        {
                            SettlementBusinessDays = -1
                        }

                        ]
                    }

                    ]
                };
                break;
            case "business-large":
                request = request with
                {
                    Sales = [sale with
                    {
                        Installments = [installment with
                        {
                            SettlementBusinessDays = 731
                        }

                        ]
                    }

                    ]
                };
                break;
            case "explicit-count":
                request = request with
                {
                    Sales = [.. Enumerable.Repeat(sale with { ExternalId = null }, 5001)]
                };
                break;
        }

        // Act
        var result = await fixture.SalesService.GenerateAsync(cnpj, request, key, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        fixture.Store.DidNotReceive().AddSale(Arg.Any<SimulatedSaleEntity>());
        fixture.Store.DidNotReceive().AddSalesBatch(Arg.Any<SalesBatchEntity>());
        await fixture.Store.DidNotReceive().SaveAsync(CancellationToken.None);
    }

    [Fact]
    public async Task WildcardsSelectCatalogAndIdempotencyDoesNotGenerateAnotherBatch()
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var merchant = MockEntities.MerchantEntity();
        merchant.Payload = (MockEntities.Merchant() with
        {
            ReceivablesScheduleConfig = new ReceivablesScheduleConfig(QueryWindowType.P6M, ["99999999000199"], ["999"])
        }

        ).ToJson();
        fixture.Merchants.Add(merchant);
        var request = new GenerateSalesRequest(Days: 1, SalesPerDay: 1);
        var key = ServiceFixture.Key();
        var first = await fixture.SalesService.GenerateAsync(MockEntities.MerchantCnpj, request, key, CancellationToken.None);
        var duplicate = await fixture.SalesService.GenerateAsync(MockEntities.MerchantCnpj, request, key, CancellationToken.None);

        // Act
        var conflict = await fixture.SalesService.GenerateAsync(MockEntities.MerchantCnpj, request with { AmountPerSale = 100 }, key, CancellationToken.None);

        // Assert
        duplicate.Value.Should().Be(first.Value);
        conflict.IsSuccess.Should().BeFalse();
        fixture.Batches.Should().ContainSingle();
        fixture.Sales.Should().HaveCount(12);
        fixture.Operations.Should().BeEmpty();
        fixture.Deliveries.Should().BeEmpty();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("missing")]
    [InlineData("deleted")]
    public async Task ListingSalesValidatesMerchant(string failure)
    {
        // Arrange
        using var fixture = new ServiceFixture();
        var cnpj = failure == "invalid" ? "invalid" : MockEntities.MerchantCnpj;

        if (failure == "deleted")
            fixture.Merchants.Add(new MerchantEntity { Cnpj = cnpj, IsDeleted = true });

        // Act
        var result = await fixture.SalesService.ListAsync(cnpj, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
