using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeyRed.Mime;
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

        private readonly IEnumerable<IRawDataProcessor> _dataProcessors;
        private readonly ISerializerProvider _serializerProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOutputFactory<LocalStorageService> _outputFactory;

        #endregion

        #region Constructors

        public LocalStorageService(
            IEnumerable<IRawDataProcessor> dataProcessors,
            ISerializerProvider serializerProvider,
            IServiceProvider serviceProvider,
            IOutputFactory<LocalStorageService> resultFactory)
        {
            _dataProcessors = dataProcessors ?? throw new ArgumentNullException(nameof(dataProcessors));
            _serializerProvider = serializerProvider ?? throw new ArgumentNullException(nameof(serializerProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _outputFactory = resultFactory ?? throw new ArgumentNullException();
        }

        #endregion

        #region ILocalFileStorageService

        public async Task<IOutput<StorageMetaData>> SaveAsync<T>(T data, string fullFilePath, LocalSaveOptions options, CancellationToken cancellationToken = default)
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
            if (options.SavePermissions == SavePermissionType.NoOverwrite && File.Exists(fullFilePath))
            {
                return _outputFactory.Fail<StorageMetaData>(
                    $"A file already exists at the given path, {fullFilePath}, and overwriting was not allowed. To overwrite an existing file, use {SavePermissionType.AllowOverwrite}.",
                    OutputSpecificityCode.InvalidParameter, Constants.LibraryName);
            }
            if (options.Encrypt && !_dataProcessors.Any(processor => processor is ICryptographicRawDataProcessor))
            {
                throw new InvalidOperationException($"Unable to encrypt a file with not cryptographic data processor provided in the application configuration.");
            }

            var directory = Path.GetDirectoryName(fullFilePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var serializer = _serializerProvider.GetSerializer(fullFilePath);
            var serializedData = await serializer.SerializeAsync(data, cancellationToken);

            try
            {
                foreach (var dataProcessor in _dataProcessors)
                {
                    if (!options.Encrypt && dataProcessor is ICryptographicRawDataProcessor)
                    {
                        continue;
                    }

                    var dataResult = await dataProcessor.ProcessPostSerializationAsync(serializedData, cancellationToken);
                    if (!dataResult.IsSuccessful)
                    {
                        return dataResult.AsOutput<StorageMetaData>();
                    }

                    serializedData = dataResult.Value;
                }

                await SaveToFileAsync(fullFilePath, serializedData, options.Encrypt, cancellationToken);
            }
            catch (Exception ex)
            {
                return _outputFactory.Exception<StorageMetaData>(ex, Constants.LibraryName);
            }

            return _outputFactory.Succeed(new StorageMetaData(fullFilePath, serializedData.LongLength,
                options.Encrypt, GetMimeType(fullFilePath), 
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
                return _outputFactory.Fail<StorageObject>($"The following file {fullFilePath} did not exist.", OutputSpecificityCode.DataNotFound);
            }

            var getLocalObjectResult = await GetLocalStorageObjectAsync(fullFilePath, keepStreamOpen: true, cancellationToken);
            if (!getLocalObjectResult.IsSuccessful)
            {
                return getLocalObjectResult.AsOutput<StorageObject>();
            }

            return _outputFactory.Succeed((StorageObject)new LocalStorageObject(
                getLocalObjectResult.Value.DataStream,
                new StorageMetaData(
                    fullFilePath, getLocalObjectResult.Value.Size, getLocalObjectResult.Value.IsEncrypted, 
                    GetMimeType(fullFilePath), File.GetLastWriteTimeUtc(fullFilePath)),
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
                return _outputFactory.Fail<IEnumerable<StorageMetaData>>(
                    $"The path {directoryPath} does not exist.", OutputSpecificityCode.DataNotFound);
            }

            var searchExtension = searchOptions == null || string.IsNullOrWhiteSpace(searchOptions.Extension)
                                        ? string.Empty : searchOptions.Extension;

            var directoryFiles = Directory.GetFiles(directoryPath, $"*{searchExtension}");

            var storageDetails = new List<StorageMetaData>();

            foreach (var filePath in directoryFiles)
            {
                var fileInfo = new FileInfo(filePath);
                fileInfo.Refresh();

                var getLocalObjectResult = await GetLocalStorageObjectAsync(filePath, keepStreamOpen: false, cancellationToken);
                if (!getLocalObjectResult.IsSuccessful)
                {
                    return getLocalObjectResult.AsOutput<IEnumerable<StorageMetaData>>();
                }
                
                storageDetails.Add(new StorageMetaData(
                    filePath, getLocalObjectResult.Value.Size, getLocalObjectResult.Value.IsEncrypted,
                    GetMimeType(filePath), File.GetLastWriteTimeUtc(filePath)));

                getLocalObjectResult.Value.Dispose();
            }

            return _outputFactory.Succeed((IEnumerable<StorageMetaData>)storageDetails);
        }

        public Task<IOutput> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return Task.FromResult(_outputFactory.Succeed());
            }

            File.Delete(filePath);

            return Task.FromResult(_outputFactory.Succeed());
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

        private async Task SaveToFileAsync(string filePath, byte[] data, bool isEncrypted, CancellationToken cancellationToken)
        {
            using var fileStream = File.Create(filePath);

            if (isEncrypted)
            {
                await fileStream.WriteAsync(LocalStorageSignatureBytes, cancellationToken);
            }

            await fileStream.WriteAsync(data, cancellationToken);
        }

        private async Task<IOutput<LocalStorageFile>> GetLocalStorageObjectAsync(string filePath, bool keepStreamOpen, CancellationToken cancellationToken)
        {
            var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var dataSize = fileStream.Length;
            var isEncrypted = false;
            if (fileStream.Length >= LocalStorageSignature.Length)
            {
                var signatureBytes = new byte[LocalStorageSignature.Length];
                await fileStream.ReadAsync(signatureBytes, cancellationToken);
                if (signatureBytes.SequenceEqual(LocalStorageSignatureBytes))
                {
                    isEncrypted = true;
                    dataSize -= sizeof(byte) * LocalStorageSignature.Length - sizeof(byte);
                }
                else {
                    fileStream.Position = 0;
                }
            }

            if (!keepStreamOpen)
            {
                await fileStream.DisposeAsync();
            }

            return _outputFactory.Succeed(new LocalStorageFile()
            {
                IsEncrypted = isEncrypted || File.GetAttributes(filePath).HasFlag(FileAttributes.Encrypted),
                Size = dataSize,
                DataStream = keepStreamOpen ? fileStream : null
            });
        }

        #endregion
    }
}
