using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteCodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvitationInviteCode",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.RenameColumn(
                name: "InvitationEmailSent",
                schema: "app",
                table: "UserEvents",
                newName: "InviteEmailSent");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_Code",
                schema: "app",
                table: "InviteCodes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InviteCodes_Code",
                schema: "app",
                table: "InviteCodes");

            migrationBuilder.RenameColumn(
                name: "InviteEmailSent",
                schema: "app",
                table: "UserEvents",
                newName: "InvitationEmailSent");

            migrationBuilder.AddColumn<string>(
                name: "InvitationInviteCode",
                schema: "app",
                table: "UserEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
