using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;

namespace WebAPI.Tests.Common
{
    public class MockHelpers
    {
        public static ProblemDetailsFactory MockProblemDetailsFactory()
        {
            var problemDetailsFactory = new Mock<ProblemDetailsFactory>();
            problemDetailsFactory.Setup(pdf => pdf.CreateProblemDetails(
                    It.IsAny<HttpContext>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())
                )
                .Returns<HttpContext, int?, string, string, string, string>((p1, p2, p3, p4, p5, p6) => new ProblemDetails() { Status = p2 });

            return problemDetailsFactory.Object;
        }
    }
}
