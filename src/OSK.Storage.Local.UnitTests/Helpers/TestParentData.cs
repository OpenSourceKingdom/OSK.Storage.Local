namespace OSK.Storage.Local.UnitTests.Helpers
{
    [Discriminator(nameof(DiscriminatorType))]
    public abstract class TestParentData
    {
        public TestParentData() { }

        public TestDiscriminatorType DiscriminatorType { get; set; }
    }
}
