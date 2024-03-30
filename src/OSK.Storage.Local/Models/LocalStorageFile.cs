using System.IO;

namespace OSK.Storage.Local.Models
{
    public class LocalStorageFile
    {
        public long Size { get; set; }

        public bool IsEncrypted { get; set; }

        public Stream DataStream { get; set; }
    }
}
