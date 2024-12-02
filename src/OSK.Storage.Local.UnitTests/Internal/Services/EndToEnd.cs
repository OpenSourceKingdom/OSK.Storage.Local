using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OSK.Extensions.Serialization.SystemTextJson.Polymorphism;
using OSK.Extensions.Serialization.YamlDotNet.Polymorphism;
using OSK.Serialization.Abstractions.Json;
using OSK.Serialization.Binary.Sharp;
using OSK.Serialization.Json.SystemTextJson;
using OSK.Serialization.Polymorphism.Discriminators;
using OSK.Serialization.Yaml.YamlDotNet;
using OSK.Storage.Local.Compression.Snappier;
using OSK.Storage.Local.Cryptography;
using OSK.Storage.Local.Cryptography.Ports;
using OSK.Storage.Local.Models;
using OSK.Storage.Local.Options;
using OSK.Storage.Local.Ports;
using OSK.Storage.Local.UnitTests.Helpers;
using OSK.Storage.Local.UnitTests.Helpers.TestFixtures;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace OSK.Storage.Local.UnitTests.Internal.Services
{
    [Collection("Sequential")]
    public class EndToEnd : IClassFixture<FileStorageFixture>
    {
        #region Variables

        FileStorageFixture FileStorageFixture { get; set; }

        #endregion

        #region Constructors

        public EndToEnd(FileStorageFixture fixture)
        {
            FileStorageFixture = fixture;
            FileStorageFixture.ClearTestDirectory();
            FileStorageFixture.SetEncoding(Encoding.UTF8);
        }

        #endregion

        [Fact]
        public async Task EndToEnd_NoDataProcessors()
        {
            // Arrange
            var storageService = GetStorageService();

            var file = new TestFile()
            {
                Date = DateTime.Now,
                Name = "file",
                Data = new List<TestParentData>()
                {
                    new TestChildData()
                    {
                        A = 1,
                        B = "2",
                        Child = new TestChildData()
                    }
                }
            };

            // Act/Assert
            var result1 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test.json"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result1.IsSuccessful);

            var result2 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test.yaml"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result2.IsSuccessful);

            var result3 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test.bin"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result3.IsSuccessful);

            var result4 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result4.IsSuccessful);

            var jsonResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test.json"));
            Assert.True(jsonResult.IsSuccessful);
            using var o = jsonResult.Value;
            _ = await o.StreamAsAsync<TestFile>();

            var yamlResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test.yaml"));
            Assert.True(yamlResult.IsSuccessful);
            using var o1 = yamlResult.Value;
            _ = await o1.StreamAsAsync<TestFile>();

            var binaryResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test.bin"));
            Assert.True(binaryResult.IsSuccessful);
            using var o2 = binaryResult.Value;
            _ = await o2.StreamAsAsync<TestFile>();

            var unknownResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test"));
            Assert.True(unknownResult.IsSuccessful);
            using var o3 = unknownResult.Value;
            _ = await o3.StreamAsAsync<TestFile>();
        }

        [Fact]
        public async Task EndToEnd_ExtraDataProcessors()
        {
            // Arrange
            var storageService = GetStorageService(services =>
            {
                services.TryAddTransient<ICryptographicKeyRepository, TestKeyRepository>();

                services
                    .AddLocalStorageCryptography()
                    .AddLocalStorageSnappierCompression()
                    .AddSerializerExtensionDescriptor<IJsonSerializer>(".testExtension");
            });

            var file = new TestFile()
            {
                Date = DateTime.Now,
                Name = "file",
                Data = new List<TestParentData>()
                {
                    new TestChildData()
                    {
                        A = 1,
                        B = "2",
                        Child = new TestChildData()
                    }
                }
            };

            // Act/Assert
            var result1 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test.json"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result1.IsSuccessful);

            var result2 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test.yaml"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result2.IsSuccessful);

            var result3 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test.bin"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result3.IsSuccessful);

            var result4 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result4.IsSuccessful);

            var result5 = await storageService.SaveAsync(file, FileStorageFixture.GetFilePath("test.testExtension"), new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });
            Assert.True(result5.IsSuccessful);

            var jsonResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test.json"));
            Assert.True(jsonResult.IsSuccessful);
            using var o = jsonResult.Value;
            _ = await o.StreamAsAsync<TestFile>();

            var yamlResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test.yaml"));
            Assert.True(yamlResult.IsSuccessful);
            using var o1 = yamlResult.Value;
            _ = await o1.StreamAsAsync<TestFile>();

            var binaryResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test.bin"));
            Assert.True(binaryResult.IsSuccessful);
            using var o2 = binaryResult.Value;
            _ = await o2.StreamAsAsync<TestFile>();

            var unknownResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test"));
            Assert.True(unknownResult.IsSuccessful);
            using var o3 = unknownResult.Value;
            _ = await o3.StreamAsAsync<TestFile>();

            var customResult = await storageService.GetAsync(FileStorageFixture.GetFilePath("test.testExtension"));
            Assert.True(customResult.IsSuccessful);
            using var o4 = customResult.Value;
            _ = await o4.StreamAsAsync<TestFile>();
        }

        #region Helpers

        private ILocalStorageService GetStorageService(Action<ServiceCollection> extras = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddPolymorphismEnumDiscriminatorStrategy();
            serviceCollection.AddBinarySharpSerialization();

            serviceCollection
                .AddYamlDotNetSerialization()
                .AddYamlDotNetPolymorphism(typeof(TestParentData));
            serviceCollection
                .AddSystemTextJsonSerialization(o =>
                {
                    o.WriteIndented = true;
                    o.PropertyNameCaseInsensitive = true;
                    o.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
                })
                .AddSystemTextJsonPolymorphism();
            serviceCollection.AddLocalStorage();

            if (extras != null)
            {
                extras(serviceCollection);
            }

            var provider = serviceCollection.BuildServiceProvider();
            return provider.GetRequiredService<ILocalStorageService>();
        }

        #endregion
    }
}
