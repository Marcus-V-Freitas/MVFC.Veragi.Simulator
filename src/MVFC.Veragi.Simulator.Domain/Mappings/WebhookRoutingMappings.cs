using MVFC.Veragi.Simulator.Domain.Entities;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class WebhookRoutingMappings
{
    public static WebhookDestinationsResponse ToResponse(this WebhookRoutingEntity? routing) =>
        new(routing?.ScheduleUrl ?? string.Empty, routing?.ContractUrl ?? string.Empty, routing is not null);
}
