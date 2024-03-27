using OSK.Storage.Local.Ports;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Cryptography.Internal.Services
{
    internal class CryptographySerializationDataProcessor : ISerializationDataProcessor
    {
        #region IRawDataProcessor

        public ValueTask<byte[]> ProcessPostSerializationAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<byte[]> ProcessPreDeserializationAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
