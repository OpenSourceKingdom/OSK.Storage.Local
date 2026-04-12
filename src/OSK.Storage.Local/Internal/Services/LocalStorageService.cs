using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeyRed.Mime;
using OSK.Operations.Outputs;
using OSK.Operations.Outputs.Models;
using OSK.Storage.Abstractions;
using OSK.Storage.Local.Models;
using OSK.Storage.Local.Options;
using OSK.Storage.Local.Ports;

namespace OSK.Storage.Local.Internal.Services;

internal class LocalStorageService(IEnumerable<IRawDataProcessor> dataProcessors, ISerializerProvider serializerProvider, IServiceProvider serviceProvider) : ILocalStorageService
{
    #region Variables

    public const string LocalStorageSignature = "__Common.Storage.LocalStorage__";
    public const string YamlMimeType = "application/x-yaml";

    private static readonly Encoding DefaultEncoding = Encoding.UTF8;
    public static readonly byte[] LocalStorageSignatureBytes = DefaultEncoding.GetBytes(LocalStorageSignature);

    #endregion

    #region ILocalFileStorageService

    public async Task<Output<StorageMetaData>> SaveAsync<T>(T data, string fullFilePath, LocalSaveOptions options, CancellationToken cancellationToken = default)
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
            return Out.InvalidRequest<StorageMetaData>($"A file already exists at the given path, {fullFilePath}, and overwriting was not allowed. To overwrite an existing file, use {SavePermissionType.AllowOverwrite}.", 
                Globals.OSK_LocalStorage);
        }
        if (options.Encrypt && !dataProcessors.Any(processor => processor is ICryptographicRawDataProcessor))
        {
            throw new InvalidOperationException($"Unable to encrypt a file with not cryptographic data processor provided in the application configuration.");
        }

        var directory = Path.GetDirectoryName(fullFilePath);
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        var serializer = serializerProvider.GetSerializer(fullFilePath);
        var serializedData = await serializer.SerializeAsync(data, cancellationToken);

        try
        {
            foreach (var dataProcessor in dataProcessors)
            {
                if (!options.Encrypt && dataProcessor is ICryptographicRawDataProcessor)
                {
                    continue;
                }

                var dataResult = await dataProcessor.ProcessPostSerializationAsync(serializedData, cancellationToken);
                if (!dataResult.IsSuccessful)
                {
                    return dataResult.As<StorageMetaData>();
                }

                serializedData = dataResult.Data;
            }

            await SaveToFileAsync(fullFilePath, serializedData, options.Encrypt, cancellationToken);
        }
        catch (Exception ex)
        {
            return Out.Fault<StorageMetaData>(ex, Globals.OSK_LocalStorage);
        }

        return Out.Success(new StorageMetaData(fullFilePath, serializedData.LongLength, options.Encrypt, GetMimeType(fullFilePath), File.GetLastWriteTimeUtc(fullFilePath)));
    }

    public async Task<Output<StorageObject>> GetAsync(string fullFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fullFilePath))
        {
            throw new ArgumentNullException(nameof(fullFilePath));
        }
        if (!File.Exists(fullFilePath))
        {
            return Out.DataNotFound<StorageObject>($"The following file {fullFilePath} did not exist.");
        }

        var getLocalObjectResult = await GetLocalStorageObjectAsync(fullFilePath, keepStreamOpen: true, cancellationToken);
        if (!getLocalObjectResult.IsSuccessful)
        {
            return getLocalObjectResult.As<StorageObject>();
        }

        return Out.Success((StorageObject)new LocalStorageObject(getLocalObjectResult.Data.DataStream, 
            new StorageMetaData(fullFilePath, getLocalObjectResult.Data.Size, getLocalObjectResult.Data.IsEncrypted, GetMimeType(fullFilePath), File.GetLastWriteTimeUtc(fullFilePath)),
            serviceProvider));
    }

    public async Task<Output<IEnumerable<StorageMetaData>>> GetStorageDetailsAsync(string directoryPath, FileSearchOptions searchOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            throw new ArgumentNullException(nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            return Out.DataNotFound<IEnumerable<StorageMetaData>>($"The path {directoryPath} does not exist.");
        }

        var searchExtension = searchOptions == null || string.IsNullOrWhiteSpace(searchOptions.Extension)
                                    ? string.Empty 
                                    : searchOptions.Extension;

        var directoryFiles = Directory.GetFiles(directoryPath, $"*{searchExtension}");

        var storageDetails = new List<StorageMetaData>();

        foreach (var filePath in directoryFiles)
        {
            var fileInfo = new FileInfo(filePath);
            fileInfo.Refresh();

            var getLocalObjectResult = await GetLocalStorageObjectAsync(filePath, keepStreamOpen: false, cancellationToken);
            if (!getLocalObjectResult.IsSuccessful)
            {
                return getLocalObjectResult.As<IEnumerable<StorageMetaData>>();
            }
            
            storageDetails.Add(new StorageMetaData(filePath, getLocalObjectResult.Data.Size, getLocalObjectResult.Data.IsEncrypted, GetMimeType(filePath), File.GetLastWriteTimeUtc(filePath)));
            getLocalObjectResult.Data.Dispose();
        }

        return Out.Success((IEnumerable<StorageMetaData>)storageDetails);
    }

    public Task<Output> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return Task.FromResult(Out.Success());
        }

        File.Delete(filePath);

        return Task.FromResult(Out.Success());
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

    private async Task<Output<LocalStorageFile>> GetLocalStorageObjectAsync(string filePath, bool keepStreamOpen, CancellationToken cancellationToken)
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

        return Out.Success(new LocalStorageFile()
        {
            IsEncrypted = isEncrypted || File.GetAttributes(filePath).HasFlag(FileAttributes.Encrypted),
            Size = dataSize,
            DataStream = keepStreamOpen ? fileStream : null
        });
    }

    #endregion
}
