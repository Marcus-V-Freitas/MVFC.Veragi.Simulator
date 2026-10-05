using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Ports;

public interface IWebhookSender
{
    Task<Result<int>> SendAsync(string kind, string eventKey, string payload, string externalReference, string destinationUrl, CancellationToken cancellationToken);
}
