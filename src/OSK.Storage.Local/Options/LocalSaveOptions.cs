using OSK.Storage.Local.Models;

namespace OSK.Storage.Local.Options
{
    public class LocalSaveOptions
    {
        /// <summary>
        /// Setting this option will attempt to encrypt the data given that an ICryptographicRawDataProcessor has been registered on the DI chain.
        /// </summary>
        public bool Encrypt { get; set; }

        public SavePermissionType SavePermissions { get; set; }
    }
}
