using Microsoft.Extensions.DependencyInjection;
using OSK.Security.Cryptography;
using OSK.Storage.Local.Cryptography.Internal.Services;

namespace OSK.Storage.Local.Cryptography
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalStorageCryptography(this IServiceCollection services)
        {
            services
                .AddCryptography()
                .AddSerializationRawDataProcessor<CryptographySerializationDataProcessor>();

            return services;
        }
    }
}
