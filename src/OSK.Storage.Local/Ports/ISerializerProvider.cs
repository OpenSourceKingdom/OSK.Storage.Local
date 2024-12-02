using OSK.Hexagonal.MetaData;
using OSK.Serialization.Abstractions;

namespace OSK.Storage.Local.Ports
{
    /// <summary>
    /// Retrives a valid <see cref="ISerializer"/> that is capable of serializing and deserializing a given file path
    /// </summary>
    [HexagonalPort(HexagonalPort.Primary)]
    public interface ISerializerProvider
    {
        ISerializer GetSerializer(string filePath);
    }
}
