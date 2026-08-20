using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangedSendingFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SaveTheDateEmailSent_New",
                schema: "app",
                table: "EventUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InviteEmailSent_New",
                schema: "app",
                table: "EventUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [app].[EventUsers] SET [SaveTheDateEmailSent_New] = '2026-08-18 12:00:00' WHERE [SaveTheDateEmailSent] = 1;");
            migrationBuilder.Sql("UPDATE [app].[EventUsers] SET [InviteEmailSent_New] = '2026-08-18 12:00:00' WHERE [InviteEmailSent] = 1;");

            migrationBuilder.DropColumn(
                name: "SaveTheDateEmailSent",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "InviteEmailSent",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.RenameColumn(
                name: "SaveTheDateEmailSent_New",
                schema: "app",
                table: "EventUsers",
                newName: "SaveTheDateEmailSent");

            migrationBuilder.RenameColumn(
                name: "InviteEmailSent_New",
                schema: "app",
                table: "EventUsers",
                newName: "InviteEmailSent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SaveTheDateEmailSent_Old",
                schema: "app",
                table: "EventUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InviteEmailSent_Old",
                schema: "app",
                table: "EventUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE [app].[EventUsers] SET [SaveTheDateEmailSent_Old] = 1 WHERE [SaveTheDateEmailSent] IS NOT NULL;");
            migrationBuilder.Sql("UPDATE [app].[EventUsers] SET [InviteEmailSent_Old] = 1 WHERE [InviteEmailSent] IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "SaveTheDateEmailSent",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "InviteEmailSent",
                schema: "app",
                table: "EventUsers");

            migrationBuilder.RenameColumn(
                name: "SaveTheDateEmailSent_Old",
                schema: "app",
                table: "EventUsers",
                newName: "SaveTheDateEmailSent");

            migrationBuilder.RenameColumn(
                name: "InviteEmailSent_Old",
                schema: "app",
                table: "EventUsers",
                newName: "InviteEmailSent");
        }
    }
}
