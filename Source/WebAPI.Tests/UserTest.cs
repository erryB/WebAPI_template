using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAPI.Constants;
using WebAPI.Controllers;
using WebAPI.Model;
using WebAPI.Model.Database;
using WebAPI.Model.Requests;
using WebAPI.Model.Responses;
using WebAPI.Services;
using WebAPI.Tests.Common;
using Xunit;
using static WebAPI.Tests.Common.DbTableFactory;
using static WebAPI.Tests.Common.ErrorHelpers;

namespace WebAPI.Tests
{
    public class UserTest
    {
        private readonly TestDbContext db;
        private readonly Role userRole;
        private readonly User user1, coordinator1, admin1, userPending;
        private readonly User[] users;

        public UserTest()
        {
            db = new TestDbContext();
            int i;

            Role coordinatorRole, adminRole;
            db.AddRange(new[]
            {
                userRole = RoleUser(),
                coordinatorRole = RoleCoordinator(),
                adminRole = RoleAdmin(),
            });

            UserStatus approved, pending, rejected;
            db.AddRange(new[]
            {
                approved = UserStatusApproved(),
                pending = UserStatusPending(),
                rejected = UserStatusRejected(),
            });

            db.AddRange(users = new[]
            {
                user1 = User(i=1, userRole, approved),
                admin1 = User(++i, adminRole, approved),
                coordinator1 = User(++i, coordinatorRole, approved),
                userPending = User(++i, userRole, pending),
                User(++i, userRole, rejected),
            });

            db.SaveChanges();
        }

        private UsersController GetUsersController(User user, AuthScheme authScheme, Mock<IMSGraphService> graphMock = null, Mock<IReCaptchaService> reCaptchaMock = null)
        {
            Mock<IConfiguration> configuration = new Mock<IConfiguration>();
            var configurationSectionAuthScheme = new Mock<IConfigurationSection>();
            var logger = new Mock<ILogger<UsersController>>();
            graphMock ??= new Mock<IMSGraphService>();
            reCaptchaMock ??= new Mock<IReCaptchaService>();

            ClaimsPrincipal userClaims = new ClaimsPrincipal();
            string emailClaim = string.Empty;

            switch (authScheme)
            {
                case AuthScheme.AzureADB2BGuest:
                    configurationSectionAuthScheme.Setup(a => a.Value).Returns("AzureAdB2B");
                    configuration.Setup(a => a.GetSection("AuthScheme")).Returns(configurationSectionAuthScheme.Object);
                    emailClaim = ClaimTypes.Email;
                    break;
                case AuthScheme.AzureADB2BNative:
                    configurationSectionAuthScheme.Setup(a => a.Value).Returns("AzureAdB2B");
                    configuration.Setup(a => a.GetSection("AuthScheme")).Returns(configurationSectionAuthScheme.Object);
                    emailClaim = ClaimTypes.Upn;
                    break;
                case AuthScheme.AzureADB2C:
                    configurationSectionAuthScheme.Setup(a => a.Value).Returns("AzureAdB2C");
                    configuration.Setup(a => a.GetSection("AuthScheme")).Returns(configurationSectionAuthScheme.Object);
                    emailClaim = "emails";
                    break;
            }

            if (user != null)
            {
                if (user.Role != null)
                {
                    userClaims.AddIdentity(new ClaimsIdentity(new[]
                    {
                        new Claim(emailClaim, user.Email),
                        new Claim(ClaimTypes.Role, user.Role.Id)
                    }));
                } 
                else
                {
                    userClaims.AddIdentity(new ClaimsIdentity(new[]
                    {
                        new Claim(emailClaim, user.Email),
                    }));
                }
            }
            
            var controller = new UsersController(db, logger.Object, configuration.Object, graphMock.Object, reCaptchaMock.Object)
            {
                ProblemDetailsFactory = MockHelpers.MockProblemDetailsFactory(),
            };

            controller.ControllerContext.HttpContext = new DefaultHttpContext()
            {
                User = userClaims,
            };

            return controller;
        }

        private NewUserRequest GetNewUserRequest(string roleId = null)
            => new NewUserRequest()
            {
                FirstName = "TestFirstName",
                LastName = "TestLastName",
                Role = roleId,
            };

        private UserResponse ExpectedUserResponse(User user)
            => new UserResponse()
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.Id,
                Status = user.UserStatus.Id,
            };

        private UserResponse ExpectedUserResponse(string email, NewUserRequest request, string roleId)
            => new UserResponse()
            {
                Email = email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = roleId ?? RoleId.User,
                Status = UserStatusId.Pending,
            };

        private User ExpectedUserRow(User user, NewUserRequest request)
            => new User()
            {
                Email = user?.Email ?? request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = new Role() { Id = request.Role ?? RoleId.User},
                UserStatus = new UserStatus() { Id = UserStatusId.Pending },
            };

        private User ExpectedUserRow(User user, UpdateUserRequest request)
            => new User()
            {
                Email = request.Email ?? user.Email,
                FirstName = request.FirstName ?? user.FirstName,
                LastName = request.LastName ?? user.LastName,
                Role = new Role() { Id = request.Role ?? user.Role.Id },
                UserStatus = new UserStatus() { Id = request.Status ?? user.UserStatus.Id },
            };

        [Theory]
        [InlineData(AuthScheme.AzureADB2BGuest, null)]
        [InlineData(AuthScheme.AzureADB2BNative, null)]
        [InlineData(AuthScheme.AzureADB2C, null)]
        [InlineData(AuthScheme.AzureADB2C, RoleId.User)]
        [InlineData(AuthScheme.AzureADB2C, RoleId.Coordinator)]
        [InlineData(AuthScheme.AzureADB2C, RoleId.Admin)]
        public async Task TestCreateUser(AuthScheme authscheme, string roleId)
        {
            // Arrange
            var email = "test@email.com";
            User user = authscheme == AuthScheme.AzureADB2BGuest ? null : new User() { Email = email }; 

            var request = GetNewUserRequest(roleId);
            
            Mock<IReCaptchaService> reCaptchaMock = null;

            if (authscheme == AuthScheme.AzureADB2BGuest)
            {
                request.Email = email;
                request.RecaptchaPayload = "recaptchaPayload";
                
                reCaptchaMock = new Mock<IReCaptchaService>();
                reCaptchaMock.Setup(a => a.ValidatePayload(It.IsAny<string>())).Returns(Task.FromResult(new ReCaptchaResponse()
                {
                    Success = true,
                    ErrorCodes = null,
                    ChallengeTs = null,
                }));
            }

            // Act
            var result = await GetUsersController(user, authscheme, reCaptchaMock: reCaptchaMock).Create(request);

            // Assert
            var expectedResult = new OkObjectResult(ExpectedUserResponse(email, request, roleId));
            result.Result.Should().BeEquivalentTo(expectedResult);

            var expectedUserRow = ExpectedUserRow(user, request);
            var userRow = db.User.Single(u => u.Email == email);
            userRow.Should().BeEquivalentTo(expectedUserRow, options => options.Excluding(u => u.Id).Excluding(u => u.Request).Excluding(u => u.Role.Users).Excluding(u => u.UserStatus.Users));

            if (authscheme == AuthScheme.AzureADB2BGuest)
            {
                reCaptchaMock.Verify(reCaptcha => reCaptcha.ValidatePayload(request.RecaptchaPayload), Times.Once());
            }                
        }

        [Theory]
        [InlineData("missingEmail", ErrorCode.MissingMandatoryField)]
        [InlineData("missingFirstName", ErrorCode.MissingMandatoryField)]
        [InlineData("missingLastName", ErrorCode.MissingMandatoryField)]
        [InlineData("missingRecaptchaPayload", ErrorCode.MissingMandatoryField)]
        [InlineData("invalidRecaptcha", ErrorCode.BotValidationError)]
        [InlineData("duplicateUser", ErrorCode.UserAlreadyExists)]
        [InlineData("invalidRequestedRole", ErrorCode.InvalidUserRoleId)]
        public async Task TestCreateUserInvalidFields(string useCase, ErrorCode errorCode)
        {
            // Arrange
            User user = null; // Anonymous access

            string errorDetails = null;

            var reCaptchaMock = new Mock<IReCaptchaService>();
            reCaptchaMock.Setup(a => a.ValidatePayload(It.IsAny<string>())).Returns(Task.FromResult(new ReCaptchaResponse()
            {
                Success = true,
                ErrorCodes = null,
                ChallengeTs = null,
            }));

            var request = GetNewUserRequest();
            request.RecaptchaPayload = "valid";

            if(useCase != "missingEmail")
            {
                request.Email = "test@email.com";
            }
            switch(useCase)
            {
                case "missingFirstName":
                    request.FirstName = null;
                    break;
                case "missingLastName":
                    request.LastName = null;
                    break;
                case "missingRecaptchaPayload":
                    request.RecaptchaPayload = null;
                    break;
                case "invalidRecaptcha":
                    request.RecaptchaPayload = "invalid";

                    reCaptchaMock = new Mock<IReCaptchaService>();
                    var errorCodes = new string[] { "error-code-1", "error-code-2" };
                    reCaptchaMock.Setup(a => a.ValidatePayload(It.IsAny<string>())).Returns(Task.FromResult(new ReCaptchaResponse()
                    {
                        Success = false,
                        ErrorCodes = errorCodes,
                        ChallengeTs = null,
                    }));

                    errorDetails = $"Validation error codes: {string.Join(", ", errorCodes)}";
                    break;
                case "duplicateUser":
                    request.Email = user1.Email;
                    break;
                case "invalidRequestedRole":
                    request.Role = "invalid";
                    break;
            }

            // Act
            var result = await GetUsersController(user, AuthScheme.AzureADB2BGuest, reCaptchaMock: reCaptchaMock).Create(request);

            // Assert
            result.Result.Should().BeEquivalentTo(ExpectedErrorResult(400, errorCode, errorDetails));
        }

        [Theory]
        [InlineData("AdminGettingUser")]
        [InlineData("UserGettingHerself")]
        public async Task TestGetUser(string useCase)
        {
            // Arrange
            var userToGet = user1;
            var callingUser = useCase == "AdminGettingUser" ? admin1 : user1;

            // Act
            var result = await GetUsersController(callingUser, AuthScheme.AzureADB2C).Get(userToGet.Email);

            // Assert
            var expectedResult = new OkObjectResult(ExpectedUserResponse(userToGet));
            result.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task TestGetUserNotAdmin()
        {
            // Arrange
            var userToGet = user1;
            var callingUser = coordinator1;

            // Act
            var result = await GetUsersController(callingUser, AuthScheme.AzureADB2C).Get(userToGet.Email);

            // Assert
            result.Result.Should().BeEquivalentTo(ExpectedErrorResult(403, ErrorCode.UserNotAllowed));
        }

        [Fact]
        public async Task TestGetUserUnknown()
        {
            // Arrange
            var email = "unknown@email.com";
            var callingUser = admin1;

            // Act
            var result = await GetUsersController(callingUser, AuthScheme.AzureADB2C).Get(email);

            // Assert
            result.Result.Should().BeEquivalentTo(ExpectedErrorResult(404, ErrorCode.UserNotFound, $"Unknown user: {email}"));
        }

        [Fact]
        public async Task TestGetAllUsers()
        {
            // Arrange
            var user = admin1;

            // Act
            var result = await GetUsersController(user, AuthScheme.AzureADB2C).GetAll();

            // Assert
            var expectedResult = new OkObjectResult(new UsersResponse()
            {
                Users = users.Select(u => ExpectedUserResponse(u)),
            });

            result.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Theory]
        [InlineData("adminUpdatingUser")]
        [InlineData("userUpdatingHerself")]
        public async Task TestUpdateUser(string useCase)
        {
            // Arrange
            var callingUser = useCase == "adminUpdatingUser" ? admin1 : user1;
            var userToUpdate = useCase == "adminUpdatingUser" ? userPending : user1;

            var request = new UpdateUserRequest()
            {
                FirstName = "newName",
                LastName = "newLastName",
            };

            if(useCase == "adminUpdatingUser")
            {
                request.Email = "newEmail@email.com";
                request.Role = RoleId.Coordinator;
                request.Status = UserStatusId.Approved;
            }

            var graphMock = new Mock<IMSGraphService>();
            graphMock.Setup(a => a.InviteUser(It.IsAny<string>())).Returns(Task.FromResult(MSGraphService.INVITEPENDING));

            // Act
            var result = await GetUsersController(callingUser, AuthScheme.AzureADB2BNative, graphMock).Update(userToUpdate.Email, request);

            // Assert
            var expectedResult = new OkObjectResult(new UserResponse()
            {
                Email = request.Email ?? userToUpdate.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.Role ?? userToUpdate.Role.Id,
                Status = request.Status ?? userToUpdate.UserStatus.Id,
            });

            result.Result.Should().BeEquivalentTo(expectedResult);

            var email = useCase == "adminUpdatingUser" ? request.Email : userToUpdate.Email;

            var expectedUserRow = ExpectedUserRow(userToUpdate, request);
            var userRow = db.User.Single(u => u.Email ==  email);
            userRow.Should().BeEquivalentTo(expectedUserRow, options => options.Excluding(u => u.Id).Excluding(u => u.Request).Excluding(u => u.Role.Users).Excluding(u => u.UserStatus.Users));

            graphMock.Verify(graph => graph.InviteUser(email), useCase == "adminUpdatingUser" ? Times.Once() : Times.Never());
        }

        [Theory]
        [InlineData("userUpdatingAnotherUser")]
        [InlineData("userUpdatingAdminRequiredInfo")]
        public async Task TestUpdateUserNotAllowed(string useCase)
        {
            // Arrange
            var callingUser = user1;
            var userToUpdate = useCase == "userUpdatingAnotherUser" ? userPending : user1;

            var request = new UpdateUserRequest()
            {
                FirstName = "newName",
                Role = useCase == "userUpdatingAdminRequiredInfo" ? RoleId.Admin : null,
            };

            // Act
            var result = await GetUsersController(callingUser, AuthScheme.AzureADB2C).Update(userToUpdate.Email, request);

            // Assert
            result.Result.Should().BeEquivalentTo(ExpectedErrorResult(403, ErrorCode.UserNotAllowed));
        }

        [Theory]
        [InlineData("userDoesNotExist", 404, ErrorCode.UserNotFound)]
        [InlineData("invalidTargetRole", 400, ErrorCode.InvalidUserRoleId)]
        [InlineData("invalidEmail", 400, ErrorCode.InvalidEmail)]
        [InlineData("invalidTargetUserStatus", 400, ErrorCode.InvalidUserStatusId)]
        [InlineData("unableToSendB2BInvitation", 400, ErrorCode.UnableToSendB2BInvitation)]
        public async Task TestUpdateUserInvalidRequest(string useCase, int statusCode, ErrorCode errorCode)
        {
            // Arrange
            var callingUser = admin1;
            var email = useCase == "userDoesNotExist" ? "unknown@email.com" : user1.Email;

            Mock<IMSGraphService> graphMock = null;

            var request = new UpdateUserRequest()
            {
                Email = useCase == "invalidEmail" ? string.Empty : null,
                Role = useCase == "invalidTargetRole" ? "invalid" : null,
                Status = useCase == "invalidTargetUserStatus" ? "invalid" : null,
            };

            if(useCase == "unableToSendB2BInvitation")
            {
                request.Status = UserStatusId.Approved;

                graphMock = new Mock<IMSGraphService>();
                graphMock.Setup(a => a.InviteUser(It.IsAny<string>())).Returns(Task.FromResult(MSGraphService.INVITEERROR));
            }

            // Act
            var result = await GetUsersController(callingUser, AuthScheme.AzureADB2BGuest, graphMock).Update(email, request);

            // Assert
            result.Result.Should().BeEquivalentTo(ExpectedErrorResult(statusCode, errorCode));
        }

        [Fact]
        public async Task TestDeleteUser()
        {
            // Arrange
            var callingUser = admin1;
            var emailToDelete = user1.Email;

            // Act
            var result = await GetUsersController(callingUser, AuthScheme.AzureADB2BGuest).Delete(emailToDelete);

            // Assert
            result.Should().BeOfType<OkResult>();

            db.User.Any(u => u.Email == emailToDelete).Should().BeFalse();
            db.RequestDetail
                .Include(rd => rd.Request).ThenInclude(r => r.User)
                .Any(rd => rd.Request.User.Email == emailToDelete).Should().BeFalse();
            db.Request
                .Include(r => r.User)
                .Any(r => r.User.Email == emailToDelete).Should().BeFalse();
        }

        [Fact]
        public async Task TestDeleteUserNotFound()
        {
            // Arrange
            var callingUser = admin1;
            var emailToDelete = "unknown@email.com";

            // Act
            var result = await GetUsersController(callingUser, AuthScheme.AzureADB2BGuest).Delete(emailToDelete);

            // Arrange
            result.Should().BeEquivalentTo(ExpectedErrorResult(404, ErrorCode.UserNotFound));
        }
    }
}
