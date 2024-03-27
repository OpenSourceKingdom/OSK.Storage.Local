using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeyRed.Mime;
using Microsoft.Extensions.Options;
using OSK.Functions.Outputs.Abstractions;
using OSK.Functions.Outputs.Logging.Abstractions;
using OSK.Storage.Abstractions;
using OSK.Storage.Local.Models;
using OSK.Storage.Local.Options;
using OSK.Storage.Local.Ports;

namespace OSK.Storage.Local.Internal.Services
{
    internal class LocalStorageService : ILocalStorageService
    {
        #region Variables

        public const string LocalStorageSignature = "__Common.Storage.LocalStorage__";
        public const string YamlMimeType = "application/x-yaml";

        private static readonly Encoding DefaultEncoding = Encoding.UTF8;
        public static readonly byte[] LocalStorageSignatureBytes = DefaultEncoding.GetBytes(LocalStorageSignature);

        private readonly ISerializerProvider _serializerProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<LocalStorageOptions> _options;
        private readonly IOutputFactory<LocalStorageService> _outputFactory;

        #endregion

        #region Constructors

        public LocalStorageService(ISerializerProvider serializerProvider,
            IServiceProvider serviceProvider,
            IOptions<LocalStorageOptions> options,
            IOutputFactory<LocalStorageService> resultFactory)
        {
            _serializerProvider = serializerProvider ?? throw new ArgumentNullException(nameof(serializerProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputFactory = resultFactory ?? throw new ArgumentNullException();
        }

        #endregion

        #region ILocalFileStorageService

        public async Task<IOutput<StorageMetaData>> SaveAsync<T>(T data, string fullFilePath, FileSaveOptions options, CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (string.IsNullOrEmpty(fullFilePath))
            {
                throw new ArgumentNullException(nameof(fullFilePath));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.SaveOption == SavePermissionType.NoOverwrite && File.Exists(fullFilePath))
            {
                return _outputFactory.BadRequest<StorageMetaData>(
                    $"A file already exists at the given path, {fullFilePath}, and overwriting was not allowed. To overwrite an existing file, use {SavePermissionType.AllowOverwrite}.",
                    Constants.LibraryName);
            }
            if (options.EncryptionAlgorithm.HasValue && string.IsNullOrWhiteSpace(_options.Value.EncryptionKey))
            {
                throw new InvalidOperationException($"No {nameof(_options.Value.EncryptionKey)} was provided during configuration startup but a request was made to encrypt the file being saved.");
            }

            var directory = Path.GetDirectoryName(fullFilePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var serializer = _serializerProvider.GetSerializer(fullFilePath);
            var serializedData = await serializer.SerializeAsync(data, cancellationToken);

            Stream dataStream = null;
            try
            {
                if (options.EncryptionAlgorithm.HasValue)
                {
                    var encryptionResult = await _cryptographyService.EncryptAsync(serializedData, GetEncryptionKey(), new CryptographyOptions()
                    {
                        EncryptionAlgorithm = ConvertEncryptionAlgorithm(options.EncryptionAlgorithm.Value)
                    });
                    if (!encryptionResult.IsSuccessful())
                    {
                        return encryptionResult.AsType<StorageMetaData>();
                    }

                    dataStream = encryptionResult.Value;
                }
                else
                {
                    dataStream = new MemoryStream(serializedData);
                }

                await SaveToFileAsync(fullFilePath, dataStream, options.EncryptionAlgorithm);
            }
            finally
            {
                dataStream?.Dispose();
            }

            return _outputFactory.Success(new StorageMetaData(fullFilePath, serializedData.LongLength,
                options.EncryptionAlgorithm.HasValue, GetMimeType(fullFilePath), 
                File.GetLastWriteTimeUtc(fullFilePath)));
        }

        public async Task<IOutput<StorageObject>> GetAsync(string fullFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fullFilePath))
            {
                throw new ArgumentNullException(nameof(fullFilePath));
            }
            if (!File.Exists(fullFilePath))
            {
                return _outputFactory.NotFound<StorageObject>($"The following file {fullFilePath} did not exist.");
            }

            var getLocalObjectResult = await GetLocalStorageObjectAsync(fullFilePath, cancellationToken);
            if (!getLocalObjectResult.IsSuccessful)
            {
                return getLocalObjectResult.AsType<StorageObject>();
            }

            return _outputFactory.Success((StorageObject)new LocalStorageObject(
                getLocalObjectResult.Value.DataStream,
                new StorageMetaData(
                    fullFilePath, getLocalObjectResult.Value.Size, getLocalObjectResult.Value.IsEncrypted, 
                    GetMimeType(fullFilePath), File.GetLastWriteTime(fullFilePath)),
                _serviceProvider));
        }

        public async Task<IOutput<IEnumerable<StorageMetaData>>> GetStorageDetailsAsync(string directoryPath, FileSearchOptions searchOptions,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (!Directory.Exists(directoryPath))
            {
                return _outputFactory.NotFound<IEnumerable<StorageMetaData>>(
                    $"The path {directoryPath} does not exist.");
            }

            var searchExtension = searchOptions == null || string.IsNullOrWhiteSpace(searchOptions.Extension)
                                        ? string.Empty : searchOptions.Extension;

            var directoryFiles = Directory.GetFiles(directoryPath, $"*{searchExtension}");

            var storageDetails = new List<StorageMetaData>();

            foreach (var filePath in directoryFiles)
            {
                var fileInfo = new FileInfo(filePath);
                fileInfo.Refresh();

                var getLocalObjectResult = await GetLocalStorageObjectAsync(filePath, cancellationToken);
                if (!getLocalObjectResult.IsSuccessful)
                {
                    return getLocalObjectResult.AsType<IEnumerable<StorageMetaData>>();
                }
                
                storageDetails.Add(new StorageMetaData(
                    filePath, getLocalObjectResult.Value.Size, getLocalObjectResult.Value.IsEncrypted,
                    GetMimeType(filePath), File.GetLastWriteTime(filePath)));
            }

            return _outputFactory.Success((IEnumerable<StorageMetaData>)storageDetails);
        }

        public Task<IOutput> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.FromResult(_outputFactory.Success());
            }

            File.Delete(filePath);

            return Task.FromResult(_outputFactory.Success());
        }

        #endregion

        #region Helpers

        private string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            if (extension == ".yaml")
            {
                return YamlMimeType;
            }

            return MimeTypesMap.GetMimeType(filePath);
        }

        private async Task SaveToFileAsync(string filePath, Stream dataStream, EncryptionAlgorithms? alogorithm)
        {
            using var fileStream = File.Create(filePath);

            if (alogorithm.HasValue)
            {
                await fileStream.WriteAsync(LocalStorageSignatureBytes);
                fileStream.WriteByte((byte)alogorithm.Value);
            }
            await dataStream.CopyToAsync(fileStream);
        }

        private async Task<IOutput<LocalStorageFile>> GetLocalStorageObjectAsync(string filePath, CancellationToken cancellationToken)
        {
            using var fileStream = File.OpenRead(filePath);

            var dataSize = fileStream.Length;
            EncryptionAlgorithms? algorithm = null;

            var fileSignatureBytes = new byte[LocalStorageSignatureBytes.Length];
            var bytesReadOnSignatureRead = await fileStream.ReadAsync(fileSignatureBytes, cancellationToken);
            if (bytesReadOnSignatureRead == LocalStorageSignatureBytes.Length
                && fileSignatureBytes.SequenceEqual(LocalStorageSignatureBytes))
            {
                algorithm = (EncryptionAlgorithms)fileStream.ReadByte();
                dataSize -= sizeof(byte) * bytesReadOnSignatureRead - sizeof(byte);
            }
            else
            {
                fileStream.Position = 0;
            }

            Stream dataStream;
            if (algorithm.HasValue)
            {
                if (string.IsNullOrWhiteSpace(_options.Value.EncryptionKey))
                {
                    throw new InvalidOperationException($"No {nameof(_options.Value.EncryptionKey)} was provided during configuration startup but the file located at {filePath} is encrypted.");
                }

                var decryptResult = await _cryptographyService.DecryptAsync(fileStream, GetEncryptionKey(), new CryptographyOptions()
                {
                    EncryptionAlgorithm = ConvertEncryptionAlgorithm(algorithm.Value)
                });
                if (!decryptResult.IsSuccessful())
                {
                    return decryptResult.AsType<LocalStorageFile>();
                }

                dataStream = decryptResult.Value;
            }
            else
            {
                dataStream = new MemoryStream((int)dataSize);
                await fileStream.CopyToAsync(dataStream);
                dataStream.Position = 0;
            }

            return _outputFactory.Success(new LocalStorageFile()
            {
                IsEncrypted = algorithm.HasValue || File.GetAttributes(filePath).HasFlag(FileAttributes.Encrypted),
                Size = fileStream.Length,
                DataStream = dataStream
            });
        }

        private Cryptography.Models.EncryptionAlgorithm ConvertEncryptionAlgorithm(EncryptionAlgorithms encryptionAlgorithm)
        {
            switch (encryptionAlgorithm)
            {
                // case EncryptionAlgorithm.Aes:
                default:
                    return Cryptography.Models.EncryptionAlgorithm.Aes;
            }
        }

        private byte[] GetEncryptionKey()
        {
            return DefaultEncoding.GetBytes(_options.Value.EncryptionKey);
        }

        #endregion
    }
}
