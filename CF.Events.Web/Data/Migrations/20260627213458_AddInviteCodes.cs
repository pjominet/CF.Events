using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserEvent_Events_EventId",
                schema: "app",
                table: "UserEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_UserEvent_Users_UserId",
                schema: "app",
                table: "UserEvent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserEvent",
                schema: "app",
                table: "UserEvent");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                schema: "app",
                table: "Events");

            migrationBuilder.RenameTable(
                name: "UserEvent",
                schema: "app",
                newName: "UserEvents",
                newSchema: "app");

            migrationBuilder.RenameIndex(
                name: "IX_UserEvent_UserId",
                schema: "app",
                table: "UserEvents",
                newName: "IX_UserEvents_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserEvents",
                schema: "app",
                table: "UserEvents",
                columns: new[] { "EventId", "UserId" });

            migrationBuilder.CreateTable(
                name: "InviteCodes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InviteCodes_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_EventId",
                schema: "app",
                table: "InviteCodes",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserEvents_Events_EventId",
                schema: "app",
                table: "UserEvents",
                column: "EventId",
                principalSchema: "app",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserEvents_Users_UserId",
                schema: "app",
                table: "UserEvents",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserEvents_Events_EventId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_UserEvents_Users_UserId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropTable(
                name: "InviteCodes",
                schema: "app");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserEvents",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.RenameTable(
                name: "UserEvents",
                schema: "app",
                newName: "UserEvent",
                newSchema: "app");

            migrationBuilder.RenameIndex(
                name: "IX_UserEvents_UserId",
                schema: "app",
                table: "UserEvent",
                newName: "IX_UserEvent_UserId");

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                schema: "app",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserEvent",
                schema: "app",
                table: "UserEvent",
                columns: new[] { "EventId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserEvent_Events_EventId",
                schema: "app",
                table: "UserEvent",
                column: "EventId",
                principalSchema: "app",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserEvent_Users_UserId",
                schema: "app",
                table: "UserEvent",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
