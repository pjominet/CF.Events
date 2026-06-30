using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InvitationEmailSent",
                schema: "app",
                table: "UserEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "InvitationInviteCode",
                schema: "app",
                table: "UserEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledFor",
                schema: "app",
                table: "UserEvents",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvitationEmailSent",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropColumn(
                name: "InvitationInviteCode",
                schema: "app",
                table: "UserEvents");

            migrationBuilder.DropColumn(
                name: "ScheduledFor",
                schema: "app",
                table: "UserEvents");
        }
    }
}
