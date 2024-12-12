using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OSK.Extensions.Object.DeepEquals;
using OSK.Extensions.Serialization.SystemTextJson.Polymorphism;
using OSK.Extensions.Serialization.YamlDotNet.Polymorphism;
using OSK.Serialization.Abstractions.Json;
using OSK.Serialization.Binary.Sharp;
using OSK.Serialization.Json.SystemTextJson;
using OSK.Serialization.Polymorphism.Discriminators;
using OSK.Serialization.Yaml.YamlDotNet;
using OSK.Storage.Abstractions;
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
using System.Xml.Linq;
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

        [Theory]
        [InlineData("test.json")]
        [InlineData("test.yaml")]
        [InlineData("test.bin")]
        [InlineData("test")]
        [InlineData("test.testExtension")]
        public async Task EndToEnd_NoDataProcessors(string fileNameWithExtension)
        {
            // Arrange
            var storageService = GetStorageService(services =>
            {
                services.AddSerializerExtensionDescriptor<IJsonSerializer>(".testExtension");
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

            var saveOptions = new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite,
                Encrypt = false
            };

            // Act/Assert
            await AssertUseCaseEndToEndAsync(storageService, file, fileNameWithExtension, saveOptions);
        }

        [Theory]
        [InlineData("test.json", false)]
        [InlineData("test.yaml", false)]
        [InlineData("test.bin", false)]
        [InlineData("test", false)]
        [InlineData("test.testExtension", false)]
        [InlineData("encrypted.json", true)]
        public async Task EndToEnd_ExtraDataProcessors(string fileNameWithExtension, bool encryptFile)
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

            var saveOptions = new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite,
                Encrypt = encryptFile
            };

            // Act/Assert
            await AssertUseCaseEndToEndAsync(storageService, file, fileNameWithExtension, saveOptions);
        }

        #region Helpers

        private async Task AssertUseCaseEndToEndAsync(ILocalStorageService storageService, TestFile testFile, 
            string fileNameWithExtension, LocalSaveOptions options)
        {
            var testFilePath = FileStorageFixture.GetFilePath(fileNameWithExtension);
            var saveOutput = await storageService.SaveAsync(testFile, testFilePath, options);
            Assert.True(saveOutput.IsSuccessful);

            var getOutput = await storageService.GetAsync(testFilePath);
            Assert.True(getOutput.IsSuccessful);
            
            using var o = getOutput.Value;
            var actualTestFile = await o.StreamAsAsync<TestFile>();

            Assert.True(testFile.DeepEquals(actualTestFile));
        }

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
