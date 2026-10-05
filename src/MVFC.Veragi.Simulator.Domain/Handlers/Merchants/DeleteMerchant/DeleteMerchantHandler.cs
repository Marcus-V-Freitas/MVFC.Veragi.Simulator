using MVFC.Veragi.Simulator.Domain.Services.Merchants;
using MediatR;
using OperationResult;

namespace MVFC.Veragi.Simulator.Domain.Handlers.Merchants.DeleteMerchant;

public sealed class DeleteMerchantHandler(MerchantService merchant) : IRequestHandler<DeleteMerchantCommand, Result<bool>>
{
    private readonly MerchantService _merchant = merchant;

    public Task<Result<bool>> Handle(DeleteMerchantCommand command, CancellationToken cancellationToken) =>
        _merchant.DeleteAsync(command.Cnpj, cancellationToken);
}
