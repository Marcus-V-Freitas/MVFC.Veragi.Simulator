using MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.CreateExternalAnticipation;

public sealed record CreateExternalAnticipationCommand(
    string MerchantCnpj,
    ExternalAnticipationRequest Request,
    string IdempotencyKey
) : IRequest<Result<ExternalAnticipationResponse>>;
