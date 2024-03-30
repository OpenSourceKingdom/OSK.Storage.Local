using System.Net;

namespace OSK.Storage.Local.UnitTests.Helpers
{
    public class NonSerializableObject
    {
        public int A { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public SerializableObject SerializableObject { get; set; }
    }
}
