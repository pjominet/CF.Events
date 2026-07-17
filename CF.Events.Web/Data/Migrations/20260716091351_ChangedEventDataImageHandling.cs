using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangedEventDataImageHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvitationFileName",
                schema: "app",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OriginalInvitationFileName",
                schema: "app",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "app",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "app",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "InvitationFileName",
                schema: "app",
                table: "Events",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalInvitationFileName",
                schema: "app",
                table: "Events",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
