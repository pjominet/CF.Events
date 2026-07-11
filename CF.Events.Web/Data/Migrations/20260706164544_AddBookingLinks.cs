using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationNights",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "BookingLinks",
                schema: "app",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceDays",
                schema: "app",
                table: "Rsvps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "BookingLinks",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingLinks_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingLinks_EventId",
                schema: "app",
                table: "BookingLinks",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingLinks",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "AttendanceDays",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.AddColumn<int>(
                name: "AccommodationNights",
                schema: "app",
                table: "Rsvps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BookingLinks",
                schema: "app",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
