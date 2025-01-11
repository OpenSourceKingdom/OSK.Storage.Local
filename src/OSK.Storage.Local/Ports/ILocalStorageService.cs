using OSK.Hexagonal.MetaData;
using OSK.Storage.Abstractions;
using OSK.Storage.Local.Options;

namespace OSK.Storage.Local.Ports
{
    [HexagonalIntegration(HexagonalIntegrationType.LibraryProvided, HexagonalIntegrationType.ConsumerPointOfEntry)]
    public interface ILocalStorageService : IStorageService<LocalSaveOptions, FileSearchOptions>
    {
    }
}
