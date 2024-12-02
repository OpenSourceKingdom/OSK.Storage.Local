using OSK.Functions.Outputs.Abstractions;
using OSK.Hexagonal.MetaData;
using OSK.Serialization.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Ports
{
    /// <summary>
    /// A data processor that is used either before an <see cref="ISerializer"/> deserializes or after serialization on the raw data represented by a given object.
    /// This provides methods for serialization and deserialization to ensure that any processing for storage can be reversed if necessary so the raw data can be 
    /// successfully converted back into an object
    /// </summary>
    [HexagonalPort(HexagonalPort.Secondary)]
    public interface IRawDataProcessor
    {
        /// <summary>
        /// This method is triggered after an object has been converted into raw data and is ran prior to being stored
        /// </summary>
        /// <param name="data">The raw data for the serialized object</param>
        /// <param name="cancellationToken">A token for cancelling the operation</param>
        /// <returns>An output of processing of the data. This should return the processed data, if anything occurred to it directly.</returns>
        ValueTask<IOutput<byte[]>> ProcessPostSerializationAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// This method is triggered before raw data is converted into an object and is ran after being loaded into memory
        /// </summary>
        /// <param name="data">The raw data for an object</param>
        /// <param name="cancellationToken">A token for cancelling the operation</param>
        /// <returns>An output of processing of the data. This should return the processed data, if anything occurred to it directly.</returns>
        ValueTask<IOutput<byte[]>> ProcessPreDeserializationAsync(byte[] data, CancellationToken cancellationToken = default);
    }
}
