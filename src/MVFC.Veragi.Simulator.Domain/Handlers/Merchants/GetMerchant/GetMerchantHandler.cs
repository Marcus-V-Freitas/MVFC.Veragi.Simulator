using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Merchants.GetMerchant;

public sealed class GetMerchantHandler(MerchantService merchant) : IRequestHandler<GetMerchantCommand, Result<Merchant>>
{
    private readonly MerchantService _merchant = merchant;

    public Task<Result<Merchant>> Handle(GetMerchantCommand command, CancellationToken cancellationToken) =>
        _merchant.GetAsync(command.Cnpj, cancellationToken);
}
