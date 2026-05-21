using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HyperlocalAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_metrics",
                columns: table => new
                {
                    Vertical = table.Column<int>(type: "INTEGER", nullable: false),
                    DayUtc = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NewLeads = table.Column<int>(type: "INTEGER", nullable: false),
                    MessagesHandled = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_metrics", x => new { x.Vertical, x.DayUtc });
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Vertical = table.Column<int>(type: "INTEGER", nullable: false),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ContactKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PhoneE164 = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LeadScore = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    FirstSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivityUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WonAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LostAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LostReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Vertical = table.Column<int>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CancelledUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appointments_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_LeadId",
                table: "appointments",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_Vertical_StartUtc",
                table: "appointments",
                columns: new[] { "Vertical", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_leads_Vertical_ContactKey",
                table: "leads",
                columns: new[] { "Vertical", "ContactKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "daily_metrics");

            migrationBuilder.DropTable(
                name: "leads");
        }
    }
}
