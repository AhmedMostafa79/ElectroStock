namespace ElectroStock.Tests.IntegrationTests.Fixtures
{
    // This tells xUnit to share the same TestContainersFixture instance across all tests in the collection (so they don't each spin up their own database
    [CollectionDefinition("TestContainers")]
    public class TestCollection : ICollectionFixture<TestContainersFixture>
    {
        // This class has no code. It's just a marker.

    }
}
