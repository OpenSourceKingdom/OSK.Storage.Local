using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OSK.Functions.Outputs.Logging;
using OSK.Storage.Local.Internal.Services;
using OSK.Storage.Local.Ports;

namespace OSK.Storage.Local
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalStorage(this IServiceCollection services)
        {
            services.AddLoggingFunctionOutputs();
            services
                    .TryAddTransient<ILocalStorageService, LocalStorageService>();
            services.TryAddTransient<ISerializerProvider, SerializerProvider>();

            /*
              
             
                    .AddBinarySerialization()
                    .AddJsonSerialization()
                    .AddYamlSerialization()
                    .AddPolymorphismEnumDiscriminatorStrategy()
                    .AddYamlPolymorphism()
                    .AddJsonPolymorphismConverter() 
             
             */

            return services;
        }

        public static IServiceCollection AddSerializationRawDataProcessor<T>(this IServiceCollection services)
           where T: class, IRawDataProcessor
        {
            services.AddTransient<IRawDataProcessor, T>();
            return services;
        }
    }
}
