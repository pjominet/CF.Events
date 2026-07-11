using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovedEventConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventConfigs",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "AccommodationDuration",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "BringsKids",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "BringsPlusOne",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "JoinsForBreakfast",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "JoinsForBrunch",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "JoinsForDinner",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "JoinsForLunch",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "KidsDetails",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "NeedsAccommodation",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "AccommodationCode",
                schema: "app",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "Date",
                schema: "app",
                table: "Events",
                newName: "StartDate");

            migrationBuilder.AddColumn<string>(
                name: "AccommodationCodes",
                schema: "app",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                schema: "app",
                table: "Events",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationCodes",
                schema: "app",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "app",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                schema: "app",
                table: "Events",
                newName: "Date");

            migrationBuilder.AddColumn<int>(
                name: "AccommodationDuration",
                schema: "app",
                table: "Rsvps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BringsKids",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BringsPlusOne",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JoinsForBreakfast",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JoinsForBrunch",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JoinsForDinner",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JoinsForLunch",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KidsDetails",
                schema: "app",
                table: "Rsvps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsAccommodation",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccommodationCode",
                schema: "app",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventConfigs",
                schema: "app",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false),
                    AllowComments = table.Column<bool>(type: "bit", nullable: false),
                    AllowKids = table.Column<bool>(type: "bit", nullable: false),
                    AllowPartners = table.Column<bool>(type: "bit", nullable: false),
                    OfferBreakfast = table.Column<bool>(type: "bit", nullable: false),
                    OfferBrunch = table.Column<bool>(type: "bit", nullable: false),
                    OfferDinner = table.Column<bool>(type: "bit", nullable: false),
                    OfferLunch = table.Column<bool>(type: "bit", nullable: false),
                    ShowAccommodationOptions = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventConfigs", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_EventConfigs_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
