using OSK.Operations.Outputs.Models;
using OSK.Hexagonal.MetaData;
using OSK.Storage.Abstractions;
using OSK.Storage.Local.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local.Ports;

[HexagonalIntegration(HexagonalIntegrationType.LibraryProvided, HexagonalIntegrationType.ConsumerPointOfEntry)]
public interface ILocalStorageService : IStorageService
{
    Task<Output<StorageMetaData>> SaveAsync<T>(T data, string fullStoragePath, LocalSaveOptions options = null, CancellationToken cancellationToken = default);
    Task<Output<IEnumerable<StorageMetaData>>> GetStorageDetailsAsync(string directoryPath, FileSearchOptions options = null, CancellationToken cancellationToken = default);
    Task<Output> DeleteAsync(string fullStoragePath, CancellationToken cancellationToken = default);
}
