using OSK.Functions.Outputs;
using OSK.Functions.Outputs.Models;
using OSK.Security.Cryptography.Abstractions;
using OSK.Storage.Local.Cryptography.Ports;
using OSK.Storage.Local.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Cryptography.Internal.Services;

internal class CryptographySerializationDataProcessor(ICryptographicKeyServiceProvider keyServiceProvider, ICryptographicKeyRepository cryptographicKeyRepository) : ICryptographicRawDataProcessor
{
    #region IRawDataProcessor

    public async ValueTask<Output<byte[]>> ProcessPostSerializationAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        var keyService = await GetKeyService(cancellationToken);
        var encryptedData = await keyService.EncryptAsync(data, cancellationToken);
        return Out.Success(encryptedData);
    }

    public async ValueTask<Output<byte[]>> ProcessPreDeserializationAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        var keyService = await GetKeyService(cancellationToken);
        var decryptedData = await keyService.DecryptAsync(data, cancellationToken);
        return Out.Success(decryptedData);
    }

    #endregion

    #region Helpers

    private async ValueTask<ICryptographicKeyService> GetKeyService(CancellationToken cancellationToken)
    {
        var keyInformation = await cryptographicKeyRepository.GetCryptographicKeyAsync(cancellationToken);
        return keyServiceProvider.GetKeyService(keyInformation);
    }

    #endregion
}
