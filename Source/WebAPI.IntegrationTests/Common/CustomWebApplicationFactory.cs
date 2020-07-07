using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebAPI.Model.Database;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace WebAPI.IntegrationTests.Common
{
    public class CustomWebApplicationFactory<TStartup>
         : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // By default, the tests will get settings from appsettings.json/appsettings.Development.json. 
            // We can override any settings here.
            builder.ConfigureAppConfiguration((context, conf) =>
            {
                var authSettings = new Dictionary<string, string>
                {
                   {"AuthScheme", "AzureADB2C"},
                };

                conf.AddInMemoryCollection(authSettings);
            });

            builder.ConfigureServices(services =>
            {
                // Fake JWT auth.
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "FakeBearer";
                    options.DefaultChallengeScheme = "FakeBearer";
                })
                .AddJwtBearer("FakeBearer", options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = MockJwtTokens.SecurityKey,
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

                // Remove the app's ApplicationDbContext registration.
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<CustomDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add ApplicationDbContext using an in-memory database for testing.
                services.AddDbContext<CustomDbContext, TestDbContext>();
                services.AddScoped<CustomDbContext, TestDbContext>();

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context.
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<CustomDbContext>();
                var logger = scopedServices
                    .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                // Ensure the database is created.
                db.Database.EnsureCreated();

                // Seed the database with test data.
                ((TestDbContext)db).Initialize();
            });
        }
    }
}
