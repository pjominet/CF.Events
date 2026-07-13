using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceDays",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "CommonDietaryOptions",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "DietaryOptionNbrPeople",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "OtherDietaryDetails",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.AddColumn<int>(
                name: "GuestGroupId",
                schema: "identity",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuestGroups",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GuestUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Participants = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestGroups_Users_GuestUserId",
                        column: x => x.GuestUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantsAttendance",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ParticipantName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AttendingDays = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantsAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantsAttendance_Rsvps_EventId_UserId",
                        columns: x => new { x.EventId, x.UserId },
                        principalSchema: "app",
                        principalTable: "Rsvps",
                        principalColumns: new[] { "EventId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantsDiets",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ParticipantName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Restrictions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OtherDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantsDiets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantsDiets_Rsvps_EventId_UserId",
                        columns: x => new { x.EventId, x.UserId },
                        principalSchema: "app",
                        principalTable: "Rsvps",
                        principalColumns: new[] { "EventId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestGroups_GuestUserId",
                schema: "app",
                table: "GuestGroups",
                column: "GuestUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsAttendance_EventId_UserId",
                schema: "app",
                table: "ParticipantsAttendance",
                columns: new[] { "EventId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantsDiets_EventId_UserId",
                schema: "app",
                table: "ParticipantsDiets",
                columns: new[] { "EventId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestGroups",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ParticipantsAttendance",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ParticipantsDiets",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "GuestGroupId",
                schema: "identity",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceDays",
                schema: "app",
                table: "Rsvps",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CommonDietaryOptions",
                schema: "app",
                table: "Rsvps",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DietaryOptionNbrPeople",
                schema: "app",
                table: "Rsvps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OtherDietaryDetails",
                schema: "app",
                table: "Rsvps",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
