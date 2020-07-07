using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebAPI.Constants;
using WebAPI.Model.Database;
using WebAPI.Model.Requests;
using WebAPI.Model.Responses;
using WebAPI.Services;
using static WebAPI.Controllers.Helpers.ControllerHelpers;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Users API.
    /// </summary>
    [Route("api/[controller]")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None)]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly CustomDbContext context;
        private readonly ILogger<UsersController> logger;
        private readonly IMSGraphService graph;
        private readonly IReCaptchaService recaptchaService;
        private readonly bool usingB2BAuth = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="configuration">Configuration containing App Settings.</param>
        /// <param name="graph">Microsoft Graph service.</param>
        /// <param name="recaptchaService">Recaptcha service.</param>
        public UsersController(CustomDbContext context, ILogger<UsersController> logger,  IConfiguration configuration, IMSGraphService graph, IReCaptchaService recaptchaService)
        {
            this.context = context;
            this.logger = logger;
            this.graph = graph;
            this.recaptchaService = recaptchaService;

            usingB2BAuth = configuration?.GetValue<string>("AuthScheme") == "AzureAdB2B";
        }

        /// <summary>
        /// POST: api/Users.
        /// </summary>
        /// <param name="request">New User Request.</param>
        /// <returns>if the data was inserted in the required tables of the db.</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserResponse>> Create([FromBody] NewUserRequest request)
        {
            var email = this.GetUserEmail();
            var isAnonymous = email == null;
            email ??= request?.Email; // Anonymous

            // Check mandatory fields
            if (AnyMissing(email, request?.FirstName, request?.LastName) || (isAnonymous && AnyMissing(request?.RecaptchaPayload)))
            {
                return this.Problem("Unable to create user", 400, ErrorCode.MissingMandatoryField);
            }

            if (isAnonymous)
            {
                // Check ReCaptcha
                var isNotABot = await recaptchaService.ValidatePayload(request.RecaptchaPayload);
                if (!isNotABot.Success)
                {
                    return this.Problem("Unable to create user", 400, ErrorCode.BotValidationError, $"Validation error codes: {string.Join(", ", isNotABot.ErrorCodes)}");
                }
            }

            // Check if the user already exists
            var existingUser = context.User.SingleOrDefault(u => u.Email == email);
            if (existingUser != null)
            {
                return this.Problem("Unable to create user", 400, ErrorCode.UserAlreadyExists);
            }

            // People will be User unless specified otherwise
            request.Role ??= RoleId.User;

            // Check requested user role
            var role = context.Role.SingleOrDefault(r => r.Id == request.Role);
            if (role == null)
            {
                return this.Problem("Unable to create user", 400, ErrorCode.InvalidUserRoleId);
            }

            var userStatus = context.UserStatus.Single(s => s.Id == UserStatusId.Pending);

            var user = new User()
            {
                Email = email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = role,
                UserStatus = userStatus,
            };

            await context.User.AddAsync(user);
            await context.SaveChangesAsync();

            return Ok(new UserResponse()
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.Id,
                Status = user.UserStatus.Id,
            });
        }

        /// <summary>
        /// GET: api/Users/{email}.
        /// </summary>
        /// <param name="email">Email of the user.</param>
        /// <returns>User info.</returns>
        [HttpGet("{email}")]
        [Authorize(Roles = "User, Coordinator, Admin")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserResponse>> Get(string email)
        {
            // Only Admins can get info on users other than themselves.
            var callerEmail = this.GetUserEmail();
            if (callerEmail != email && !User.IsInRole(RoleId.Admin))
            {
                return this.Problem("Unable to get user", 403, ErrorCode.UserNotAllowed);
            }

            // Get user.
            var user = await context.User
                .Where(u => u.Email == email)
                .Include(u => u.Role)
                .Include(u => u.UserStatus)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return this.Problem("Unable to get user", 404, ErrorCode.UserNotFound, $"Unknown user: {email}");
            }

            return Ok(new UserResponse()
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.Id,
                Status = user.UserStatus.Id,
            });
        }

        /// <summary>
        /// GET: api/Users.
        /// </summary>
        /// <returns>User info.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserResponse>> GetAll()
        {
            var users = await context.User
                .Include(u => u.Role)
                .Include(u => u.UserStatus)
                .ToListAsync();

            return Ok(new UsersResponse()
            {
                Users = users.Select(u =>
                    new UserResponse()
                    {
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role.Id,
                        Status = u.UserStatus.Id,
                    }),
            });
        }

        /// <summary>
        /// PATCH: api/Users/{email}.
        /// </summary>
        /// <param name="email">Email of the user.</param>
        /// <param name="request">The request with the values to be updated.</param>
        /// <returns>User info.</returns>
        [HttpPatch("{email}")]
        [Authorize(Roles = "User, Coordinator, Admin")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserResponse>> Update(string email, [FromBody] UpdateUserRequest request)
        {
            var callerEmail = this.GetUserEmail();
            bool adminRequired = request.Role != null || request.Status != null || request.Email != null;

            // A User/Coordinator can update her own FirstName and LastName. Anything else requires an Admin.
            if ((callerEmail != email || adminRequired) && !User.IsInRole(RoleId.Admin))
            {
                return this.Problem("Unable to update the user", 403, ErrorCode.UserNotAllowed);
            }

            // Get the user to update.
            var userToUpdate = await context.User
                .Where(u => u.Email == email)
                .Include(u => u.Role)
                .Include(u => u.UserStatus)
                .SingleOrDefaultAsync();

            if (userToUpdate == null)
            {
                return this.Problem("Unable to update the user", 404, ErrorCode.UserNotFound);
            }

            // Update the information if needed.
            userToUpdate.FirstName = request.FirstName ?? userToUpdate.FirstName;
            userToUpdate.LastName = request.LastName ?? userToUpdate.LastName;

            if (request.Role != null)
            {
                var role = context.Role.SingleOrDefault(r => r.Id == request.Role);
                if (role == null)
                {
                    return this.Problem("Unable to update the user", 400, ErrorCode.InvalidUserRoleId);
                }

                userToUpdate.Role = role;
            }

            if (request.Email != null)
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return this.Problem("Unable to update the user", 400, ErrorCode.InvalidEmail);
                }

                userToUpdate.Email = request.Email;
            }

            if (request.Status != null)
            {
                var status = context.UserStatus.SingleOrDefault(r => r.Id == request.Status);
                if (status == null)
                {
                    return this.Problem("Unable to update the user", 400, ErrorCode.InvalidUserStatusId);
                }

                userToUpdate.UserStatus = status;

                // Invite User to B2B.
                // Note we could invite the user and still fail to update the database.
                if (userToUpdate.UserStatus.Id == UserStatusId.Approved && usingB2BAuth)
                {
                    var inviteResult = await graph.InviteUser(userToUpdate.Email);
                    if (inviteResult == MSGraphService.INVITEERROR)
                    {
                        return this.Problem("Unable to update the user", 400, ErrorCode.UnableToSendB2BInvitation);
                    }
                }
            }

            context.User.Update(userToUpdate);
            await context.SaveChangesAsync();

            return Ok(new UserResponse()
            {
                Email = userToUpdate.Email,
                FirstName = userToUpdate.FirstName,
                LastName = userToUpdate.LastName,
                Role = userToUpdate.Role.Id,
                Status = userToUpdate.UserStatus.Id,
            });
        }

        /// <summary>
        /// DELETE: api/Users/{email}.
        /// </summary>
        /// <param name="email">User to delete.</param>
        /// <returns>Ok.</returns>
        [HttpDelete("{email}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Delete(string email)
        {
            // Get the user to be deleted,
            var userToDelete = await context
                .User
                .Include(u => u.Request).ThenInclude(r => r.RequestDetails)
                .SingleOrDefaultAsync(u => u.Email == email);

            if (userToDelete == null)
            {
                return this.Problem("Unable to delete the user", 404, ErrorCode.UserNotFound);
            }

            // Delete the user and related data.
            using var transaction = await context.Database.BeginTransactionAsync();

            var requestsDetails = userToDelete.Request.SelectMany(r => r.RequestDetails);

            context.RequestDetail.RemoveRange(requestsDetails);
            await context.SaveChangesAsync();

            context.Request.RemoveRange(userToDelete.Request);
            await context.SaveChangesAsync();

            context.User.Remove(userToDelete);
            await context.SaveChangesAsync();

            transaction.Commit();

            return Ok();
        }
    }
}
