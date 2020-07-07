using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;
using Xunit;

namespace WebAPI.Tests
{
    public class EchoTest
    {
        private EchoController GetEchoController()
        {
            var logger = new Mock<ILogger<EchoController>>();
            return new EchoController(logger.Object);
        }

        [Fact]
        public void TestPost()
        {
            // Arrange
            var expectedResult = new OkObjectResult("test");

            // Act
            var result = GetEchoController().Post("test");

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void TestGet()
        {
            // Arrange
            var expectedResult = new OkObjectResult("Hello World");

            // Act
            var result = GetEchoController().Get();

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
        }
    }
}
