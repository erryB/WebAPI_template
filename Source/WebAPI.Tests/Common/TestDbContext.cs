using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Linq;
using WebAPI.Model;
using WebAPI.Model.Database;

namespace WebAPI.Tests.Common
{
    public class TestDbContext : CustomDbContext
    {
        public static DbContextOptions<CustomDbContext> TestDbContextOptions()
        {
            // Create a new service provider to create a new in-memory database.
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Create a new options instance using an in-memory database and 
            // IServiceProvider that the context should resolve all of its 
            // services from.
            var builder = new DbContextOptionsBuilder<CustomDbContext>()
                .UseInMemoryDatabase("InMemoryDb")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .UseInternalServiceProvider(serviceProvider);

            return builder.Options;
        }

        public static IOptions<ManagedIdentityOptions> TestManagedIdentityOptions()
        {
            // Fake Manage Identity options.
            var options = Options.Create(new ManagedIdentityOptions()
            {
                ConnectionOption = "TestConnectionOption",
                TenantId = "TestTenantId"
            });

            return options;
        }

        public TestDbContext() : base(TestDbContextOptions(), TestManagedIdentityOptions()) { }

        public override int SaveChanges()
        {
            // Force the code under test to go to the database instead of pulling the existing object graph from ChangeTracker.
            // This is required to test if our controllers are using EF's Eager Loading (https://docs.microsoft.com/en-us/ef/core/querying/related-data#eager-loading) properly.
            // More info: How to disable eager loading when using InMemoryDatabase https://stackoverflow.com/questions/52740665/how-to-disable-eager-loading-when-using-inmemorydatabase.
            var affectedRows = base.SaveChanges();

            ChangeTracker.Entries()
                .Where(e => e.Entity != null)
                .ToList()
                .ForEach(e => e.State = EntityState.Detached);

            return affectedRows;
        }
    }
}
