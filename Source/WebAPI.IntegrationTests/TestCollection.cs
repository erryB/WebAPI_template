using WebAPI.IntegrationTests.Common;
using Xunit;

namespace WebAPI.IntegrationTests
{
    [CollectionDefinition("Integration Tests")]
    public class TestCollection : ICollectionFixture<CustomWebApplicationFactory<Startup>>
    {
    }
}
