using Microsoft.Extensions.DependencyInjection;
using OSK.Serialization.Abstractions;
using OSK.Serialization.Abstractions.Binary;
using OSK.Serialization.Abstractions.Json;
using OSK.Serialization.Abstractions.Yaml;
using OSK.Storage.Local.Ports;
using System;
using System.IO;

namespace OSK.Storage.Local.Internal.Services
{
    internal class SerializerProvider : ISerializerProvider
    {
        #region Variables

        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Constructors

        public SerializerProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region ISerializerProvider

        public ISerializer GetSerializer(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            ISerializer serializer = extension switch
            {
                ".yaml" => _serviceProvider.GetService<IYamlSerializer>(),
                ".json" => _serviceProvider.GetService<IJsonSerializer>(),
                _ => null
            };

            return serializer == null
                // Binary should always be available for non specific serialization
                ? _serviceProvider.GetRequiredService<IBinarySerializer>()
                : serializer;
        }

        #endregion
    }
}
