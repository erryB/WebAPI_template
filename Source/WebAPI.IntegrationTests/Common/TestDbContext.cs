using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq;
using WebAPI.Model.Database;

namespace WebAPI.IntegrationTests.Common
{
    public class TestDbContext : CustomDbContext
    {
        public TestDbContext() : base() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Use an in memory database.
            optionsBuilder
                .UseInMemoryDatabase("InMemoryDb")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }

        public void Initialize()
        {
            // Seed the database.
            this.Add(new User() 
            {
                Id = SeedingData.Admin.Id,
                Email = SeedingData.Admin.Email,
                FirstName = SeedingData.Admin.FirstName,
                LastName = SeedingData.Admin.LastName,
                Role = this.Role.Single(r => r.Id == SeedingData.Admin.RoleId),
                UserStatus = this.UserStatus.Single(us => us.Id == SeedingData.Admin.UserStatusId)
            });

            this.Add(new Product()
            {
                Id = SeedingData.Product4.Id,
                DisplayName = SeedingData.Product4.DisplayName,
                Price = SeedingData.Product4.Price,
                PriceCurrency = SeedingData.Product4.PriceCurrency,
            });

            this.Add(new Product()
            {
                Id = SeedingData.Product5.Id,
                DisplayName = SeedingData.Product5.DisplayName,
                Price = SeedingData.Product5.Price,
                PriceCurrency = SeedingData.Product5.PriceCurrency,
            });

            this.SaveChanges();           
        }
    }
}
