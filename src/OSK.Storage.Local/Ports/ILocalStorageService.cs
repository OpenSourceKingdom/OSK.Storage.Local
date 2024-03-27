using OSK.Storage.Abstractions;
using OSK.Storage.Local.Options;

namespace OSK.Storage.Local.Ports
{
    public interface ILocalStorageService : IStorageService<FileSaveOptions, FileSearchOptions>
    {
    }
}
