using OSK.Functions.Outputs.Abstractions;
using OSK.Functions.Outputs.Logging.Abstractions;
using OSK.Storage.Local.Ports;
using Snappier;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Compression.Snappier.Internal.Services
{
    internal class SnappierCompressionSerializationDataProcessor : IRawDataProcessor
    {
        #region Variables

        private readonly IOutputFactory<SnappierCompressionSerializationDataProcessor> _outputFactory;

        #endregion

        #region Constructors

        public SnappierCompressionSerializationDataProcessor(IOutputFactory<SnappierCompressionSerializationDataProcessor> outputFactory)
        {
            _outputFactory = outputFactory ?? throw new ArgumentNullException(nameof(outputFactory));
        }

        #endregion

        #region ISerializationDataProcessor

        public ValueTask<IOutput<byte[]>> ProcessPostSerializationAsync(byte[] data,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<IOutput<byte[]>>(_outputFactory.Success(Snappy.CompressToArray(data)));
        }

        public ValueTask<IOutput<byte[]>> ProcessPreDeserializationAsync(byte[] data, 
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<IOutput<byte[]>>(_outputFactory.Success(Snappy.DecompressToArray(data)));
        }

        #endregion
    }
}
