using OSK.Operations.Outputs;
using OSK.Operations.Outputs.Models;
using OSK.Storage.Local.Ports;
using Snappier;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Compression.Snappier.Internal.Services;

internal class SnappierCompressionSerializationDataProcessor : IRawDataProcessor
{
    #region ISerializationDataProcessor

    public ValueTask<Output<byte[]>> ProcessPostSerializationAsync(byte[] data,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Output<byte[]>>(Out.Success(Snappy.CompressToArray(data)));
    }

    public ValueTask<Output<byte[]>> ProcessPreDeserializationAsync(byte[] data, 
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Output<byte[]>>(Out.Success(Snappy.DecompressToArray(data)));
    }

    #endregion
}
