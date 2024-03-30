using Microsoft.Extensions.DependencyInjection;
using OSK.Serialization.Binary.Sharp;
using OSK.Serialization.Json.SystemTextJson;
using OSK.Serialization.Yaml.YamlDotNet;
using System.Text.Json.Serialization.Metadata;

namespace OSK.Storage.Local.DefaultConfiguration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalStorageDefaultSerializers(this IServiceCollection services)
        {
            services
                .AddBinarySharpSerialization()
                .AddYamlDotNetSerialization()
                .AddSystemTextJsonSerialization(o =>
                {
                    o.WriteIndented = true;
                    o.PropertyNameCaseInsensitive = true;
                    o.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
                })
                .AddLocalStorage();

            return services;
        }
    }
}
