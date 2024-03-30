using OSK.Security.Cryptography.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Cryptography.Ports
{
    public interface ICryptographicKeyRepository
    {
        ValueTask<ICryptographicKeyInformation> GetCryptographicKeyAsync(CancellationToken cancellationToken = default);
    }
}
