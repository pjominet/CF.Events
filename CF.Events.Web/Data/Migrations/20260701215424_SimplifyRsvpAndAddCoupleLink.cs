using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyRsvpAndAddCoupleLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DietaryRestrictions",
                schema: "rsvps",
                table: "RsvpPersons");

            migrationBuilder.DropColumn(
                name: "OtherDietaryDetails",
                schema: "rsvps",
                table: "RsvpPersons");

            migrationBuilder.DropColumn(
                name: "JoinsForBreakfast",
                schema: "rsvps",
                table: "RsvpFoodPreferences");

            migrationBuilder.DropColumn(
                name: "JoinsForBrunch",
                schema: "rsvps",
                table: "RsvpFoodPreferences");

            migrationBuilder.DropColumn(
                name: "JoinsForDinner",
                schema: "rsvps",
                table: "RsvpFoodPreferences");

            migrationBuilder.DropColumn(
                name: "JoinsForLunch",
                schema: "rsvps",
                table: "RsvpFoodPreferences");

            migrationBuilder.DropColumn(
                name: "RoomType",
                schema: "rsvps",
                table: "RsvpAccommodations");

            migrationBuilder.DropColumn(
                name: "SpecialRequests",
                schema: "rsvps",
                table: "RsvpAccommodations");

            migrationBuilder.RenameColumn(
                name: "Notes",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                newName: "SpecialRequests");

            migrationBuilder.RenameColumn(
                name: "NeedsAccommodation",
                schema: "rsvps",
                table: "RsvpAccommodations",
                newName: "HasBooked");

            migrationBuilder.AddColumn<int>(
                name: "DietaryOption",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LinkedPersonId",
                schema: "invitations",
                table: "InvitedPersons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvitedPersons_LinkedPersonId",
                schema: "invitations",
                table: "InvitedPersons",
                column: "LinkedPersonId",
                unique: true,
                filter: "[LinkedPersonId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitedPersons_InvitedPersons_LinkedPersonId",
                schema: "invitations",
                table: "InvitedPersons",
                column: "LinkedPersonId",
                principalSchema: "invitations",
                principalTable: "InvitedPersons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvitedPersons_InvitedPersons_LinkedPersonId",
                schema: "invitations",
                table: "InvitedPersons");

            migrationBuilder.DropIndex(
                name: "IX_InvitedPersons_LinkedPersonId",
                schema: "invitations",
                table: "InvitedPersons");

            migrationBuilder.DropColumn(
                name: "DietaryOption",
                schema: "rsvps",
                table: "RsvpFoodPreferences");

            migrationBuilder.DropColumn(
                name: "LinkedPersonId",
                schema: "invitations",
                table: "InvitedPersons");

            migrationBuilder.RenameColumn(
                name: "SpecialRequests",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "HasBooked",
                schema: "rsvps",
                table: "RsvpAccommodations",
                newName: "NeedsAccommodation");

            migrationBuilder.AddColumn<string>(
                name: "DietaryRestrictions",
                schema: "rsvps",
                table: "RsvpPersons",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherDietaryDetails",
                schema: "rsvps",
                table: "RsvpPersons",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JoinsForBreakfast",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "JoinsForBrunch",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "JoinsForDinner",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "JoinsForLunch",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                schema: "rsvps",
                table: "RsvpAccommodations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialRequests",
                schema: "rsvps",
                table: "RsvpAccommodations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
