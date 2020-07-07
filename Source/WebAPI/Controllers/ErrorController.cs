using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Default error handler controller.
    /// </summary>
    [AllowAnonymous]
    [ApiVersionNeutral]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None)]
    [ApiController]
    public class ErrorController : ControllerBase
    {
        /// <summary>
        /// Return unhandled error info for local development.
        /// </summary>
        /// <param name="webHostEnvironment">Web host environment.</param>
        /// <returns>Error info.</returns>
        [Route("/error-local-development")]
        public IActionResult ErrorLocalDevelopment(
                [FromServices] IWebHostEnvironment webHostEnvironment)
        {
            if (webHostEnvironment.EnvironmentName != "Development")
            {
                throw new InvalidOperationException(
                    "This shouldn't be invoked in non-development environments.");
            }

            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();

            return Problem(
                detail: context.Error.StackTrace,
                title: context.Error.Message);
        }

        /// <summary>
        /// Returns unhandled error info.
        /// </summary>
        /// <returns>Error info.</returns>
        [Route("/error")]
        public IActionResult Error() => Problem();
    }
}
