using System.Text.Json.Nodes;
using FluentAssertions;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Requests.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Contracts;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Shareable.Tests.Serialization;

public sealed class SchemaContractTests
{
    [Fact]
    public void StandardValuesSerializeWithDocumentedTextOrNumericRepresentation()
    {
        // Arrange
        var merchant = MockEntities.MerchantRequest();
        var query = new ScheduleQuery(Status: ScheduleQueryStatusType.PROCESSED);
        var contract = new ContractByExternalReference(Status: ContractStatusType.Active, EffectType: EffectType.OwnershipTransfer);
        var merchantJson = JsonNode.Parse(merchant.ToJson())!;
        var queryJson = JsonNode.Parse(query.ToJson())!;

        // Act
        var contractJson = JsonNode.Parse(contract.ToJson())!;

        // Assert
        merchantJson["limitType"]!.GetValue<int>().Should().Be(1);
        merchantJson["creditConfigurations"]![0]!.GetValue<int>().Should().Be(2);
        merchantJson["receivablesScheduleConfig"]!["queryWindow"]!.GetValue<string>().Should().Be("P6M");
        merchantJson["anticipationSettlementAccount"]!["accountType"]!.GetValue<string>().Should().Be("CONTA_DEPOSITO_A_VISTA");
        queryJson["status"]!.GetValue<string>().Should().Be("PROCESSED");
        contractJson["status"]!.GetValue<int>().Should().Be(1);
        contractJson["effectType"]!.GetValue<int>().Should().Be(1);
        merchant.ToJson().FromJson<MerchantCreateRequest>()!.Should().BeEquivalentTo(merchant);
    }

    [Fact]
    public void FingerprintPreservesArrayOrderAndDistinguishesNullFromEmptyObject()
    {
        // Arrange
        var first = JsonNode.Parse("{\"values\":[1,null,{\"b\":2,\"a\":1}]}");
        var reorderedProperties = JsonNode.Parse("{\"values\":[1,null,{\"a\":1,\"b\":2}]}");
        var reorderedItems = JsonNode.Parse("{\"values\":[null,1,{\"a\":1,\"b\":2}]}");

        // Act
        var fingerprint = first.Fingerprint();

        // Assert
        fingerprint.Should().Be(reorderedProperties.Fingerprint());
        fingerprint.Should().NotBe(reorderedItems.Fingerprint());
        ((JsonNode?)null).Fingerprint().Should().NotBe(new JsonObject().Fingerprint());
    }
}
