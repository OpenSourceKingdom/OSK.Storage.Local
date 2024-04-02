using System.Text;
using Xunit;

namespace OSK.Storage.Local.UnitTests.Helpers.TestFixtures
{
    public class FileStorageFixture : IDisposable
    {
        private static readonly string TestDirectoryTemplate = Path.Combine(".", "TestData");
        private Encoding _encoding = Encoding.UTF8;

        private string _testDirectory;

        public FileStorageFixture()
        {
            NewDirectory();
        }

        public void NewDirectory()
        {
            _testDirectory = TestDirectoryTemplate;
            Directory.CreateDirectory(_testDirectory);

            Assert.True(Directory.Exists(_testDirectory));
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }

            Assert.False(Directory.Exists(_testDirectory));
        }

        #region Helpers

        public string TestDirectory => _testDirectory;

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
            return Path.Combine(_testDirectory, fileNameWithExtension);
        }

        public void ClearTestDirectory()
        {
            var directoryInfo = new DirectoryInfo(_testDirectory);
            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in directoryInfo.GetDirectories())
            {
                dir.Delete(true);
            }

            var directoryExists = Directory.Exists(_testDirectory);

            Assert.True(directoryExists);
        }

        #endregion
    }
}
