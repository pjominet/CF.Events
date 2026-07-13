using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedInviteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventUsers_InviteCodes_InviteCodeId",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_InviteCodes_Events_EventId",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropIndex(
                name: "IX_InviteCodes_Code",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropIndex(
                name: "IX_InviteCodes_EventId",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropIndex(
                name: "IX_EventUsers_InviteCodeId",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropColumn(
                name: "EventId",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropColumn(
                name: "InviteCodeId",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.RenameColumn(
                name: "Label",
                schema: "app",
                table: "InviteCodes",
                newName: "Value");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                schema: "app",
                table: "InviteCodes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "InviteValidity",
                schema: "app",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_UserId",
                schema: "app",
                table: "InviteCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_Value",
                schema: "app",
                table: "InviteCodes",
                column: "Value",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InviteCodes_Users_UserId",
                schema: "app",
                table: "InviteCodes",
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
                name: "FK_InviteCodes_Users_UserId",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropIndex(
                name: "IX_InviteCodes_UserId",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropIndex(
                name: "IX_InviteCodes_Value",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.DropColumn(
                name: "InviteValidity",
                schema: "app",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "Value",
                schema: "app",
                table: "InviteCodes",
                newName: "Label");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "app",
                table: "InviteCodes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                schema: "app",
                table: "InviteCodes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InviteCodeId",
                schema: "app",
                table: "EventUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_Code",
                schema: "app",
                table: "InviteCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_EventId",
                schema: "app",
                table: "InviteCodes",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventUsers_InviteCodeId",
                schema: "app",
                table: "EventUsers",
                column: "InviteCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventUsers_InviteCodes_InviteCodeId",
                schema: "app",
                table: "EventUsers",
                column: "InviteCodeId",
                principalSchema: "app",
                principalTable: "InviteCodes",
                principalColumn: "Id");

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
    }
}
