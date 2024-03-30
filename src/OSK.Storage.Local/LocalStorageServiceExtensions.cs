using OSK.Functions.Outputs.Abstractions;
using OSK.Storage.Abstractions;
using OSK.Storage.Local.Options;
using OSK.Storage.Local.Ports;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.Storage.Local
{
    public static class LocalStorageServiceExtensions
    {
        public static Task<IOutput<IEnumerable<StorageMetaData>>> GetStorageDetailsAsync(this ILocalStorageService service, string directoryPath, string extension = null,
            CancellationToken cancellationToken = default)
            => service.GetStorageDetailsAsync(directoryPath, new FileSearchOptions()
            {
                Extension = extension
            }, cancellationToken);
    }
}
