using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OSK.Functions.Outputs.Logging;
using OSK.Storage.Local.Internal.Services;
using OSK.Storage.Local.Options;
using OSK.Storage.Local.Ports;
using System;

namespace OSK.Storage.Local
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalStorage(this IServiceCollection services)
            => services.AddLocalStorage(_ => { });

        public static IServiceCollection AddLocalStorage(this IServiceCollection services, Action<LocalStorageOptions> optionConfigurator)
        {
            services.AddLoggingFunctionOutputs();
            services
                    .TryAddTransient<ILocalStorageService, LocalStorageService>();
            services.TryAddTransient<ISerializerProvider, SerializerProvider>();
            services.AddOptions()
                    .Configure(optionConfigurator);

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
    }
}
