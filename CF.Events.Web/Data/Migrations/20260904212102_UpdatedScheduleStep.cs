using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedScheduleStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeStamp",
                schema: "app",
                table: "EventSchedule",
                newName: "StartTime");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                schema: "app",
                table: "EventSchedule",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                schema: "app",
                table: "EventSchedule");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                schema: "app",
                table: "EventSchedule",
                newName: "TimeStamp");
        }
    }
}
