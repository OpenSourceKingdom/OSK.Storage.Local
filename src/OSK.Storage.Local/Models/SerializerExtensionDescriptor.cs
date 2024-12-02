using System;
using System.Collections.Generic;

namespace OSK.Storage.Local.Models
{
    public class SerializerExtensionDescriptor
    {
        public Type SerializerType { get; set; }

        public ISet<string> Extensions { get; set; }
    }
}
