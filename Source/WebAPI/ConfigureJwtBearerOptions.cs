using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebAPI.Constants;
using WebAPI.Helpers;
using WebAPI.Model.Database;

namespace WebAPI
{
    /// <summary>
    /// Configures the Jwt options.
    /// </summary>
    public class ConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureJwtBearerOptions"/> class.
        /// </summary>
        /// <param name="serviceScopeFactory">Service scope factory.</param>
        public ConfigureJwtBearerOptions(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        /// <inheritdoc />
        public void PostConfigure(string name, JwtBearerOptions options)
        {
            options.Events ??= new JwtBearerEvents();

            options.Events.OnTokenValidated = async context =>
            {
                // Get the user identity and her email from the Claims.
                var identity = context.Principal.Identities.First();
                var email = identity.GetEmail();

                // Access the database.
                using var scope = serviceScopeFactory.CreateScope();
                var provider = scope.ServiceProvider;
                using var databaseContext = provider.GetRequiredService<CustomDbContext>();

                // Get the role of the user only if her status is Approved.
                var user = await databaseContext
                    .User
                    .Where(u => u.Email == email && u.UserStatus.Id == UserStatusId.Approved)
                    .Include(u => u.UserStatus)
                    .Include(u => u.Role)
                    .SingleOrDefaultAsync();

                if (user != null)
                {
                    // Add the role claim to the user identity.
                    var extraClaim = new Claim(ClaimTypes.Role, user.Role.Id);
                    identity.AddClaim(extraClaim);
                }

                context.Success();
            };
        }
    }
}
