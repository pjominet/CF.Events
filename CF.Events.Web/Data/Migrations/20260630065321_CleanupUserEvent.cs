using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanupUserEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserEvents_Rsvps_RsvpEventId_RsvpUserId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropIndex(
                name: "IX_UserEvents_RsvpEventId_RsvpUserId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropColumn(
                name: "RsvpEventId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropColumn(
                name: "RsvpUserId",
                schema: "app",
                table: "UserEvents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RsvpEventId",
                schema: "app",
                table: "UserEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RsvpUserId",
                schema: "app",
                table: "UserEvents",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEvents_RsvpEventId_RsvpUserId",
                schema: "app",
                table: "UserEvents",
                columns: new[] { "RsvpEventId", "RsvpUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserEvents_Rsvps_RsvpEventId_RsvpUserId",
                schema: "app",
                table: "UserEvents",
                columns: new[] { "RsvpEventId", "RsvpUserId" },
                principalSchema: "app",
                principalTable: "Rsvps",
                principalColumns: new[] { "EventId", "UserId" });
        }
    }
}
