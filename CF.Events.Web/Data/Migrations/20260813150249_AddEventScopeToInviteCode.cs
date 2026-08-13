using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventScopeToInviteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                schema: "app",
                table: "InviteCodes",
                type: "int",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_EventId",
                schema: "app",
                table: "InviteCodes",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_InviteCodes_Events_EventId",
                schema: "app",
                table: "InviteCodes",
                column: "EventId",
                principalSchema: "app",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InviteCodes_Events_EventId",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropIndex(
                name: "IX_InviteCodes_EventId",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropColumn(
                name: "EventId",
                schema: "app",
                table: "InviteCodes");
        }
    }
}
