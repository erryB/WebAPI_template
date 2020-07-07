using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Echo endpoint.
    /// </summary>
    [Route("/")]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None)]
    [ApiController]
    public class EchoController : ControllerBase
    {
        private readonly ILogger<EchoController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EchoController"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public EchoController(ILogger<EchoController> logger)
            => this.logger = logger;

        /// <summary>
        /// POST: /.
        /// </summary>
        /// <param name="message">Message to echo.</param>
        /// <returns>The same message we got as parameter.</returns>
        [HttpPost]
        public IActionResult Post([FromBody] string message)
            => Ok(message);

        /// <summary>
        /// GET: /.
        /// </summary>
        /// <returns>The string 'Hello World'.</returns>
        [HttpGet]
        public IActionResult Get()
            => Ok("Hello World");
    }
}
