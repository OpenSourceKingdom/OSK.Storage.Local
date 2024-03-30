using Microsoft.Extensions.DependencyInjection;
using OSK.Security.Cryptography.Aes;
using OSK.Storage.Local.Cryptography.Internal.Services;

namespace OSK.Storage.Local.Cryptography
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalStorageCryptography(this IServiceCollection services)
        {
            services.AddAesKeyService();
            services.AddSerializationRawDataProcessor<CryptographySerializationDataProcessor>();

            return services;
        }
    }
}
