using FluentAssertions;
using MVFC.Veragi.Simulator.Domain.Mappings;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.TestHelpers;
using Xunit;

namespace MVFC.Veragi.Simulator.Domain.Tests.Webhooks;

public sealed class WebhookMetadataMappingTests
{
    [Fact]
    public void PendingScheduleWebhookReportsProcessingAndPreservesCorrelation()
    {
        // Arrange
        var requestId = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var query = new ScheduleQuery(ScheduleQueryStatusType.PROCESSING, "Processing",
            new Schedule(requestId, ScheduleQueryOriginType.ONLINE,
                DateTimeOffset.UtcNow.ToString("O"), MockEntities.MerchantCnpj));

        // Act
        var webhook = query.ToScheduleWebhook();

        // Assert
        webhook.Status.Should().Be(ScheduleQueryStatusType.PROCESSING);
        webhook.Detalhe.Should().Be("A solicitação de consulta de agenda está em processamento.");
        webhook.DadosConsultaAgenda!.IdRequisicao.Should().Be(requestId);
        webhook.DadosConsultaAgenda.CnpjEstabelecimento.Should().Be(MockEntities.MerchantCnpj);
        webhook.DadosConsultaAgenda.Credenciadoras.Should().BeNull();
    }

    [Fact]
    public void ContractReceivablesErrorWebhookPreservesMissingAgendaInsteadOfCreatingEmptyReceivables()
    {
        // Arrange
        var query = new ScheduleQuery(ScheduleQueryStatusType.ERROR, "Query failed",
            new Schedule(Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(), ScheduleQueryOriginType.ONLINE,
                DateTimeOffset.UtcNow.ToString("O"), MockEntities.MerchantCnpj));

        // Act
        var webhook = query.ToContractReceivablesWebhook();

        // Assert
        webhook.Status.Should().Be(ScheduleQueryStatusType.ERROR);
        webhook.ScheduleQueryData.Acquirers.Should().BeNull();
        webhook.ScheduleQueryData.RequestId.Should().Be(query.ScheduleQueryData!.RequestId);
    }

    [Fact]
    public void PortugueseWebhookWithoutDataKeepsMissingCorrelationMetadata()
    {
        // Arrange
        var request = new ScheduleWebhookNotification(ScheduleQueryStatusType.ERROR, "Missing agenda");
        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();

        // Act
        var received = request.ToReceivedWebhook("schedule", key, null, "trace-test");

        // Assert
        received.MerchantCnpj.Should().BeNull();
        received.RequestId.Should().BeNull();
        received.BusinessStatus.Should().Be(ScheduleQueryStatusType.ERROR);
        received.EventKey.Should().Be(key);
        received.Payload["dadosConsultaAgenda"].Should().BeNull();
    }

    [Fact]
    public void EnglishWebhookWithoutDataKeepsMissingCorrelationMetadata()
    {
        // Arrange
        var request = "{\"status\":\"ERROR\",\"detail\":\"Missing agenda\"}".FromJson<ContractWebhookNotification>()!;
        var key = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();

        // Act
        var received = request.ToReceivedWebhook("contract", key, "SIM-reference", "trace-test");

        // Assert
        received.MerchantCnpj.Should().BeNull();
        received.RequestId.Should().BeNull();
        received.BusinessStatus.Should().Be(ScheduleQueryStatusType.ERROR);
        received.ExternalReference.Should().Be("SIM-reference");
        received.Payload["scheduleQueryData"].Should().BeNull();
    }

    [Theory]
    [InlineData("{\"status\":\"ERROR\",\"dadosConsultaAgenda\":{}}")]
    [InlineData("{\"status\":\"ERROR\",\"scheduleQueryData\":{}}")]
    public void DiagnosticReceiptWithoutCorrelationFieldsPreservesPayloadAndUsesEmptyMetadata(string payload)
    {
        // Arrange
        var key = "schedule:" + Guid.CreateVersion7(DateTimeOffset.UtcNow);
        var receivedAt = DateTime.UtcNow;

        // Act
        var receipt = payload.ToReceipt("schedule", key, "", receivedAt);

        // Assert
        receipt.MerchantCnpj.Should().BeEmpty();
        receipt.RequestId.Should().BeEmpty();
        receipt.EventKey.Should().Be(key);
        receipt.Payload.Should().Be(payload);
        receipt.ReceivedAt.Should().Be(receivedAt);
        receipt.Id.Version.Should().Be(7);
    }
}
