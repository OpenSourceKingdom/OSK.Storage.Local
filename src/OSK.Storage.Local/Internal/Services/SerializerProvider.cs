using Microsoft.Extensions.DependencyInjection;
using OSK.Serialization.Abstractions;
using OSK.Serialization.Abstractions.Binary;
using OSK.Serialization.Abstractions.Json;
using OSK.Serialization.Abstractions.Yaml;
using OSK.Storage.Local.Models;
using OSK.Storage.Local.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OSK.Storage.Local.Internal.Services
{
    internal class SerializerProvider : ISerializerProvider
    {
        #region Variables

        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<SerializerExtensionDescriptor> _extensionDescriptors;

        #endregion

        #region Constructors

        public SerializerProvider(IEnumerable<SerializerExtensionDescriptor> extensionDescriptors, IServiceProvider serviceProvider)
        {
            _extensionDescriptors = extensionDescriptors ?? throw new ArgumentNullException(nameof(extensionDescriptors));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region ISerializerProvider

        public ISerializer GetSerializer(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            var serializerType = extension switch
            {
                ".bin" => typeof(IBinarySerializer),
                ".yaml" => typeof(IYamlSerializer),
                ".json" => typeof(IJsonSerializer),
                "" => typeof(IBinarySerializer),
                _ => _extensionDescriptors.FirstOrDefault(descriptor => descriptor.Extensions.Contains(extension))?.SerializerType
            };

            return (ISerializer)_serviceProvider.GetRequiredService(serializerType);
        }

        #endregion
    }
}
