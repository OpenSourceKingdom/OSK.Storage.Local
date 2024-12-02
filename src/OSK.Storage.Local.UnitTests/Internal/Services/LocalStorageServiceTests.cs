using Moq;
using OSK.Functions.Outputs.Abstractions;
using OSK.Functions.Outputs.Logging.Abstractions;
using OSK.Functions.Outputs.Mocks;
using OSK.Serialization.Abstractions.Binary;
using OSK.Storage.Local.Internal.Services;
using OSK.Storage.Local.Models;
using OSK.Storage.Local.Options;
using OSK.Storage.Local.Ports;
using OSK.Storage.Local.UnitTests.Helpers;
using OSK.Storage.Local.UnitTests.Helpers.TestFixtures;
using System.Text;
using Xunit;
namespace OSK.Storage.Local.UnitTests.Internal.Services
{
    [Collection("Sequential")]
    public class LocalStorageServiceTests : IClassFixture<FileStorageFixture>
    {
        #region Variables

        private readonly Encoding DefaultEncoding = Encoding.UTF8;

        private readonly Mock<IBinarySerializer> _mockBinarySerializer;
        private readonly IList<IRawDataProcessor> _dataProcessors;
        
        private readonly IOutputFactory<LocalStorageService> _mockOutputFactory;

        private readonly ILocalStorageService _localStorageService;
        private readonly FileStorageFixture _fileStorageFixture;

        #endregion

        #region Constructors

        public LocalStorageServiceTests(FileStorageFixture fixture)
        {
            _fileStorageFixture = fixture;
            _fileStorageFixture.NewDirectory();
            fixture.SetEncoding(DefaultEncoding);
            _fileStorageFixture.ClearTestDirectory();

            _mockBinarySerializer = new Mock<IBinarySerializer>();
            _mockOutputFactory = new MockOutputFactory<LocalStorageService>();

            var mockSerializerProvider = new Mock<ISerializerProvider>();
            mockSerializerProvider.Setup(m => m.GetSerializer(It.IsAny<string>()))
                .Returns(_mockBinarySerializer.Object);

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(m => m.GetService(It.Is<Type>(t => t == typeof(ISerializerProvider))))
                .Returns(mockSerializerProvider.Object);

            _dataProcessors = new List<IRawDataProcessor>();

            _localStorageService = new LocalStorageService(
                _dataProcessors, mockSerializerProvider.Object,
                mockServiceProvider.Object, _mockOutputFactory);
        }

        #endregion

        #region SaveAsync

        [Fact]
        public async Task SaveAsync_NullData_ThrowsArgumentNullException()
        {
            // Arrange
            var filePath = _fileStorageFixture.GetFilePath("Test");

            // Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _localStorageService.SaveAsync((string)null, filePath, new LocalSaveOptions()));
        }

        [Fact]
        public async Task SaveAsync_NullFilePath_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _localStorageService.SaveAsync("Test", null, new LocalSaveOptions()));
        }

        [Fact]
        public async Task SaveAsync_EmptyFilePath_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _localStorageService.SaveAsync("Test", "", new LocalSaveOptions()));
        }

        [Fact]
        public async Task SaveAsync_NullStorageOptions_ThrowsArgumentNullException()
        {
            // Arrange
            var filePath = _fileStorageFixture.GetFilePath("Test");
            var data = "test";

            // Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _localStorageService.SaveAsync(data, filePath, null));
        }

        [Fact]
        public async Task SaveAsync_EncryptionRequestWithoutICryptographicRawDataProcessor_ThrowsArgumentNullException()
        {
            // Arrange
            var filePath = _fileStorageFixture.GetFilePath("Test");
            var data = "test";

            // Act/Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _localStorageService.SaveAsync(data, filePath, new LocalSaveOptions()
            {
                Encrypt = true
            }));
        }

        [Fact]
        public async Task SaveAsync_FileExistsNoOverwritePermission_ReturnsError()
        {
            // Arrange
            var testFilePath = _fileStorageFixture.CreateTestFile("testTextFile", "Test");
            var data = "Test";
            var storageOptions = new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.NoOverwrite
            };

            // Act
            var result = await _localStorageService.SaveAsync(data, testFilePath, storageOptions);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains("A file already exists", result.GetErrorString());
        }

        [Fact]
        public async Task SaveAsync_FileDoesNotExist_UsesEncryption_EncryptingError_ReturnsError()
        {
            // Arrange
            _mockBinarySerializer.Setup(m => m.SerializeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object input, CancellationToken _) => DefaultEncoding.GetBytes((string)input));

            var mockCryptographicDataProcessor = new Mock<ICryptographicRawDataProcessor>();
            mockCryptographicDataProcessor.Setup(m => m.ProcessPostSerializationAsync(It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockOutputFactory.BadRequest<byte[]>("A bad day!"));
            _dataProcessors.Add(mockCryptographicDataProcessor.Object);

            var text = "Test Text";
            var fileName = "testTextFile";
            var testFilePath = _fileStorageFixture.GetFilePath(fileName + ".txt");
            var storageOptions = new LocalSaveOptions()
            {
                Encrypt = true,
                SavePermissions = SavePermissionType.NoOverwrite
            };

            // Act
            var result = await _localStorageService.SaveAsync(text, testFilePath, storageOptions);

            // Assert
            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public async Task SaveAsync_FileDoesNotExist_NoEncryption_ReturnsSuccess()
        {
            // Arrange
            _mockBinarySerializer.Setup(m => m.SerializeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object input, CancellationToken _) => DefaultEncoding.GetBytes((string)input));

            var text = "Test Text";
            var fileName = "testTextFile";
            var testFilePath = _fileStorageFixture.GetFilePath($"{fileName}.txt");
            var storageOptions = new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.NoOverwrite
            };

            // Act
            var result = await _localStorageService.SaveAsync(text, testFilePath, storageOptions);

            // Assert
            Assert.True(result.IsSuccessful);

            Assert.Equal(".txt", result.Value.Extension);
            Assert.Equal(fileName, result.Value.FileName);
            Assert.Equal(_fileStorageFixture.TestDirectory, result.Value.StorageDirectory);
            Assert.Equal(testFilePath, result.Value.FullStoragePath);
            Assert.Equal("text/plain", result.Value.StorageMimeType);
            Assert.False(result.Value.IsEncrypted);
        }

        [Fact]
        public async Task SaveAsync_FileExistsOverwritingAllowed_NoEncryption_ReturnsSuccess()
        {
            // Arrange
            _mockBinarySerializer.Setup(m => m.SerializeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object input, CancellationToken _) => DefaultEncoding.GetBytes((string)input));

            var text = "Test Text";
            var fileName = "testTextFile";
            var testFilePath = _fileStorageFixture.CreateTestFile(fileName, "Test");
            var storageOptions = new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            };

            // Act
            var result = await _localStorageService.SaveAsync(text, testFilePath, storageOptions);

            // Assert
            Assert.True(result.IsSuccessful);

            Assert.Equal(testFilePath, result.Value.FullStoragePath);
            Assert.Equal(fileName, result.Value.FileName);
            Assert.Equal(_fileStorageFixture.TestDirectory, result.Value.StorageDirectory);
            Assert.Equal(".txt", result.Value.Extension);
            Assert.Equal("text/plain", result.Value.StorageMimeType);
            Assert.False(result.Value.IsEncrypted);
            Assert.True(result.Value.LastModifiedTimeUtc >= DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public async Task SaveAsync_FileDoesNotExist_UsesEncryption_ReturnsSuccess()
        {
            // Arrange
            _mockBinarySerializer.Setup(m => m.SerializeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object input, CancellationToken _) => DefaultEncoding.GetBytes((string)input));

            var mockCryptographicDataProcessor = new Mock<ICryptographicRawDataProcessor>();
            mockCryptographicDataProcessor.Setup(m => m.ProcessPostSerializationAsync(It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] data, CancellationToken _) => _mockOutputFactory.Success(data));
            _dataProcessors.Add(mockCryptographicDataProcessor.Object);

            var text = "Test Text";
            var fileName = "testTextFile";
            var testFilePath = _fileStorageFixture.GetFilePath($"{fileName}.txt");
            var storageOptions = new LocalSaveOptions()
            {
                Encrypt = true,
                SavePermissions = SavePermissionType.NoOverwrite
            };

            // Act
            var result = await _localStorageService.SaveAsync(text, testFilePath, storageOptions);

            // Assert
            Assert.True(result.IsSuccessful);

            Assert.Equal(testFilePath, result.Value.FullStoragePath);
            Assert.Equal(fileName, result.Value.FileName);
            Assert.Equal(_fileStorageFixture.TestDirectory, result.Value.StorageDirectory);
            Assert.Equal(".txt", result.Value.Extension);
            Assert.Equal("text/plain", result.Value.StorageMimeType);
            Assert.True(result.Value.IsEncrypted);
            Assert.True(result.Value.LastModifiedTimeUtc >= DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1)));
        }

        #endregion

        #region GetAsync

        [Fact]
        public async Task GetAsync_NullFilePath_ThrowsArgumentNullException()
        {
            // Assert/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _localStorageService.GetAsync(null));
        }

        [Fact]
        public async Task GetAsync_EmptyFilePath_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _localStorageService.GetAsync(""));
        }

        [Fact]
        public async Task GetAsync_FilePathDoesNotExist_ReturnsError()
        {
            // Act
            var result = await _localStorageService.GetAsync("NotARealPath");

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains("did not exist", result.GetErrorString());
        }

        [Fact]
        public async Task GetAsync_FilePathExists_IsEncrypted_DecryptionReturnsError()
        {
            // Arrange
            var text = "Test text for file exists";
            var testFilePath = _fileStorageFixture.GetFilePath("testFile.txt");

            var encryptedTestBytes = LocalStorageService.LocalStorageSignatureBytes;

            _mockBinarySerializer.Setup(m => m.SerializeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object o, CancellationToken _) => DefaultEncoding.GetBytes((string)o));
            _mockBinarySerializer.Setup(m => m.DeserializeAsync(It.IsAny<byte[]>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] input, byte[] _, CancellationToken _) => DefaultEncoding.GetString(input));

            var mockCryptographicDataProcessor = new Mock<ICryptographicRawDataProcessor>();
            mockCryptographicDataProcessor.Setup(m => m.ProcessPostSerializationAsync(It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockOutputFactory.Success(encryptedTestBytes));
            mockCryptographicDataProcessor.Setup(m => m.ProcessPreDeserializationAsync(It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockOutputFactory.BadRequest<byte[]>("A bad day!"));
            _dataProcessors.Add(mockCryptographicDataProcessor.Object);

            // Act
            var encryptedResult = await _localStorageService.SaveAsync(text, testFilePath,
                new LocalSaveOptions()
                {
                    Encrypt = true
                });
            Assert.True(encryptedResult.IsSuccessful);

            var result = await _localStorageService.GetAsync(testFilePath);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.True(result.Value.MetaData.IsEncrypted);

            using var storageObject = result.Value;

            await Assert.ThrowsAsync<InvalidOperationException>(async() => await storageObject.StreamAsAsync<string>());
        }

        [Fact]
        public async Task GetAsync_FilePathExists_NotEncrypted_ReturnsSuccess()
        {
            // Arrange
            _mockBinarySerializer.Setup(m => m.DeserializeAsync(It.IsAny<byte[]>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] input, Type _, CancellationToken _) => input);

            var text = "Test text for file exists";
            var filename = "testFile";
            var testFilePath = _fileStorageFixture.CreateTestFile(filename, text);

            // Act
            var result = await _localStorageService.GetAsync(testFilePath);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Value.Value);
            Assert.NotNull(result.Value.MetaData);

            using var storageObject = result.Value;
            var bytes = await storageObject.StreamAsAsync<byte[]>();
            var value = Encoding.UTF8.GetString(bytes);

            Assert.Equal(text, value);
            Assert.Equal(filename, result.Value.MetaData.FileName);
            Assert.Equal(testFilePath, result.Value.MetaData.FullStoragePath);
            Assert.Equal(".txt", result.Value.MetaData.Extension);
            Assert.False(result.Value.MetaData.IsEncrypted);
            Assert.True(result.Value.MetaData.LastModifiedTimeUtc >= DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1)));
        }

        #endregion

        #region GetStorageDetailsAsync

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetStorageDetailsAsync_InvalidDirectoryPath_ThrowsArgumentNullException(string path)
        {
            // Arrange/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _localStorageService.GetStorageDetailsAsync(path, null));
        }

        [Fact]
        public async Task GetStorageDetailsAsync_DirectoryPathDoesNotExist_ReturnsError()
        {
            // Act
            var result = await _localStorageService.GetStorageDetailsAsync("bad/Path", null);

            // Assert
            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public async Task GetStorageDetailsAsync_EmptyDirectory_ReturnsEmptyList()
        {
            // Act
            var result = await _localStorageService.GetStorageDetailsAsync(_fileStorageFixture.TestDirectory, null);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetStorageDetailsAsync_DirectoryHasObjects_NoExtension_ReturnsllFiles()
        {
            // Arrange
            var testTexts = new[]
            {
                "Test A",
                "Test B",
                "Test C"
            };

            _fileStorageFixture.CreateTestTextFiles(testTexts);
            _fileStorageFixture.CreateTestFile("TestFile-3", "ABC", ".special");

            _mockBinarySerializer.Setup(m => m.SerializeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object input, CancellationToken _) => DefaultEncoding.GetBytes((string)input));

            var mockCryptographicDataProcessor = new Mock<ICryptographicRawDataProcessor>();
            mockCryptographicDataProcessor.Setup(m => m.ProcessPostSerializationAsync(It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] bytes, CancellationToken _) => _mockOutputFactory.Success(bytes));
            mockCryptographicDataProcessor.Setup(m => m.ProcessPreDeserializationAsync(It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] bytes, CancellationToken _) => _mockOutputFactory.Success(bytes));
            _dataProcessors.Add(mockCryptographicDataProcessor.Object);

            var encryptedFileName = "encryptedTestFile";
            var encryptedPath = _fileStorageFixture.GetFilePath(encryptedFileName);

            // Act
            await _localStorageService.SaveAsync("Test", encryptedPath, new LocalSaveOptions()
            {
                Encrypt = true
            });

            var getResult = await _localStorageService.GetStorageDetailsAsync(_fileStorageFixture.TestDirectory, null);

            // Assert
            Assert.True(getResult.IsSuccessful);
            Assert.Equal(testTexts.Length + 1, getResult.Value.Count(storageObjects => !storageObjects.IsEncrypted));
            Assert.Equal(1, getResult.Value.Count(storageObjects => storageObjects.IsEncrypted));

            foreach (var storageMetaData in getResult.Value)
            {
                if (storageMetaData.IsEncrypted)
                {
                    Assert.Equal(encryptedFileName, storageMetaData.FileName);
                    Assert.True(storageMetaData.LastModifiedTimeUtc >= DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1)));
                    continue;
                }

                Assert.NotEqual(encryptedFileName, storageMetaData.FileName);
            }
        }

        [Fact]
        public async Task GetStorageDetailsAsync_DirectoryHasObjects_Extension_ReturnsFilesWithExtension()
        {
            // Arrange
            var testTexts = new[]
            {
                "Test A",
                "Test B",
                "Test C"
            };

            _fileStorageFixture.CreateTestTextFiles(testTexts);
            _fileStorageFixture.CreateTestFile("Test D", "ABC", ".special");

            _mockBinarySerializer.Setup(m => m.SerializeAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((object input, CancellationToken _) => DefaultEncoding.GetBytes((string)input));

            var mockCryptographicDataProcessor = new Mock<ICryptographicRawDataProcessor>();
            mockCryptographicDataProcessor.Setup(m => m.ProcessPostSerializationAsync(It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] bytes, CancellationToken _) => _mockOutputFactory.Success(bytes));
            mockCryptographicDataProcessor.Setup(m => m.ProcessPreDeserializationAsync(It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] bytes, CancellationToken _) => _mockOutputFactory.Success(bytes));
            _dataProcessors.Add(mockCryptographicDataProcessor.Object);

            var encryptedFileName = "encryptedTestFile";
            var encryptedPath = _fileStorageFixture.GetFilePath(encryptedFileName);

            // Act
            await _localStorageService.SaveAsync("Test", encryptedPath, new LocalSaveOptions()
            {
                Encrypt = true
            });

            var getResult = await _localStorageService.GetStorageDetailsAsync(_fileStorageFixture.TestDirectory, new FileSearchOptions()
            {
                Extension = ".special"
            });

            // Assert
            Assert.True(getResult.IsSuccessful);
            Assert.Single(getResult.Value);
        }

        #endregion

        #region Delete

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task DeleteAsync_NullAndEmptyString_ReturnsSuccess(string path)
        {
            // Act
            var result = await _localStorageService.DeleteAsync(path);

            // Assert
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public async Task DeleteAsync_PathDoesNotExist_ReturnsSuccess()
        {
            // Act
            var result = await _localStorageService.DeleteAsync("bad/path");

            // Assert
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public async Task DeleteAsync_PathExists_DeletesFile_ReturnsSuccess()
        {
            // Arrange
            var testFilePath = _fileStorageFixture.CreateTestFile("testFile", "test");

            // Act
            var fileExistedBeforeTest = File.Exists(testFilePath);
            var result = await _localStorageService.DeleteAsync(testFilePath);

            // Assert
            Assert.True(fileExistedBeforeTest);
            Assert.True(result.IsSuccessful);
            Assert.False(File.Exists(testFilePath));
        }

        #endregion

        #region e2e

        [Fact]
        public async Task E2E_SaveFollowedByGet_ReturnsExpectedValue()
        {
            var testObj = new TestFile()
            {
                Name = "Hello",
                Date = DateTime.UtcNow,
                Data = new List<TestParentData>()
                {
                    new TestChildData()
                    {
                        A = 1,
                        B = "Hello",
                        Child = null
                    },
                    new TestChildData()
                    {
                        A = 11,
                        B = "World",
                        Child = new TestChildData()
                        {
                            A = 42,
                            B = "The Answers Are In"
                        }
                    }
                }
            };

            var path = _fileStorageFixture.GetFilePath("data.persisted");

            var saveResult = await _localStorageService.SaveAsync(testObj, path, new LocalSaveOptions()
            {
                SavePermissions = SavePermissionType.AllowOverwrite
            });

            Assert.True(saveResult.IsSuccessful);

            var getResult = await _localStorageService.GetAsync(path);
            getResult.Value.Dispose();

            Assert.True(getResult.IsSuccessful);
        }

        #endregion
    }
}
