using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAPI.Constants;
using WebAPI.Controllers.Helpers;
using WebAPI.Model.Database;
using WebAPI.Model.Responses;
using static WebAPI.Controllers.Helpers.ControllerHelpers;

namespace WebAPI.Controllers.V2
{
    /// <summary>
    /// Requests endpoint.
    /// </summary>
    [ApiVersion("2.0")]
    [Route("api/[controller]")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None)]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly ILogger<RequestsController> logger;
        private readonly CustomDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestsController"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="logger">Logger.</param>
        public RequestsController(CustomDbContext context, ILogger<RequestsController> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        /// <summary>
        /// GET: api/Requests.
        /// This is not implemented, it's here just to show how to handle API versioning.
        /// </summary>
        /// <returns>All the requests that a user can access.</returns>
        [HttpGet]
        [Authorize(Roles = "User, Coordinator")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        public ActionResult GetAll()
            => this.Problem("Unable to get requests", 400, ErrorCode.NotImplemented);
    }
}
