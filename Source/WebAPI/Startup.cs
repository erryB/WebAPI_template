using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebAPI.Formatters;
using WebAPI.Model;
using WebAPI.Model.Database;
using WebAPI.Services;

namespace WebAPI
{
    public class Startup
    {
        private readonly string allowOrigins = "_allowOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Setup Cross Origin Resource Sharing.
            services.AddCors(options =>
            {
                options.AddPolicy(
                    name: allowOrigins,
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:3000")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });

            // Use managed identity to access the Azure Sql Database.
            services.Configure<ManagedIdentityOptions>(Configuration.GetSection("ManagedIdentity"));

            // Add the context to Azure SQL Database.
            services.AddDbContext<CustomDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Add auth support with B2C or B2B.
            // If the passed in token doesn't have "scope" claim, using Microsoft.Identity.Web will throw an exception:
            // https://github.com/AzureAD/microsoft-identity-web/blob/master/src/Microsoft.Identity.Web/WebApiAuthenticationBuilderExtensions.cs#L137
            // but MSAL doesn't enforce that additional check.
            var authScheme = Configuration.GetValue<string>("AuthScheme");
            services.AddProtectedWebApi(Configuration, authScheme)
                .AddProtectedWebApiCallsProtectedWebApi(Configuration, authScheme)
                .AddInMemoryTokenCaches();

            // Get user role from database after validating JWT.
            services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

            services.AddControllers(config =>
            {
                // All endpoints require authentication by default.
                var protectAllPolicy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
                config.Filters.Add(new AuthorizeFilter(protectAllPolicy));
            })
            .AddNewtonsoftJson(options =>
            {
                // Change naming strategy for JSON output.
                options.SerializerSettings.ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(),
                };
            });

            // Adds support to different versions of the API.
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            services.AddVersionedApiExplorer(
               options =>
               {
                    // Add the versioned api explorer, which also adds IApiVersionDescriptionProvider service.
                    // Note: the specified format code will format the version as "'v'major[.minor][-status]".
                    options.GroupNameFormat = "'v'VVV";

                    // Note: this option is only necessary when versioning by url segment. The SubstitutionFormat
                    // can also be used to control the format of the API version in route templates.
                    options.SubstituteApiVersionInUrl = true;
               });

            // Adds support to CSV output.
            services.AddMvc(options =>
            {
                options.OutputFormatters.Add(new CsvOutputFormatter());
                options.FormatterMappings.SetMediaTypeMappingForFormat("csv", MediaTypeHeaderValue.Parse("text/csv"));
            });

            // Register the Swagger generator, defining 1 or more Swagger documents.
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGenNewtonsoftSupport();
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Please enter your JWT",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer", // The name of the previously defined security scheme.
                                Type = ReferenceType.SecurityScheme,
                            },
                        }, new List<string>()
                    },
                });
            });

            services.AddApplicationInsightsTelemetry();

            // Register Microsoft Graph service to invite users to B2B.
            services.AddTransient<IMSGraphService, MSGraphService>();

            // Register Recaptcha service to validate that the user is not a bot.
            services.AddTransient<IReCaptchaService, ReCaptchaService>();
            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            // Setup default error handler.
            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/error-local-development");
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseHttpsRedirection();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }
            });

            if (env.IsDevelopment())
            {
                app.UseCors(allowOrigins);
            }

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
