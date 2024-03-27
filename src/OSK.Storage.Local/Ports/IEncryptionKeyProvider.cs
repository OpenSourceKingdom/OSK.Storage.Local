namespace OSK.Storage.Local.Ports
{
    public interface IEncryptionKeyProvider
    {
        byte[] GetKey();
    }
}
