using OSK.Hexagonal.MetaData;
using OSK.Security.Cryptography.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Cryptography.Ports
{
    /// <summary>
    /// A key repository that provides a cryptographic key to a cryptographic data processor when data manipulations are happening prior to storage"/>
    /// </summary>
    [HexagonalIntegration(HexagonalIntegrationType.ConsumerRequired)]
    public interface ICryptographicKeyRepository
    {
        ValueTask<ICryptographicKeyInformation> GetCryptographicKeyAsync(CancellationToken cancellationToken = default);
    }
}
