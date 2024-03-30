namespace OSK.Storage.Local.UnitTests.Helpers
{
    public static class ByteHelper
    {
        #region Variables

        private static readonly Random Random = new Random();

        #endregion

        public static byte[] CreateBytes(int size)
        {
            var bytes = new byte[size];

            Random.NextBytes(bytes);

            return bytes;
        }
    }
}
