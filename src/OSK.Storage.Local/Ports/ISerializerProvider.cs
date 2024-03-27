using OSK.Serialization.Abstractions;

namespace OSK.Storage.Local.Ports
{
    public interface ISerializerProvider
    {
        ISerializer GetSerializer(string filePath);
    }
}
