using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserInviteCodeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventConfigs_Events_EventId",
                schema: "app",
                table: "EventConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_EventUsers_EventId_UserId",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.AddColumn<int>(
                name: "InviteCodeId",
                schema: "app",
                table: "EventUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EventUsers_InviteCodeId",
                schema: "app",
                table: "EventUsers",
                column: "InviteCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventConfigs_Events_EventId",
                schema: "app",
                table: "EventConfigs",
                column: "EventId",
                principalSchema: "app",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventUsers_InviteCodes_InviteCodeId",
                schema: "app",
                table: "EventUsers",
                column: "InviteCodeId",
                principalSchema: "app",
                principalTable: "InviteCodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rsvps_EventUsers_EventId_UserId",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" },
                principalSchema: "app",
                principalTable: "EventUsers",
                principalColumns: new[] { "EventId", "UserId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventConfigs_Events_EventId",
                schema: "app",
                table: "EventConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_EventUsers_InviteCodes_InviteCodeId",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_EventUsers_EventId_UserId",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropIndex(
                name: "IX_EventUsers_InviteCodeId",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "InviteCodeId",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_EventConfigs_Events_EventId",
                schema: "app",
                table: "EventConfigs",
                column: "EventId",
                principalSchema: "app",
                principalTable: "Events",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rsvps_EventUsers_EventId_UserId",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" },
                principalSchema: "app",
                principalTable: "EventUsers",
                principalColumns: new[] { "EventId", "UserId" });
        }
    }
}
