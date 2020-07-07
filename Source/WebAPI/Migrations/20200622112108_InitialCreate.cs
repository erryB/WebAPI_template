using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebAPI.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "(newid())"),
                    DisplayName = table.Column<string>(maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(30, 5)", nullable: false),
                    PriceCurrency = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestStatus",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserStatus",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "(newid())"),
                    Email = table.Column<string>(maxLength: 255, nullable: true),
                    FirstName = table.Column<string>(maxLength: 255, nullable: true),
                    LastName = table.Column<string>(maxLength: 255, nullable: true),
                    RoleId = table.Column<string>(nullable: true),
                    UserStatusId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_User_UserStatus_UserStatusId",
                        column: x => x.UserStatusId,
                        principalTable: "UserStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Request",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "(newid())"),
                    RefNo = table.Column<Guid>(nullable: false),
                    IsCurrent = table.Column<int>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true),
                    RequestStatusId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Request", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Request_RequestStatus_RequestStatusId",
                        column: x => x.RequestStatusId,
                        principalTable: "RequestStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Request_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false, defaultValueSql: "(newid())"),
                    Qty = table.Column<long>(nullable: false),
                    ProductId = table.Column<Guid>(nullable: true),
                    RequestId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestDetail_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestDetail_Request_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Request",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Product",
                columns: new[] { "Id", "DisplayName", "Price", "PriceCurrency" },
                values: new object[,]
                {
                    { new Guid("e6f6ddb0-02dd-4106-8716-e6ffa329c664"), "Product1", 5.99m, "Euro" },
                    { new Guid("ce901d35-85d4-45a2-8e14-49bc360f70eb"), "Product2", 15m, "Euro" },
                    { new Guid("ad45055b-f1b3-46aa-a4c2-8ba5a4d27236"), "Product3", 100m, "Euro" }
                });

            migrationBuilder.InsertData(
                table: "RequestStatus",
                column: "Id",
                values: new object[]
                {
                    "Pending",
                    "Approved",
                    "Rejected"
                });

            migrationBuilder.InsertData(
                table: "Role",
                column: "Id",
                values: new object[]
                {
                    "User",
                    "Coordinator",
                    "Admin"
                });

            migrationBuilder.InsertData(
                table: "UserStatus",
                column: "Id",
                values: new object[]
                {
                    "Pending",
                    "Approved",
                    "Rejected"
                });

            migrationBuilder.CreateIndex(
                name: "IX_Request_RequestStatusId",
                table: "Request",
                column: "RequestStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Request_UserId",
                table: "Request",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetail_ProductId",
                table: "RequestDetail",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetail_RequestId",
                table: "RequestDetail",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "user_unique_email",
                table: "User",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                table: "User",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_User_UserStatusId",
                table: "User",
                column: "UserStatusId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestDetail");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Request");

            migrationBuilder.DropTable(
                name: "RequestStatus");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "UserStatus");
        }
    }
}
