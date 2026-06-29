using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinForDinner",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.AlterColumn<bool>(
                name: "BringsPlusOne",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

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

            migrationBuilder.AddColumn<string>(
                name: "CommonDietaryOptions",
                schema: "app",
                table: "Rsvps",
                type: "nvarchar(4000)",
                maxLength: 4000,
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
                name: "OtherDietaryDetails",
                schema: "app",
                table: "Rsvps",
                type: "nvarchar(500)",
                maxLength: 500,
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
                    OfferDinner = table.Column<bool>(type: "bit", nullable: false),
                    OfferLunch = table.Column<bool>(type: "bit", nullable: false),
                    OfferBreakfast = table.Column<bool>(type: "bit", nullable: false),
                    OfferBrunch = table.Column<bool>(type: "bit", nullable: false),
                    ShowAccommodationOptions = table.Column<bool>(type: "bit", nullable: false),
                    AllowComments = table.Column<bool>(type: "bit", nullable: false),
                    AllowPartners = table.Column<bool>(type: "bit", nullable: false),
                    AllowKids = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventConfigs", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_EventConfigs_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "CommonDietaryOptions",
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
                name: "OtherDietaryDetails",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "AccommodationCode",
                schema: "app",
                table: "Events");

            migrationBuilder.AlterColumn<bool>(
                name: "BringsPlusOne",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JoinForDinner",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
