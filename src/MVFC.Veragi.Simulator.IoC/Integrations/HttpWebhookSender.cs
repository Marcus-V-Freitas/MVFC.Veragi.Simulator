using System.Text;
using Microsoft.Extensions.Logging;
using OperationResult;
using MVFC.Veragi.Simulator.Domain.Ports;
using MVFC.Veragi.Simulator.Shareable.Logging;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.IoC.Integrations;

public sealed class HttpWebhookSender(HttpClient client, ILogger<HttpWebhookSender> logger) : IWebhookSender
{
    private readonly HttpClient _client = client;
    private readonly ILogger<HttpWebhookSender> _logger = logger;

    public async Task<Result<int>> SendAsync(
        string kind,
        string eventKey,
        string payload,
        string externalReference,
        string destinationUrl,
        CancellationToken cancellationToken
    )
    {
        if (!Uri.TryCreate(destinationUrl, UriKind.Absolute, out var uri) || uri.Scheme is not "http" and not "https")
            return new SimulationFailureException(0, "WEBHOOK_NOT_CONFIGURED", "Webhook URL is not configured");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Add("Idempotency-Key", eventKey);
            request.Headers.Add("X-Simulator-Event-Type", kind);

            if (kind == "contract")
                request.Headers.Add("X-Contract-External-Reference", externalReference);

            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await _client.SendAsync(request, cancellationToken);
            _logger.LogDeliveryAttempted(kind, eventKey, (int)response.StatusCode);

            return response.IsSuccessStatusCode ? Result.Success((int)response.StatusCode) : new SimulationFailureException((int)response.StatusCode, "WEBHOOK_REJECTED", "Partner rejected webhook");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogDeliveryFailed(kind, eventKey, exception.GetType().Name);

            return new SimulationFailureException(0, "WEBHOOK_NETWORK_FAILURE", "Webhook network failure");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new SimulationFailureException(0, "WEBHOOK_TIMEOUT", "Webhook request timed out");
        }
    }
}
