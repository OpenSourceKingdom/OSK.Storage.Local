namespace OSK.Storage.Local.UnitTests.Helpers
{
    public class TestFile
    {
        public string Name { get; set; }

        public DateTime Date { get; set; }

        public IEnumerable<TestParentData> Data { get; set; }
    }
}
