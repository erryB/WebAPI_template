using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAPI.Constants;
using WebAPI.Controllers;
using WebAPI.Model.Database;
using WebAPI.Model.Requests;
using WebAPI.Model.Responses;
using WebAPI.Tests.Common;
using Xunit;
using static WebAPI.Tests.Common.DbTableFactory;
using static WebAPI.Tests.Common.ErrorHelpers;

namespace WebAPI.Tests
{
    public class RequestsTest
    {
        private readonly TestDbContext db;
        private readonly User user1, user2, coordinator1;
        private readonly Request[] requests;
        private readonly Product[] products;

        public RequestsTest()
        {
            db = new TestDbContext();
            int i;

            Role userRole, coordinatorRole, adminRole;
            db.AddRange(new[]
            {
                userRole = RoleUser(),
                coordinatorRole = RoleCoordinator(),
                adminRole = RoleAdmin(),
            });

            UserStatus userStatusApproved;
            db.AddRange(new[]
            {
                userStatusApproved = UserStatusApproved(),
                UserStatusPending(),
                UserStatusRejected() 
            });

            db.AddRange(new[]
            {
                user1 = User(i=1, userRole, userStatusApproved),
                user2 = User(++i, userRole, userStatusApproved),
                User(++i, adminRole, userStatusApproved),
                coordinator1 = User(++i, coordinatorRole, userStatusApproved),
            });

            db.AddRange(products = new[]
            {
                Product(i=1),
                Product(++i),
                Product(++i),
            });

            RequestStatus requestStatusPending, requestStatusApproved;
            db.AddRange(new[]
            {
                requestStatusApproved = RequestStatusApproved(),
                requestStatusPending = RequestStatusPending(),
                RequestStatusRejected()
            });

            var refNo = Guid.NewGuid();
            db.AddRange(requests = new[]
            {
                Request(user1, Guid.NewGuid(), requestStatusPending, 1),
                Request(user2, refNo, requestStatusPending, 0),
                Request(user2, refNo, requestStatusApproved, 1),
            });

            db.AddRange(new[]
            {
                RequestDetail(i = 1, requests[0], products[0]),
                RequestDetail(++i, requests[0], products[1]),
                RequestDetail(++i, requests[1], products[2]),
                RequestDetail(++i, requests[2], products[0]),
            });

            db.SaveChanges();
        }

        private RequestsController GetRequestsController(User user)
        {
            var logger = new Mock<ILogger<RequestsController>>();

            ClaimsPrincipal userClaims = new ClaimsPrincipal();           
            userClaims.AddIdentity(new ClaimsIdentity(new[]
            {
                new Claim("emails", user.Email), // B2C
                new Claim(ClaimTypes.Role, user.Role.Id)
            }));

            var controller = new RequestsController(db, logger.Object)
            {
                ProblemDetailsFactory = MockHelpers.MockProblemDetailsFactory(),
            };

            controller.ControllerContext.HttpContext = new DefaultHttpContext()
            {
                User = userClaims,
            };

            return controller;
        }

        private Request ExpectedRequestRow(string refNo, int isCurrent = 1, string requestStatus = RequestStatusId.Pending)
            => new Request()
            {
                RefNo = new Guid(refNo),
                IsCurrent = isCurrent,
                RequestStatus = new RequestStatus() { Id = requestStatus },
            };

        private RequestDetail ExpectedRequestDetailRow(ProductRequestRequest request)
            => new RequestDetail()
            {
                Product = products.Single(p => p.Id == request.Id),
                Qty = request.Quantity,
            };

        [Fact]
        public async Task TestCreateRequest()
        {
            // Arrange
            var user = user1;
            var request1 = new NewRequestRequest()
            {
                SelectedProducts = new[]
                {
                    new ProductRequestRequest() {Id = products[0].Id, Quantity = 9},
                    new ProductRequestRequest() {Id = products[1].Id, Quantity = 2},
                }
            };
            
            // Act
            var result1 = await GetRequestsController(user).Create(request1);

            // Assert
            var response1 = result1.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<NewRequestResponse>().Which;

            var expectedRequestRow = ExpectedRequestRow(response1.RefNo.ToString());
            var requestRow = db.Request.Single(r => r.User.Email == user.Email && r.RefNo == response1.RefNo);
            requestRow.Should().BeEquivalentTo(expectedRequestRow, options => options.Excluding(r => r.Id).Excluding(r => r.User).Excluding(r => r.RequestDetails).Excluding(r => r.RequestStatus.Requests));

            var expectedRequestDetailRows = request1.SelectedProducts.Select(ExpectedRequestDetailRow).ToList();
            var requestDetailRows = db.RequestDetail.Where(rd => rd.Request.RefNo == response1.RefNo).ToList();
            requestDetailRows.Should().BeEquivalentTo(expectedRequestDetailRows, options => options.Excluding(rd => rd.Id).Excluding(rd => rd.Product.RequestDetails).Excluding(rd => rd.Request));
        
            // User creates the second request

            // Arrange
            var request2 = new NewRequestRequest()
            {
                RefNo = response1.RefNo,
                SelectedProducts = new[]
                {
                    new ProductRequestRequest() {Id = products[2].Id, Quantity = 15},
                }
            };

            // Act
            var result2 = await GetRequestsController(user).Create(request2);

            // Assert
            var expectedResult2 = new OkObjectResult(new NewRequestResponse() { RefNo = response1.RefNo });
            result2.Result.Should().BeEquivalentTo(expectedResult2);

            var expectedRequestRows = new[]
            {
                ExpectedRequestRow(response1.RefNo.ToString(), isCurrent: 0),
                ExpectedRequestRow(response1.RefNo.ToString(), isCurrent: 1),
            };
            var requestRows = db.Request.Where(r => r.User.Email == user.Email && r.RefNo == response1.RefNo);
            requestRows.Should().BeEquivalentTo(expectedRequestRows, options => options.Excluding(r => r.Id).Excluding(r => r.User).Excluding(r => r.RequestDetails).Excluding(r => r.RequestStatus.Requests));

            expectedRequestDetailRows = request2.SelectedProducts.Select(ExpectedRequestDetailRow).ToList();
            requestDetailRows = db.RequestDetail.Where(rd => rd.Request.RefNo == response1.RefNo && rd.Request.IsCurrent == 1).ToList();
            requestDetailRows.Should().BeEquivalentTo(expectedRequestDetailRows, options => options.Excluding(rd => rd.Id).Excluding(rd => rd.Product.RequestDetails).Excluding(rd => rd.Request));
        }

        // missing parameters, negatuive value, 2nd request: wrong ref no and different user

        [Theory]
        [InlineData("missingParameters", ErrorCode.MissingMandatoryField)]
        [InlineData("negativeValue", ErrorCode.NegativeValue)]
        [InlineData("invalidProductId", ErrorCode.InvalidProductId)]
        public async Task TestCreateRequestInvalidRequest(string useCase, ErrorCode errorCode)
        {
            // Arrange
            var user = user1;
            var request = new NewRequestRequest();
            
            if (useCase != "missingParameters")
            {
                request.SelectedProducts = new[]
                {
                    new ProductRequestRequest() 
                    {
                        Id = useCase == "invalidProductId" ? Guid.NewGuid() : products[2].Id, 
                        Quantity = useCase == "negativeValue" ? -12 : 15,
                    },
                };
            }

            // Act
            var result = await GetRequestsController(user).Create(request);

            // Assert
            result.Result.Should().BeEquivalentTo(ExpectedErrorResult(400, errorCode));
        }

        [Theory]
        [InlineData("invalidRefNo", 400, ErrorCode.InvalidRefNo)]
        [InlineData("differentUser", 403, ErrorCode.UserNotAllowed)]
        public async Task TestCreateRequestSecondInvalidRequest(string useCase, int statusCode, ErrorCode errorCode)
        {
            // Arrange
            var user = user1;
            var request1 = new NewRequestRequest()
            {
                SelectedProducts = new[]
                {
                    new ProductRequestRequest() {Id = products[0].Id, Quantity = 9},
                    new ProductRequestRequest() {Id = products[1].Id, Quantity = 2},
                }
            };

            // Act
            var result1 = await GetRequestsController(user).Create(request1);

            // Assert
            var response1 = result1.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<NewRequestResponse>().Which;

            // User creates the second request

            // Arrange
            if (useCase == "differentUser")
            {
                user = user2;
            }

            var request2 = new NewRequestRequest()
            {
                RefNo = useCase == "invalidRefNo" ? Guid.NewGuid() : response1.RefNo,
                SelectedProducts = new[]
                {
                    new ProductRequestRequest() {Id = products[2].Id, Quantity = 15},
                }
            };

            // Act
            var result2 = await GetRequestsController(user).Create(request2);

            // Assert
            result2.Result.Should().BeEquivalentTo(ExpectedErrorResult(statusCode, errorCode));
        }

        [Fact]
        public async Task TestUpdateRequest()
        {
            // Arrange
            var user = coordinator1;
            var refNo = requests[0].RefNo;

            var request = new UpdateRequestRequest()
            {
                Status = RequestStatusId.Approved,
            };

            var requestRow = db.Request.Include(r => r.RequestStatus).Single(r => r.RefNo == refNo && r.IsCurrent == 1);
            requestRow.RequestStatus.Id.Should().Be(RequestStatusId.Pending);

            // Act
            var result = await GetRequestsController(user).Update(refNo.ToString(), request);

            // Assert
            var expectedResult = new OkObjectResult(new UpdateRequestResponse()
            {
                RefNo = refNo,
                Status = request.Status,
            });

            result.Result.Should().BeEquivalentTo(expectedResult);

            requestRow = db.Request.Single(r => r.RefNo == refNo && r.IsCurrent == 1);
            requestRow.RequestStatus.Id.Should().Be(request.Status);
        }

        [Theory]
        [InlineData("missingStatus", ErrorCode.MissingMandatoryField)]
        [InlineData("invalidStatus", ErrorCode.InvalidRequestStatusId)]
        [InlineData("invalidRefNo", ErrorCode.InvalidRefNo)]
        public async Task TestUpdateRequestInvalidRequest(string useCase, ErrorCode errorCode)
        {
            // Arrange
            var user = coordinator1;
            var refNo = useCase == "invalidRefNo" ? Guid.NewGuid() : requests[0].RefNo;

            var request = new UpdateRequestRequest()
            {
                Status = useCase switch
                {
                    "missingStatus" => null,
                    "invalidStatus" => "invalid",
                    _ => RequestStatusId.Approved,
                },
            };

            // Act
            var result = await GetRequestsController(user).Update(refNo.ToString(), request);

            // Assert
            result.Result.Should().BeEquivalentTo(ExpectedErrorResult(400, errorCode));
        }

        [Theory]
        [InlineData("user")]
        [InlineData("coordinator")]
        public async Task TestGetAllRequests(string useCase)
        {
            // Arrange
            var user = useCase == "user" ? user2 : coordinator1;

            // Act
            var result = await GetRequestsController(user).GetAll();

            // Assert
            var expectedResult = new OkObjectResult(new RequestsResponse()
            {
                Requests = requests.Where(r => ((useCase == "user" && r.User.Email == user.Email)
                    || useCase == "coordinator")
                    && r.IsCurrent == 1)
                    .Select(r => new RequestResponse()
                    {
                        RefNo = r.RefNo,
                        UserEmail = r.User.Email,
                        RequestStatus = r.RequestStatus.Id,
                        RequestDetails = r.RequestDetails.Select(rd => new RequestDetailResponse()
                        {
                            ProductDisplayName = rd.Product.DisplayName,
                            ProductPrice = rd.Product.Price,
                            ProductPriceCurrency = rd.Product.PriceCurrency,
                            Qty = rd.Qty,
                        }).ToList(),
                    }),
            });

            result.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Theory]
        [InlineData("user")]
        [InlineData("coordinator")]
        public async Task TestGetAllCsvRequests(string useCase)
        {
            // Arrange
            var user = useCase == "user" ? user2 : coordinator1;

            // Act
            var result = await GetRequestsController(user).GetAllAsCsv();

            // Assert
            var expectedResult = new OkObjectResult(requests.Where(r => ((useCase == "user" && r.User.Email == user.Email)
                || useCase == "coordinator")
                && r.IsCurrent == 1)
                .SelectMany(r => r.RequestDetails.Select(rd => new RequestCSVResponse()
                {
                    RefNo = r.RefNo,
                    UserEmail = r.User.Email,
                    RequestStatus = r.RequestStatus.Id,
                    ProductDisplayName = rd.Product.DisplayName,
                    ProductPrice = rd.Product.Price,
                    ProductPriceCurrency = rd.Product.PriceCurrency,
                    Qty = rd.Qty,

                })).ToList());

            result.Result.Should().BeEquivalentTo(expectedResult);
        }

        [Theory]
        [InlineData("user")]
        [InlineData("coordinator")]
        public async Task TestDeleteRequest(string useCase)
        {
            // Arrange
            var user = useCase == "user" ? user2 : coordinator1;
            var refNo = requests[1].RefNo;

            // Act
            var result = await GetRequestsController(user).Delete(refNo.ToString());

            // Assert
            result.Should().BeOfType<OkResult>();

            db.RequestDetail.Include(rd => rd.Request).Any(rd => rd.Request.RefNo == refNo).Should().BeFalse();
            db.Request.Any(r => r.RefNo == refNo).Should().BeFalse();
        }

        [Theory]
        [InlineData("invalidRequest", 404, ErrorCode.InvalidRefNo)]
        [InlineData("userNotAllowed", 403, ErrorCode.UserNotAllowed)]
        public async Task TestDeleteInvalidRequest(string useCase, int statusCode, ErrorCode errorCode)
        {
            // Arrange
            var user = useCase == "userNotAllowed" ? user1 : user2;
            var refNo = useCase == "invalidRequest" ? "invalid" : requests[1].RefNo.ToString();

            // Act
            var result = await GetRequestsController(user).Delete(refNo);

            // Assert
            result.Should().BeEquivalentTo(ExpectedErrorResult(statusCode, errorCode));
        }

    }
}
