using OSK.Security.Cryptography.Abstractions;
using OSK.Security.Cryptography.Aes.Models;
using OSK.Storage.Local.Cryptography.Ports;

namespace OSK.Storage.Local.UnitTests.Helpers
{
    public class TestKeyRepository : ICryptographicKeyRepository
    {
        private static readonly AesKeyInformation KeyInformation = AesKeyInformation.New(128);

        internal AesKeyInformation CustomKeyInformation { get; set; }

        public ValueTask<ICryptographicKeyInformation> GetCryptographicKeyAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<ICryptographicKeyInformation>(CustomKeyInformation ?? KeyInformation);
        }
    }
}
