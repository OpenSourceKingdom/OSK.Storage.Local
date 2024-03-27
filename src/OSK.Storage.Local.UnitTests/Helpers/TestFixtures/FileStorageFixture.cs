using System.Text;
using Xunit;

namespace OSK.Storage.Local.UnitTests.Helpers.TestFixtures
{
    public class FileStorageFixture : IDisposable
    {
        public static readonly string TestDirectory = Path.Combine(".", "TestData");
        private Encoding _encoding = Encoding.UTF8;

        public FileStorageFixture()
        {
            Directory.CreateDirectory(TestDirectory);

            Assert.True(Directory.Exists(TestDirectory));
        }

        public void Dispose()
        {
            if (Directory.Exists(TestDirectory))
            {
                Directory.Delete(TestDirectory, true);
            }

            Assert.False(Directory.Exists(TestDirectory));
        }

        #region Helpers

        public void SetEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public IEnumerable<string> CreateTestTextFiles(params string[] text)
        {
            ClearTestDirectory();

            var fileNames = new List<string>();

            for (int i = 0; i < text.Length; i++)
            {
                var filePath = CreateTestFile($"TestFile-{i}", text[i]);

                fileNames.Add(filePath);
            }

            return fileNames;
        }

        public string CreateTestFile(string fileNameWithoutExtension, string content, string extension = ".txt")
        {
            var filePath = GetFilePath($"{fileNameWithoutExtension}{extension}");
            using var fileStream = File.Create(filePath);

            fileStream.Write(_encoding.GetBytes(content));

            return filePath;
        }

        public string GetFilePath(string fileNameWithExtension)
        {
            return Path.Combine(TestDirectory, fileNameWithExtension);
        }

        public void ClearTestDirectory()
        {
            var directoryInfo = new DirectoryInfo(TestDirectory);
            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in directoryInfo.GetDirectories())
            {
                dir.Delete(true);
            }

            var directoryExists = Directory.Exists(TestDirectory);

            Assert.True(directoryExists);
        }

        #endregion
    }
}
