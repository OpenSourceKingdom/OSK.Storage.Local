using Microsoft.Extensions.DependencyInjection;
using OSK.Storage.Local.Compression.Snappier.Internal.Services;

namespace OSK.Storage.Local.Compression.Snappier
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalStorageSnappierCompression(this IServiceCollection services)
        {
            services.AddSerializationRawDataProcessor<SnappierCompressionSerializationDataProcessor>();

            return services;
        }
    }
}
