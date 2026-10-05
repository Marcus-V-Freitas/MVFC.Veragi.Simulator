using FluentAssertions;
using MVFC.Veragi.Simulator.Shareable.Enums;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using Xunit;

namespace MVFC.Veragi.Simulator.Shareable.Tests.Extensions;

public sealed class WebhookReceiptMappingsTests
{
    [Fact]
    public void ToReceivedWebhookPortugueseWithDataMapsAllFields()
    {
        // Arrange
        var data = new ScheduleWebhookData(
            IdRequisicao: "req-123",
            TipoOrigem: ScheduleQueryOriginType.ONLINE,
            DataHoraAtualizacao: "2026-10-04T12:00:00Z",
            CnpjEstabelecimento: "12345678000195",
            Credenciadoras: []
        );
        var notification = new ScheduleWebhookNotification(
            Status: ScheduleQueryStatusType.PROCESSED,
            Detalhe: "Detalhe",
            DadosConsultaAgenda: data
        );

        // Act
        var received = notification.ToReceivedWebhook("schedule", "evt-1", "ref-1", "trace-1");

        // Assert
        received.Kind.Should().Be("schedule");
        received.EventKey.Should().Be("evt-1");
        received.ExternalReference.Should().Be("ref-1");
        received.MerchantCnpj.Should().Be("12345678000195");
        received.RequestId.Should().Be("req-123");
        received.BusinessStatus.Should().Be(ScheduleQueryStatusType.PROCESSED);
        received.TraceId.Should().Be("trace-1");
        received.Payload.Should().NotBeNull();
    }

    [Fact]
    public void ToReceivedWebhookPortugueseWithoutDataMapsNullFields()
    {
        // Arrange
        var notification = new ScheduleWebhookNotification(
            Status: ScheduleQueryStatusType.ERROR,
            Detalhe: "Falha",
            DadosConsultaAgenda: null
        );

        // Act
        var received = notification.ToReceivedWebhook("schedule", "evt-2", null, "trace-2");

        // Assert
        received.MerchantCnpj.Should().BeNull();
        received.RequestId.Should().BeNull();
        received.BusinessStatus.Should().Be(ScheduleQueryStatusType.ERROR);
        received.ExternalReference.Should().BeNull();
    }

    [Fact]
    public void ToReceivedWebhookEnglishWithDataMapsAllFields()
    {
        // Arrange
        var data = new ContractWebhookData(
            RequestId: "req-456",
            OriginType: ScheduleQueryOriginType.ONLINE,
            UpdatedAt: "2026-10-04T12:00:00Z",
            MerchantCnpj: "98765432000188",
            Acquirers: []
        );
        var notification = new ContractWebhookNotification(
            Status: ScheduleQueryStatusType.PROCESSED,
            Detail: "Success",
            ScheduleQueryData: data
        );

        // Act
        var received = notification.ToReceivedWebhook("contract", "evt-3", "ref-3", "trace-3");

        // Assert
        received.Kind.Should().Be("contract");
        received.EventKey.Should().Be("evt-3");
        received.ExternalReference.Should().Be("ref-3");
        received.MerchantCnpj.Should().Be("98765432000188");
        received.RequestId.Should().Be("req-456");
        received.BusinessStatus.Should().Be(ScheduleQueryStatusType.PROCESSED);
        received.TraceId.Should().Be("trace-3");
        received.Payload.Should().NotBeNull();
    }

    [Fact]
    public void ToReceivedWebhookEnglishWithoutDataMapsNullFields()
    {
        // Arrange
        var notification = new ContractWebhookNotification(
            Status: ScheduleQueryStatusType.ERROR,
            Detail: "Error",
            ScheduleQueryData: null!
        );

        // Act
        var received = notification.ToReceivedWebhook("contract", "evt-4", null, "trace-4");

        // Assert
        received.MerchantCnpj.Should().BeNull();
        received.RequestId.Should().BeNull();
        received.BusinessStatus.Should().Be(ScheduleQueryStatusType.ERROR);
        received.ExternalReference.Should().BeNull();
    }
}
