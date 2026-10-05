using MVFC.Veragi.Simulator.Domain.Services.Reconciliation;
using MVFC.Veragi.Simulator.Shareable.Responses.Reconciliation;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Reconciliation.CreateExternalAnticipation;

public sealed class CreateExternalAnticipationHandler(ExternalAnticipationService service) : IRequestHandler<CreateExternalAnticipationCommand, Result<ExternalAnticipationResponse>>
{
    private readonly ExternalAnticipationService _service = service;

    public Task<Result<ExternalAnticipationResponse>> Handle(CreateExternalAnticipationCommand request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request.MerchantCnpj, request.Request, request.IdempotencyKey, cancellationToken);
}
