using Microsoft.Extensions.DependencyInjection;
using OSK.Extensions.Serialization.SystemTextJson.Polymorphism;
using OSK.Extensions.Serialization.YamlDotNet.Polymorphism;
using OSK.Serialization.Polymorphism.Discriminators;
using System;

namespace OSK.Storage.Local.DefaultConfiguration.Polymorphism
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalStorageDefaultPolymorphism(this IServiceCollection services,
            Type assemblyMarkerType)
        {
            services
                .AddPolymorphismEnumDiscriminatorStrategy()
                .AddYamlDotNetPolymorphism(assemblyMarkerType)
                .AddSystemTextJsonPolymorphism();

            return services;
        }
    }
}
