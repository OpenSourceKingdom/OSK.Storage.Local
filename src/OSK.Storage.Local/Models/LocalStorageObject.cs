using Microsoft.Extensions.DependencyInjection;
using OSK.Functions.Outputs.Abstractions;
using OSK.Serialization.Abstractions;
using OSK.Storage.Abstractions;
using OSK.Storage.Local.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Models
{
    public class LocalStorageObject : StorageObject
    {
        #region Variables

        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Constructors

        internal LocalStorageObject(Stream stream, StorageMetaData metaData, IServiceProvider serviceProvider)
            : base(stream, metaData)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region StorageObject Overrides

        /// <inheritdoc/>
        public override async ValueTask<T> StreamAsAsync<T>(CancellationToken cancellationToken = default)
        {
            var serializerProvider = _serviceProvider.GetRequiredService<ISerializerProvider>();
            var serializer = serializerProvider.GetSerializer(MetaData.FullStoragePath);
            var dataProcessors = _serviceProvider.GetService<IEnumerable<IRawDataProcessor>>() 
                ?? Enumerable.Empty<IRawDataProcessor>();

            var decrypted = !MetaData.IsEncrypted;
            var streamBytes = await ReadStreamBytesAsync(cancellationToken);
            foreach (var dataProcessor in dataProcessors.Reverse())
            {
                if (decrypted && dataProcessor is ICryptographicRawDataProcessor)
                {
                    continue;
                }

                var dataResult = await dataProcessor.ProcessPreDeserializationAsync(streamBytes, cancellationToken);
                if (!dataResult.IsSuccessful)
                {
                    throw new InvalidOperationException($"An error occurred attempting to derialize the storage object's stream: {dataResult.GetErrorString()}");
                }

                streamBytes = dataResult.Value;
                if (dataProcessor is ICryptographicRawDataProcessor)
                {
                    decrypted = true;
                }
            }

            if (!decrypted)
            {
                throw new InvalidOperationException("Unable to deserialize an encrypted stream.");
            }

            return await serializer.DeserializeAsync<T>(streamBytes, cancellationToken);
        }

        #endregion

        #region Helpers

        private async ValueTask<byte[]> ReadStreamBytesAsync(CancellationToken cancellationToken)
        {
            using var contentStream = await GetContentAsMemoryStreamAsync(cancellationToken);
            return await contentStream.ToArrayAsync(cancellationToken: cancellationToken);
        }

        private async ValueTask<MemoryStream> GetContentAsMemoryStreamAsync(CancellationToken cancellationToken)
        {
            if (Value is MemoryStream memoryStream)
            {
                return memoryStream;
            }

            var contentStream = new MemoryStream();
            await Value.CopyToAsync(contentStream, cancellationToken);
            await Value.FlushAsync(cancellationToken);
            contentStream.Position = 0;

            Value.Dispose();

            return contentStream;
        }

        #endregion
    }
}
