using System;
using WebAPI.Constants;

namespace WebAPI.IntegrationTests.Common
{
    public static class SeedingData
    {
        public static (Guid Id, string Email, string FirstName, string LastName, string RoleId, string UserStatusId) Admin
            = (new Guid("bd03a30c-27da-4ff0-a820-d5db8e514ff6"), "admin@email.com", "Admin", "Admin", RoleId.Admin, UserStatusId.Approved);

        public static (Guid Id, string DisplayName, decimal Price, string PriceCurrency) Product4
            = (new Guid("a6a69012-abf8-4d48-8f47-26f134e9928a"), "Product4", 15.99M, "Euro");

        public static (Guid Id, string DisplayName, decimal Price, string PriceCurrency) Product5
            = (new Guid("5afc8666-45b9-44ef-97d8-1a810dc99860"), "Product5", 200M, "Euro");
    }
}
