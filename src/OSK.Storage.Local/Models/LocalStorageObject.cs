using Microsoft.Extensions.DependencyInjection;
using OSK.Serialization.Abstractions;
using OSK.Storage.Abstractions;
using OSK.Storage.Local.Ports;
using System;
using System.IO;
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

            if (Value is MemoryStream memoryStream)
            {
                return await serializer.DeserializeAsync<T>(memoryStream.ToArray(), cancellationToken);
            }

            using var copyStream = new MemoryStream();
            await Value.CopyToAsync(copyStream, cancellationToken);

            return await serializer.DeserializeAsync<T>(copyStream.ToArray(), cancellationToken);
        }

        #endregion
    }
}
