using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TableRenaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_UserEvents_EventId_UserId",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropTable(
                name: "UserEvents",
                schema: "app");

            migrationBuilder.CreateTable(
                name: "EventUsers",
                schema: "app",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    AssignedAccommodationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InviteEmailSent = table.Column<bool>(type: "bit", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventUsers", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EventUsers_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventUsers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventUsers_UserId",
                schema: "app",
                table: "EventUsers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rsvps_EventUsers_EventId_UserId",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" },
                principalSchema: "app",
                principalTable: "EventUsers",
                principalColumns: new[] { "EventId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_EventUsers_EventId_UserId",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropTable(
                name: "EventUsers",
                schema: "app");

            migrationBuilder.CreateTable(
                name: "UserEvents",
                schema: "app",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedAccommodationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InviteEmailSent = table.Column<bool>(type: "bit", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEvents", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserEvents_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEvents_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEvents_UserId",
                schema: "app",
                table: "UserEvents",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rsvps_UserEvents_EventId_UserId",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" },
                principalSchema: "app",
                principalTable: "UserEvents",
                principalColumns: new[] { "EventId", "UserId" });
        }
    }
}
