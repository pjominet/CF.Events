using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRsvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Rsvps",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropIndex(
                name: "IX_Rsvps_EventId_UserId",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "app",
                table: "Rsvps");

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

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rsvps",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserEvents_RsvpEventId_RsvpUserId",
                schema: "app",
                table: "UserEvents",
                columns: new[] { "RsvpEventId", "RsvpUserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Rsvps_UserEvents_EventId_UserId",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" },
                principalSchema: "app",
                principalTable: "UserEvents",
                principalColumns: new[] { "EventId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserEvents_Rsvps_RsvpEventId_RsvpUserId",
                schema: "app",
                table: "UserEvents",
                columns: new[] { "RsvpEventId", "RsvpUserId" },
                principalSchema: "app",
                principalTable: "Rsvps",
                principalColumns: new[] { "EventId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_UserEvents_EventId_UserId",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropForeignKey(
                name: "FK_UserEvents_Rsvps_RsvpEventId_RsvpUserId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropIndex(
                name: "IX_UserEvents_RsvpEventId_RsvpUserId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rsvps",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "RsvpEventId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropColumn(
                name: "RsvpUserId",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                schema: "app",
                table: "Rsvps",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rsvps",
                schema: "app",
                table: "Rsvps",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Rsvps_EventId_UserId",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" },
                unique: true);
        }
    }
}
