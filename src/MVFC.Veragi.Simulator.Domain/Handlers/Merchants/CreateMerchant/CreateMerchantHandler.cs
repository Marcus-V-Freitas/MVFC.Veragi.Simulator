using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Merchants.CreateMerchant;

public sealed class CreateMerchantHandler(MerchantService merchant) : IRequestHandler<CreateMerchantCommand, Result<Merchant>>
{
    private readonly MerchantService _merchant = merchant;

    public Task<Result<Merchant>> Handle(CreateMerchantCommand command, CancellationToken cancellationToken) =>
        _merchant.CreateAsync(command.Request, cancellationToken);
}
