using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangedToProperDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteValidity",
                schema: "app",
                table: "Events");

            migrationBuilder.AddColumn<DateOnly>(
                name: "RsvpDeadline",
                schema: "app",
                table: "Events",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RsvpDeadline",
                schema: "app",
                table: "Events");

            migrationBuilder.AddColumn<int>(
                name: "InviteValidity",
                schema: "app",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
