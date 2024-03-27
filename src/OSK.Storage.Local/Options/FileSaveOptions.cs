using OSK.Storage.Local.Models;

namespace OSK.Storage.Local.Options
{
    public class FileSaveOptions
    {
        public EncryptionAlgorithms? EncryptionAlgorithm { get; set; }

        public SavePermissionType SaveOption { get; set; }
    }
}
