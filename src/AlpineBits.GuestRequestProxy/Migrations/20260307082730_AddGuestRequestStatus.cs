using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlpineBits.GuestRequestProxy.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestRequestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HotelTenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelTenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuestRequestLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestXml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestRequestLogs_HotelTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "HotelTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "HotelTenants",
                columns: new[] { "Id", "ApiKey", "CreatedAtUtc", "HotelCode", "IsActive", "Name", "Password", "TargetUrl", "Username" },
                values: new object[] { 1, "replace-me", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "HOTEL001", true, "Seed Hotel", "", "https://asa.example.com/alpinebits", "" });

            migrationBuilder.CreateIndex(
                name: "IX_GuestRequestLogs_CreatedAtUtc",
                table: "GuestRequestLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_GuestRequestLogs_Status",
                table: "GuestRequestLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GuestRequestLogs_TenantId",
                table: "GuestRequestLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HotelTenants_ApiKey",
                table: "HotelTenants",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelTenants_HotelCode",
                table: "HotelTenants",
                column: "HotelCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestRequestLogs");

            migrationBuilder.DropTable(
                name: "HotelTenants");
        }
    }
}
