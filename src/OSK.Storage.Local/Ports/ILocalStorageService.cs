using OSK.Hexagonal.MetaData;
using OSK.Storage.Abstractions;
using OSK.Storage.Local.Options;

namespace OSK.Storage.Local.Ports
{
    [HexagonalPort(HexagonalPort.Primary)]
    public interface ILocalStorageService : IStorageService<LocalSaveOptions, FileSearchOptions>
    {
    }
}
