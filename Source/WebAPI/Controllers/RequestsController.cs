using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAPI.Constants;
using WebAPI.Controllers.Helpers;
using WebAPI.Model.Database;
using WebAPI.Model.Requests;
using WebAPI.Model.Responses;
using static WebAPI.Controllers.Helpers.ControllerHelpers;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Requests endpoint.
    /// </summary>
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
        /// POST: api/Requests.
        /// </summary>
        /// <param name="request">New Request.</param>
        /// <returns>if the data was inserted in the required tables of the db and a reference to the Request.</returns>
        [HttpPost]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(NewRequestResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<NewRequestResponse>> Create([FromBody] NewRequestRequest request)
        {
            var email = this.GetUserEmail();

            // Check mandatory parameters.
            if (AnyMissing(request?.SelectedProducts))
            {
                return this.Problem("Unable to create request", 400, ErrorCode.MissingMandatoryField);
            }

            // Check for negative quantities.
            if (request.SelectedProducts.Any(p => p.Quantity < 0))
            {
                return this.Problem("Unable to create request", 400, ErrorCode.NegativeValue);
            }

            // Create new request.
            using var transaction = await context.Database.BeginTransactionAsync();

            Guid refNo;
            if (request.RefNo == null)
            {
                // Unknown refNo. Assign new one.
                refNo = Guid.NewGuid();
            }
            else
            {
                // Known refNo. Set previous request with same refNo as not being current anymore.
                refNo = request.RefNo.Value;
                var existingRequest = await context
                    .Request
                    .Include(r => r.RequestStatus)
                    .Include(r => r.User)
                    .SingleOrDefaultAsync(r => r.RefNo.Equals(refNo) && r.IsCurrent == 1);

                if (existingRequest == null)
                {
                    return this.Problem("Unable to create request", 400, ErrorCode.InvalidRefNo);
                }

                if (existingRequest.User.Email != email)
                {
                    return this.Problem("Unable to create request", 403, ErrorCode.UserNotAllowed);
                }

                existingRequest.IsCurrent = 0;
                context.Request.Update(existingRequest);
                await context.SaveChangesAsync();
            }

            var user = await context.User.SingleAsync(u => u.Email == email);
            var status = await context.RequestStatus.SingleAsync(s => s.Id == RequestStatusId.Pending);

            var newRequest = new Request()
            {
                Id = Guid.NewGuid(),
                RefNo = refNo,
                User = user,
                IsCurrent = 1,
                RequestStatus = status,
            };

            var newRequestDetails = request.SelectedProducts.Select(sp =>
                new RequestDetail()
                {
                    Id = Guid.NewGuid(),
                    Qty = sp.Quantity,
                    Product = context.Product.SingleOrDefault(p => p.Id == sp.Id),
                    Request = newRequest,
                }).ToList();

            if (newRequestDetails.Any(rd => rd.Product == null))
            {
                return this.Problem("Unable to create request", 400, ErrorCode.InvalidProductId);
            }

            await context.Request.AddAsync(newRequest);
            await context.SaveChangesAsync();

            await context.RequestDetail.AddRangeAsync(newRequestDetails);
            await context.SaveChangesAsync();

            transaction.Commit();

            return Ok(new NewRequestResponse() { RefNo = newRequest.RefNo });
        }

        /// <summary>
        /// PATCH: api/Requests/{refNo}.
        /// </summary>
        /// <param name="refNo">Request to update.</param>
        /// <param name="request">Update information.</param>
        /// <returns>If the update was successful.</returns>
        [HttpPatch("{refNo}")]
        [Authorize(Roles = "Coordinator")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UpdateRequestResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<UpdateRequestResponse>> Update(string refNo, [FromBody] UpdateRequestRequest request)
        {
            // Check mandatory parameters.
            if (AnyMissing(request?.Status))
            {
                return this.Problem("Unable to update the request", 400, ErrorCode.MissingMandatoryField);
            }

            // Ensure valid target status.
            var status = await context.RequestStatus.SingleOrDefaultAsync(s => s.Id == request.Status);
            if (status == null)
            {
                return this.Problem("Unable to update the request", 400, ErrorCode.InvalidRequestStatusId);
            }

            // Ensure valid request to update.
            var requestToUpdate = await context.Request.SingleOrDefaultAsync(r => r.RefNo.ToString() == refNo && r.IsCurrent == 1);
            if (requestToUpdate == null)
            {
                return this.Problem("Unable to update the request", 400, ErrorCode.InvalidRefNo);
            }

            // Update request.
            requestToUpdate.RequestStatus = status;
            context.Request.Update(requestToUpdate);
            await context.SaveChangesAsync();

            return Ok(new UpdateRequestResponse()
            {
                RefNo = requestToUpdate.RefNo,
                Status = requestToUpdate.RequestStatus.Id,
            });
        }

        /// <summary>
        /// GET: api/Requests.
        /// </summary>
        /// <returns>All the requests that a user can access.</returns>
        [HttpGet]
        [Authorize(Roles = "User, Coordinator")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(RequestsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<RequestsResponse>> GetAll()
        {
            var callerEmail = this.GetUserEmail();

            // Get latest version of the requests. A User can only see her requests. A Coordinator can see all of them.
            var requests = await context
                .Request
                .Where(r => ((User.IsInRole(RoleId.User) && r.User.Email == callerEmail) || User.IsInRole(RoleId.Coordinator))
                    && r.IsCurrent == 1)
                .Include(r => r.User)
                .Include(r => r.RequestDetails).ThenInclude(rd => rd.Product)
                .Include(r => r.RequestStatus)
                .ToListAsync();

            // Return the details of the requests.
            var requestResponses = requests.Select(r => new RequestResponse()
            {
                RefNo = r.RefNo,
                RequestStatus = r.RequestStatus.Id,
                UserEmail = r.User.Email,
                RequestDetails = r.RequestDetails.Select(rd => new RequestDetailResponse()
                {
                    Qty = rd.Qty,
                    ProductDisplayName = rd.Product.DisplayName,
                    ProductPrice = rd.Product.Price,
                    ProductPriceCurrency = rd.Product.PriceCurrency,
                }).ToList(),
            });

            return Ok(new RequestsResponse() { Requests = requestResponses });
        }

        /// <summary>
        /// GET: api/Requests/download.
        /// </summary>
        /// <returns>All the requests that a user can access in CSV format.</returns>
        [HttpGet("download")]
        [Produces("text/csv")]
        [Authorize(Roles = "User, Coordinator")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(IEnumerable<RequestCSVResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RequestCSVResponse>>> GetAllAsCsv()
        {
            var callerEmail = this.GetUserEmail();

            // Get latest version of the requests. A User can only see her requests. A Coordinator can see all of them.
            var requests = await context
                .Request
                .Where(r => ((User.IsInRole(RoleId.User) && r.User.Email == callerEmail) || User.IsInRole(RoleId.Coordinator))
                    && r.IsCurrent == 1)
                .Include(r => r.User)
                .Include(r => r.RequestDetails).ThenInclude(rd => rd.Product)
                .Include(r => r.RequestStatus)
                .ToListAsync();

            // Return the details of the requests.
            var requestResponses = requests.SelectMany(r => r.RequestDetails.Select(rd => new RequestCSVResponse()
            {
                RefNo = r.RefNo,
                RequestStatus = r.RequestStatus.Id,
                UserEmail = r.User.Email,
                Qty = rd.Qty,
                ProductDisplayName = rd.Product.DisplayName,
                ProductPrice = rd.Product.Price,
                ProductPriceCurrency = rd.Product.PriceCurrency,
            })).ToList();

            return Ok(requestResponses);
        }

        /// <summary>
        /// DELETE: api/Requests/{refNo}.
        /// </summary>
        /// <param name="refNo">Request to delete.</param>
        /// <returns>Ok.</returns>
        [HttpDelete("{refNo}")]
        [Authorize(Roles = "User, Coordinator")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Delete(string refNo)
        {
            var callerEmail = this.GetUserEmail();

            // Get all versions of the requests and their details.
            var requests = await context
                .Request
                .Where(r => r.RefNo.ToString() == refNo)
                .Include(r => r.User)
                .Include(r => r.RequestDetails)
                .ToListAsync();

            if (requests.Count == 0)
            {
                return this.Problem("Unable to delete the request", 404, ErrorCode.InvalidRefNo);
            }

            // A User can only delete her own requests. A Coordinator can delete any.
            if (User.IsInRole(RoleId.User) && requests.Any(r => r.User.Email != callerEmail))
            {
                return this.Problem("Unable to delete the request", 403, ErrorCode.UserNotAllowed);
            }

            // Delete all versions of the request and their details.
            using var transaction = await context.Database.BeginTransactionAsync();

            var requestDetails = requests.SelectMany(r => r.RequestDetails);
            context.RequestDetail.RemoveRange(requestDetails);
            await context.SaveChangesAsync();

            context.Request.RemoveRange(requests);
            await context.SaveChangesAsync();

            transaction.Commit();

            return Ok();
        }
    }
}
