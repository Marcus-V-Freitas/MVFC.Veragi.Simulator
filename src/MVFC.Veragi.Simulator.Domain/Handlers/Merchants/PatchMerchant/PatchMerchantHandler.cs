using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Merchants.PatchMerchant;

public sealed class PatchMerchantHandler(MerchantService merchant) : IRequestHandler<PatchMerchantCommand, Result<Merchant>>
{
    private readonly MerchantService _merchant = merchant;

    public Task<Result<Merchant>> Handle(PatchMerchantCommand command, CancellationToken cancellationToken) =>
        _merchant.PatchAsync(command.Cnpj, command.Request, cancellationToken);
}
