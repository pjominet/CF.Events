using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.RenameTable(
                name: "LoginAudits",
                schema: "identity",
                newName: "LoginAudits",
                newSchema: "audit");

            migrationBuilder.CreateTable(
                name: "EmailActivities",
                schema: "audit",
                columns: table => new
                {
                    EmailId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LatestEvent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LatestEventAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false),
                    IsBounced = table.Column<bool>(type: "bit", nullable: false),
                    IsSpam = table.Column<bool>(type: "bit", nullable: false),
                    WasOpened = table.Column<bool>(type: "bit", nullable: false),
                    OpenCount = table.Column<int>(type: "int", nullable: false),
                    FirstOpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastOpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WasClicked = table.Column<bool>(type: "bit", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    FirstClickedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastClickedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HasError = table.Column<bool>(type: "bit", nullable: false),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastSmtpResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailActivities", x => x.EmailId);
                });

            migrationBuilder.CreateTable(
                name: "EmailActivityEvents",
                schema: "audit",
                columns: table => new
                {
                    EmailActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Event = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SmtpResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Host = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClickUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailActivityEvents", x => x.EmailActivityId);
                    table.ForeignKey(
                        name: "FK_EmailActivityEvents_EmailActivities_EmailId",
                        column: x => x.EmailId,
                        principalSchema: "audit",
                        principalTable: "EmailActivities",
                        principalColumn: "EmailId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailActivityEvents_EmailActivityId_Event_EventAt",
                schema: "audit",
                table: "EmailActivityEvents",
                columns: new[] { "EmailActivityId", "Event", "EventAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailActivityEvents_EmailId",
                schema: "audit",
                table: "EmailActivityEvents",
                column: "EmailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailActivityEvents",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "EmailActivities",
                schema: "audit");

            migrationBuilder.RenameTable(
                name: "LoginAudits",
                schema: "audit",
                newName: "LoginAudits",
                newSchema: "identity");
        }
    }
}
