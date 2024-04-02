using System;
using System.IO;

namespace OSK.Storage.Local.Models
{
    public class LocalStorageFile: IDisposable
    {
        public long Size { get; set; }

        public bool IsEncrypted { get; set; }

        public Stream DataStream { get; set; }

        public void Dispose()
        {
            DataStream?.Dispose();
        }
    }
}
