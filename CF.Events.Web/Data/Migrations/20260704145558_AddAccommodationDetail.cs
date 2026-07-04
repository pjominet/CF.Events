using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccommodationDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccommodationNights",
                schema: "app",
                table: "Rsvps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationNights",
                schema: "app",
                table: "Rsvps");
        }
    }
}
