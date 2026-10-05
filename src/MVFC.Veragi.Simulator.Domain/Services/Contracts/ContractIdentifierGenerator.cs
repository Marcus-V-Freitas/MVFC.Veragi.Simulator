using System.Globalization;
using System.Security.Cryptography;

namespace MVFC.Veragi.Simulator.Domain.Services.Contracts;

public static class ContractIdentifierGenerator
{
    public static string Create(DateTime now) => string.Create(CultureInfo.InvariantCulture,
        $"CON/{RandomNumberGenerator.GetInt32(100_000_000):D8}/{RandomNumberGenerator.GetInt32(10_000_000):D7}{RandomNumberGenerator.GetInt32(10_000_000):D7}/{now:ddMMyy}/{now:HHmmss}");
}
