using System.Text.Json.Nodes;
using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Services.Catalogs;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;
using MVFC.Veragi.Simulator.Shareable.Responses.Common;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Shareable.Tests.Serialization;

public sealed class DocumentExamplesTests
{
    [Fact]
    public void GenericEnvelopeCanBeReadByNamedMerchantAndCatalogResponseContracts()
    {
        // Arrange
        var merchant = MockEntities.Merchant();
        var catalog = new CatalogService();
        var metadata = new ApiMetadataResponse("v1", 0);
        var merchantEnvelope = new ApiResponse<Merchant>("Request processed", DateTime.UtcNow, "trace-test", merchant, metadata).ToJson();
        var acquirersEnvelope = new ApiResponse<IReadOnlyList<Acquirer>>("Request processed", DateTime.UtcNow, "trace-test", catalog.Acquirers, metadata).ToJson();
        var arrangementsEnvelope = new ApiResponse<IReadOnlyList<PaymentArrangement>>("Request processed", DateTime.UtcNow, "trace-test", catalog.Arrangements, metadata).ToJson();

        // Act
        var created = merchantEnvelope.FromJson<MerchantCreateResponse>()!;
        var retrieved = merchantEnvelope.FromJson<MerchantGetByCnpjResponse>()!;
        var acquirers = acquirersEnvelope.FromJson<AcquirersListResponse>()!;
        var arrangements = arrangementsEnvelope.FromJson<PaymentArrangementsListResponse>()!;

        // Assert
        created.Data!.Cnpj.Should().Be(MockEntities.MerchantCnpj);
        retrieved.Data!.Cnpj.Should().Be(MockEntities.MerchantCnpj);
        created.Metadata!["apiVersion"]!.GetValue<string>().Should().Be("v1");
        acquirers.Data.Should().HaveCount(3);
        arrangements.Data.Should().HaveCount(4);
    }

    [Fact]
    public void ConflictMetadataPreservesDocumentedLocationAndValue()
    {
        // Arrange
        var conflict = new ConflictItem("CONFLICT", "Conflict", "Account already exists", "body", "$.releaseAccounts", JsonValue.Create("9999"));
        var problem = new ProblemDetails(Status: 409, Conflicts: [conflict]);

        // Act
        var json = JsonNode.Parse(problem.ToJson())!;

        // Assert
        json["conflicts"]![0]!["value"]!.GetValue<string>().Should().Be("9999");
        json["conflicts"]![0]!["path"]!.GetValue<string>().Should().Be("$.releaseAccounts");
    }
}
