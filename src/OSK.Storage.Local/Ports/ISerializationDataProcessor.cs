using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Ports
{
    public interface ISerializationDataProcessor
    {
        ValueTask<byte[]> ProcessPostSerializationAsync(byte[] data, CancellationToken cancellationToken = default);

        ValueTask<byte[]> ProcessPreDeserializationAsync(byte[] data, CancellationToken cancellationToken = default);
    }
}
