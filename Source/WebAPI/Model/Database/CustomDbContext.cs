using System;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebAPI.Constants;

namespace WebAPI.Model.Database
{
    public class CustomDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDbContext"/> class.
        /// </summary>
        public CustomDbContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDbContext"/> class.
        /// </summary>
        /// <param name="options">db context options.</param>
        /// <param name="managedIdentityOptions">managed identity options.</param>
        public CustomDbContext(DbContextOptions<CustomDbContext> options, IOptions<ManagedIdentityOptions> managedIdentityOptions)
            : base(options)
        {
            if (Database.IsSqlServer())
            {
                var conn = (SqlConnection)Database.GetDbConnection();
                conn.AccessToken = new AzureServiceTokenProvider(managedIdentityOptions.Value.ConnectionOption).GetAccessTokenAsync("https://database.windows.net/", managedIdentityOptions.Value.TenantId).Result;
            }
        }

        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<Role> Role { get; set; }
        public virtual DbSet<Request> Request { get; set; }
        public virtual DbSet<RequestDetail> RequestDetail { get; set; }
        public virtual DbSet<RequestStatus> RequestStatus { get; set; }
        public virtual DbSet<Product> Product { get; set; }
        public virtual DbSet<UserStatus> UserStatus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email)
                    .HasName("user_unique_email")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Email)
                    .HasMaxLength(255);

                entity.Property(e => e.FirstName)
                    .HasMaxLength(255);

                entity.Property(e => e.LastName)
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(30, 5)");

                entity.Property(e => e.DisplayName)
                    .HasMaxLength(1000);

                entity.Property(e => e.PriceCurrency)
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Request>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasDefaultValueSql("(newid())");
            });

            modelBuilder.Entity<RequestDetail>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasDefaultValueSql("(newid())");
            });

            modelBuilder.Entity<Product>().HasData(
                new Product() { Id = new Guid("e6f6ddb0-02dd-4106-8716-e6ffa329c664"), DisplayName = "Product1", Price = 5.99M, PriceCurrency = "Euro" },
                new Product() { Id = new Guid("ce901d35-85d4-45a2-8e14-49bc360f70eb"), DisplayName = "Product2", Price = 15, PriceCurrency = "Euro" },
                new Product() { Id = new Guid("ad45055b-f1b3-46aa-a4c2-8ba5a4d27236"), DisplayName = "Product3", Price = 100, PriceCurrency = "Euro" });

            modelBuilder.Entity<Role>().HasData(
                new Role() { Id = RoleId.User },
                new Role() { Id = RoleId.Coordinator },
                new Role() { Id = RoleId.Admin });

            modelBuilder.Entity<UserStatus>().HasData(
                new Role() { Id = UserStatusId.Pending },
                new Role() { Id = UserStatusId.Approved },
                new Role() { Id = UserStatusId.Rejected });

            modelBuilder.Entity<RequestStatus>().HasData(
                new Role() { Id = RequestStatusId.Pending },
                new Role() { Id = RequestStatusId.Approved },
                new Role() { Id = RequestStatusId.Rejected });
        }
    }
}
