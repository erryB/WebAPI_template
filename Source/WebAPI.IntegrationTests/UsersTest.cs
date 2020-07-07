using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebAPI.Constants;
using WebAPI.Controllers.Helpers;
using WebAPI.IntegrationTests.Common;
using WebAPI.Model.Requests;
using WebAPI.Model.Responses;
using Xunit;

using static WebAPI.IntegrationTests.Common.HttpHelpers;
using static WebAPI.IntegrationTests.Common.MockJwtTokens;

namespace WebAPI.IntegrationTests
{
    [Collection("Integration Tests")]
    public class UsersTest
    {
        private readonly CustomWebApplicationFactory<Startup> factory;

        public UsersTest(CustomWebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
        }

        private (string Email, string Token) GetIdentity(string email)
            => (email, GenerateJwtToken("emails", email));

        private UserResponse ExpectedUserResponse(string email, NewUserRequest request, string role, string status)
            => new UserResponse()
            {
                Email = email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = role,
                Status = status,
            };

        private UserResponse ExpectedSeedingAdminUserResponse()
            => new UserResponse()
            {
                Email = SeedingData.Admin.Email,
                FirstName = SeedingData.Admin.FirstName,
                LastName = SeedingData.Admin.LastName,
                Role = SeedingData.Admin.RoleId,
                Status = SeedingData.Admin.UserStatusId,
            };

        [Fact]
        public async Task TestGetAllUsersWithDifferentUsersB2C()
        {
            var admin = GetIdentity(SeedingData.Admin.Email);
            var user = GetIdentity("user1B2C@email.com");
            var coordinator = GetIdentity("coordinator1B2C@email.com");
            var newAdmin = GetIdentity("newAdminB2C@email.com");

            // Try with Anonymous User and fail
            Func<Task<UsersResponse>> actAnonymous = async () => await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users");
            await actAnonymous.Should().ThrowAsync<WebAPIException>().WithMessage("*401*");

            // Try with Unknown User and fail
            Func<Task<UsersResponse>> actUnknown = async () => await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users", newAdmin.Token);
            await actUnknown.Should().ThrowAsync<WebAPIException>().WithMessage("*403*");

            // Try with known and approved User and fail
            var newUserRequest = new NewUserRequest()
            {
                FirstName = "UserFirstName",
                LastName = "UserLastName",
            };

            var createUserResponse = await ExecutePostRequestAsync<UserResponse>(factory, "/api/users", newUserRequest, user.Token);
            var expectedUserResponse = ExpectedUserResponse(user.Email, newUserRequest, RoleId.User, UserStatusId.Pending);
            createUserResponse.Should().BeEquivalentTo(expectedUserResponse);

            var updateUserRequest = new UpdateUserRequest()
            {
                Status = UserStatusId.Approved,
            };

            var updateUserResponse = await ExecutePatchRequestAsync<UserResponse>(factory, $"/api/users/{createUserResponse.Email}", updateUserRequest, admin.Token);
            expectedUserResponse.Status = UserStatusId.Approved;
            updateUserResponse.Should().BeEquivalentTo(expectedUserResponse);

            Func<Task<UsersResponse>> actUser = async () => await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users", user.Token);
            await actUser.Should().ThrowAsync<WebAPIException>().WithMessage("*403*");

            // Try with known and approved Coordinator and fail
            var newCoordinatorRequest = new NewUserRequest()
            {
                FirstName = "CoordinatorFirstName",
                LastName = "CoordinatorLastName",
                Role = RoleId.Coordinator,
            };

            var createCoordinatorResponse = await ExecutePostRequestAsync<UserResponse>(factory, "/api/users", newCoordinatorRequest, coordinator.Token);
            var expectedCoordinatorResponse = ExpectedUserResponse(coordinator.Email, newCoordinatorRequest, RoleId.Coordinator, UserStatusId.Pending);
            createCoordinatorResponse.Should().BeEquivalentTo(expectedCoordinatorResponse);

            var updateCoordinatorRequest = new UpdateUserRequest()
            {
                Status = UserStatusId.Approved,
            };

            var updateCoordinatorResponse = await ExecutePatchRequestAsync<UserResponse>(factory, $"/api/users/{createCoordinatorResponse.Email}", updateCoordinatorRequest, admin.Token);
            expectedCoordinatorResponse.Status = UserStatusId.Approved;
            updateCoordinatorResponse.Should().BeEquivalentTo(expectedCoordinatorResponse);

            Func<Task<UsersResponse>> actCoordinator = async () => await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users", coordinator.Token);
            await actCoordinator.Should().ThrowAsync<WebAPIException>().WithMessage("*403*");

            // Try with known Admin with Pending UserStatus and fail
            var newAdminRequest = new NewUserRequest()
            {
                FirstName = "NewAdminFirstName",
                LastName = "NewAdminLastName",
                Role = RoleId.Admin,
            };

            var createNewAdminResponse = await ExecutePostRequestAsync<UserResponse>(factory, "/api/users", newAdminRequest, newAdmin.Token);
            var expectedNewAdminResponse = ExpectedUserResponse(newAdmin.Email, newAdminRequest, RoleId.Admin, UserStatusId.Pending);
            createNewAdminResponse.Should().BeEquivalentTo(expectedNewAdminResponse);

            Func<Task<UsersResponse>> actPending = async () => await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users", newAdmin.Token);
            await actPending.Should().ThrowAsync<WebAPIException>().WithMessage("*403*");

            // Try with a known Admin with Rejected UserStatus and fail
            var updateAdminRequest = new UpdateUserRequest()
            {
                Status = UserStatusId.Rejected,
            };

            var updateAdminResponse = await ExecutePatchRequestAsync<UserResponse>(factory, $"/api/users/{createNewAdminResponse.Email}", updateAdminRequest, admin.Token);
            expectedNewAdminResponse.Status = UserStatusId.Rejected;
            updateAdminResponse.Should().BeEquivalentTo(expectedNewAdminResponse);

            Func<Task<UsersResponse>> actRejected = async () => await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users", newAdmin.Token);
            await actRejected.Should().ThrowAsync<WebAPIException>().WithMessage("*403*");

            // Try with known and approved admin and succeed
            updateAdminRequest = new UpdateUserRequest()
            {
                Status = UserStatusId.Approved,
            };

            updateAdminResponse = await ExecutePatchRequestAsync<UserResponse>(factory, $"/api/users/{createNewAdminResponse.Email}", updateAdminRequest, admin.Token);
            expectedNewAdminResponse.Status = UserStatusId.Approved;
            updateAdminResponse.Should().BeEquivalentTo(expectedNewAdminResponse);

            var getUsersResponse = await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users", newAdmin.Token);
            var expectedUsersResponse = new UsersResponse()
            {
                Users = new[] {
                    ExpectedSeedingAdminUserResponse(),
                    expectedUserResponse,
                    expectedCoordinatorResponse,
                    expectedNewAdminResponse,
                }
            };
            getUsersResponse.Should().BeEquivalentTo(expectedUsersResponse);

            // Delete all the users we created
            await ExecuteDeleteRequestAsync<CustomProblemDetails>(factory, $"/api/users/{createUserResponse.Email}", admin.Token);
            await ExecuteDeleteRequestAsync<CustomProblemDetails>(factory, $"/api/users/{createCoordinatorResponse.Email}", admin.Token);
            await ExecuteDeleteRequestAsync<CustomProblemDetails>(factory, $"/api/users/{createNewAdminResponse.Email}", admin.Token);

            // Verify that the only user in the database is the initial Admin
            getUsersResponse = await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users", admin.Token);
            expectedUsersResponse = new UsersResponse()
            {
                Users = new[] {
                    ExpectedSeedingAdminUserResponse(),
                }
            };
            getUsersResponse.Should().BeEquivalentTo(expectedUsersResponse);
        }

        [Fact]
        public async Task TestCreateUserAndRequestAndDeleteUserB2C()
        {
            var admin = GetIdentity(SeedingData.Admin.Email);
            var user = GetIdentity("user2B2C@email.com");
            var coordinator = GetIdentity("coordinator2B2C@email.com");

            // Create new User
            var newUserRequest = new NewUserRequest()
            {
                FirstName = "UserFirstName",
                LastName = "UserLastName",
            };

            var createUserResponse = await ExecutePostRequestAsync<UserResponse>(factory, "/api/users", newUserRequest, user.Token);
            var expectedUserResponse = ExpectedUserResponse(user.Email, newUserRequest, RoleId.User, UserStatusId.Pending);
            createUserResponse.Should().BeEquivalentTo(expectedUserResponse);

            // Create new Coordinator
            var newCoordinatorRequest = new NewUserRequest()
            {
                FirstName = "CoordinatorFirstName",
                LastName = "CoordinatorLastName",
                Role = RoleId.Coordinator,
            };

            var createCoordinatorResponse = await ExecutePostRequestAsync<UserResponse>(factory, "/api/users", newCoordinatorRequest, coordinator.Token);
            var expectedCoordinatorResponse = ExpectedUserResponse(coordinator.Email, newCoordinatorRequest, RoleId.Coordinator, UserStatusId.Pending);
            createCoordinatorResponse.Should().BeEquivalentTo(expectedCoordinatorResponse);

            // Approve the new User with an Admin
            var updateUserRequest = new UpdateUserRequest()
            {
                Status = UserStatusId.Approved,
            };

            var updateUserResponse = await ExecutePatchRequestAsync<UserResponse>(factory, $"/api/users/{createUserResponse.Email}", updateUserRequest, admin.Token);
            expectedUserResponse.Status = UserStatusId.Approved;
            updateUserResponse.Should().BeEquivalentTo(expectedUserResponse);

            // Approve the new Coordinator with an Admin
            var updateCoordinatorRequest = new UpdateUserRequest()
            {
                Status = UserStatusId.Approved,
            };

            var updateCoordinatorResponse = await ExecutePatchRequestAsync<UserResponse>(factory, $"/api/users/{createCoordinatorResponse.Email}", updateCoordinatorRequest, admin.Token);
            expectedCoordinatorResponse.Status = UserStatusId.Approved;
            updateCoordinatorResponse.Should().BeEquivalentTo(expectedCoordinatorResponse);

            // Create a new request with the User
            var product = SeedingData.Product4;
            var newRequestRequest = new NewRequestRequest()
            {
                SelectedProducts = new[] {
                    new ProductRequestRequest()
                    {
                        Id = product.Id,
                        Quantity = 10,
                    }
                }
            };

            var newRequestResponse = await ExecutePostRequestAsync<NewRequestResponse>(factory, "/api/requests", newRequestRequest, user.Token);

            // Get all the requests created by the User. Just one in this case.
            var requestsResponse = await ExecuteGetRequestAsync<RequestsResponse>(factory, "/api/requests", user.Token);
            var expectedRequestsResponse = new RequestsResponse()
            {
                Requests = new[]
                {
                    new RequestResponse()
                    {
                        RefNo = newRequestResponse.RefNo,
                        RequestStatus = RequestStatusId.Pending,
                        UserEmail = createUserResponse.Email,
                        RequestDetails = new[]
                        {
                            new RequestDetailResponse()
                            {
                                Qty = newRequestRequest.SelectedProducts.First().Quantity,
                                ProductDisplayName = product.DisplayName,
                                ProductPrice = product.Price,
                                ProductPriceCurrency = product.PriceCurrency,
                            },
                        },
                    },
                },
            };
            requestsResponse.Should().BeEquivalentTo(expectedRequestsResponse);

            // Delete the User and all her requests 
            await ExecuteDeleteRequestAsync<CustomProblemDetails>(factory, $"/api/users/{createUserResponse.Email}", admin.Token);

            // Verify that the User is not in the system
            Func<Task<CustomProblemDetails>> act = async () => await ExecuteGetRequestAsync<CustomProblemDetails>(factory, $"/api/users/{createUserResponse.Email}", admin.Token);
            var expectedErrorResponse = new CustomProblemDetails()
            {
                Title = "Unable to get user",
                Status = 404,
                DetailCode = ErrorCode.UserNotFound,
                Detail = ErrorCode.UserNotFound.GetStringValue(),
                InnerDetail = $"Unknown user: {createUserResponse.Email}",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            };
            act.Should().Throw<WebAPIException>().WithMessage("*404*")
                .And.CustomProblemDetails.Should().BeEquivalentTo(expectedErrorResponse, options 
                    => options.Excluding(cpd => cpd.TraceId));

            // Verify that there are no Requests for the deleted User
            requestsResponse = await ExecuteGetRequestAsync<RequestsResponse>(factory, "/api/requests", coordinator.Token);
            requestsResponse.Requests.Where(r => r.UserEmail == createUserResponse.Email).Should().BeEmpty();

            // Delete all the users we created
            await ExecuteDeleteRequestAsync<CustomProblemDetails>(factory, $"/api/users/{createCoordinatorResponse.Email}", admin.Token);

            // Verify that the only user in the database is the initial Admin
            var getUsersResponse = await ExecuteGetRequestAsync<UsersResponse>(factory, "/api/users", admin.Token);
            var expectedUsersResponse = new UsersResponse()
            {
                Users = new[] {
                    ExpectedSeedingAdminUserResponse(),
                }
            };
            getUsersResponse.Should().BeEquivalentTo(expectedUsersResponse);
        }
    }
}
