using System;
using WebAPI.Constants;
using WebAPI.Model.Database;

namespace WebAPI.Tests.Common
{
    public static class DbTableFactory
    {
        public static Role Role(string roleId)
            => new Role() { Id = roleId };

        public static Role RoleUser()
            => Role(RoleId.User);
        public static Role RoleCoordinator()
            => Role(RoleId.Coordinator);
        public static Role RoleAdmin()
            => Role(RoleId.Admin);

        public static User User(int i, Role role, UserStatus approvalStatus)
            => new User()
            {
                Id = Guid.NewGuid(),
                Email = $"email{i}@email.com",
                FirstName = $"FirstName{i}",
                LastName = $"LastName{i}",
                UserStatus = approvalStatus,
                Role = role,
            };

        public static UserStatus UserStatus(string userStatusId)
           => new UserStatus() { Id = userStatusId };

        public static UserStatus UserStatusApproved()
            => UserStatus(UserStatusId.Approved);
        public static UserStatus UserStatusPending()
            => UserStatus(UserStatusId.Pending);
        public static UserStatus UserStatusRejected()
            => UserStatus(UserStatusId.Rejected);

        public static Product Product(int i)
            => new Product()
            {
                Id = Guid.NewGuid(),
                DisplayName = $"Name{i}",
                Price = i,
                PriceCurrency = $"Currency{i}",
            };

        public static RequestStatus RequestStatus(string requestStatusId)
           => new RequestStatus() { Id = requestStatusId };

        public static RequestStatus RequestStatusApproved()
            => RequestStatus(RequestStatusId.Approved);
        public static RequestStatus RequestStatusPending()
            => RequestStatus(RequestStatusId.Pending);
        public static RequestStatus RequestStatusRejected()
            => RequestStatus(RequestStatusId.Rejected);

        public static Request Request(User user, Guid refNo, RequestStatus status, int isCurrent)
            => new Request()
            {
                Id = Guid.NewGuid(),
                RefNo = refNo,
                IsCurrent = isCurrent,
                RequestStatus = status,
                User = user,
            };

        public static RequestDetail RequestDetail(int i, Request request, Product product)
            => new RequestDetail()
            {
                Id = Guid.NewGuid(),
                Qty = i,
                Request = request,
                Product = product,
            };
    }
}
