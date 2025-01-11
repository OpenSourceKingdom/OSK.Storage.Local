using OSK.Functions.Outputs.Abstractions;
using OSK.Functions.Outputs.Logging.Abstractions;
using OSK.Security.Cryptography.Abstractions;
using OSK.Storage.Local.Cryptography.Ports;
using OSK.Storage.Local.Ports;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Cryptography.Internal.Services
{
    internal class CryptographySerializationDataProcessor : ICryptographicRawDataProcessor
    {
        #region Variables

        private readonly ICryptographicKeyServiceProvider _keyServiceProvider;
        private readonly ICryptographicKeyRepository _cryptographicKeyRepository;
        private readonly IOutputFactory<CryptographySerializationDataProcessor> _outputFactory;

        #endregion

        #region Constructors

        public CryptographySerializationDataProcessor(ICryptographicKeyServiceProvider keyServiceProvider,
            ICryptographicKeyRepository cryptographicKeyRepository,
            IOutputFactory<CryptographySerializationDataProcessor> outputFactory)
        {
            _keyServiceProvider = keyServiceProvider ?? throw new ArgumentNullException(nameof(keyServiceProvider));
            _cryptographicKeyRepository = cryptographicKeyRepository ?? throw new ArgumentNullException(nameof(cryptographicKeyRepository));
            _outputFactory = outputFactory ?? throw new ArgumentNullException(nameof(outputFactory));
        }

        #endregion

        #region IRawDataProcessor

        public async ValueTask<IOutput<byte[]>> ProcessPostSerializationAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            var keyService = await GetKeyService(cancellationToken);
            var encryptedData = await keyService.EncryptAsync(data, cancellationToken);
            return _outputFactory.Succeed(encryptedData);
        }

        public async ValueTask<IOutput<byte[]>> ProcessPreDeserializationAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            var keyService = await GetKeyService(cancellationToken);
            var decryptedData = await keyService.DecryptAsync(data, cancellationToken);
            return _outputFactory.Succeed(decryptedData);
        }

        #endregion

        #region Helpers

        private async ValueTask<ICryptographicKeyService> GetKeyService(CancellationToken cancellationToken)
        {
            var keyInformation = await _cryptographicKeyRepository.GetCryptographicKeyAsync(cancellationToken);
            return _keyServiceProvider.GetKeyService(keyInformation);
        }

        #endregion
    }
}
