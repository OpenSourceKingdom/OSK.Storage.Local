using OSK.Functions.Outputs.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Ports
{
    public interface IRawDataProcessor
    {
        ValueTask<IOutput<byte[]>> ProcessPostSerializationAsync(byte[] data, CancellationToken cancellationToken = default);
         
        ValueTask<IOutput<byte[]>> ProcessPreDeserializationAsync(byte[] data, CancellationToken cancellationToken = default);
    }
}
