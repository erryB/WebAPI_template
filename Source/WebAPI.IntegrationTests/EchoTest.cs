using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebAPI.IntegrationTests.Common;
using Xunit;

namespace WebAPI.IntegrationTests
{
    [Collection("Integration Tests")]
    public class EchoTest
    {
        private readonly CustomWebApplicationFactory<Startup> factory;

        public EchoTest(CustomWebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task TestPost()
        {
            // Arrange
            var client = factory.CreateClient();
            var body = new StringContent("\"Hello World!\"", Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("", body);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TestGet()
        {
            // Arrange
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
        }
    }
}
