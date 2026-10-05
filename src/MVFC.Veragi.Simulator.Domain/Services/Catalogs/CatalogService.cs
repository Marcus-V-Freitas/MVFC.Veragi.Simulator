using MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;
using OperationResult;
using MVFC.Veragi.Simulator.Shareable.Extensions;
using MVFC.Veragi.Simulator.Shareable.Results;

namespace MVFC.Veragi.Simulator.Domain.Services.Catalogs;

public sealed class CatalogService
{
    public IReadOnlyList<Acquirer> Acquirers { get; } = [new()
    {
        Cnpj = "01027058000191",
        CorporateName = "CIELO S.A.",
    }, new()
    {
        Cnpj = "82413081000116",
        CorporateName = "REDE",
    }, new()
    {
        Cnpj = "16501555000157",
        CorporateName = "STONE",
    },
    ];

    public IReadOnlyList<PaymentArrangement> Arrangements { get; } = [new()
    {
        Code = "VCC",
        Description = "Visa Crédito",
    }, new()
    {
        Code = "MCC",
        Description = "Mastercard Crédito",
    }, new()
    {
        Code = "ACC",
        Description = "American Express Crédito",
    }, new()
    {
        Code = "DCC",
        Description = "Diners Crédito",
    },
    ];

    public Result<IReadOnlyList<Acquirer>> GetAcquirers(string? cnpj) =>
        cnpj is not null && !IsCnpj(cnpj) ?
            Failures.Validation("Invalid CNPJ", "cnpj") :
            Result.Success<IReadOnlyList<Acquirer>>([.. Acquirers.Where(x => cnpj is null || x.Cnpj == cnpj)]);

    public Result<IReadOnlyList<PaymentArrangement>> GetArrangements(string? code) =>
        code is not null && !InputFormats.IsArrangement(code) ?
            Failures.Validation("Invalid arrangement code", "arrangementCode") :
            Result.Success<IReadOnlyList<PaymentArrangement>>([.. Arrangements.Where(x => code is null || x.Code == code)]);

    public static bool IsCnpj(string value) => InputFormats.IsCnpj(value);
}
