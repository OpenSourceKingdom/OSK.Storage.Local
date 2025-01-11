using OSK.Hexagonal.MetaData;

namespace OSK.Storage.Local.Ports
{
    /// <summary>
    /// Represents a <see cref="IRawDataProcessor"/> that is focused on cryptographic encryption functions. This is meant to be used for encryption related purposes; for non-ecryption cryptographic purposes, please add a <see cref="IRawDataProcessor"/> instead. 
    /// </summary>
    [HexagonalIntegration(HexagonalIntegrationType.IntegrationOptional)]
    public interface ICryptographicRawDataProcessor: IRawDataProcessor
    {
    }
}
