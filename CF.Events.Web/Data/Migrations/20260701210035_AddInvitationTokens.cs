using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_EventUsers_EventId_UserId",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropTable(
                name: "EventUsers",
                schema: "app");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rsvps",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "AccommodationDuration",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "Attending",
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
                name: "NeedsAccommodation",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "OtherDietaryDetails",
                schema: "app",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "AllowPartners",
                schema: "app",
                table: "EventConfigs");

            migrationBuilder.DropColumn(
                name: "OfferBreakfast",
                schema: "app",
                table: "EventConfigs");

            migrationBuilder.DropColumn(
                name: "OfferBrunch",
                schema: "app",
                table: "EventConfigs");

            migrationBuilder.DropColumn(
                name: "OfferDinner",
                schema: "app",
                table: "EventConfigs");

            migrationBuilder.DropColumn(
                name: "OfferLunch",
                schema: "app",
                table: "EventConfigs");

            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.EnsureSchema(
                name: "invitations");

            migrationBuilder.EnsureSchema(
                name: "rsvps");

            migrationBuilder.RenameTable(
                name: "Rsvps",
                schema: "app",
                newName: "Rsvps",
                newSchema: "rsvps");

            migrationBuilder.RenameTable(
                name: "InviteCodes",
                schema: "app",
                newName: "InviteCodes",
                newSchema: "invitations");

            migrationBuilder.RenameTable(
                name: "Events",
                schema: "app",
                newName: "Events",
                newSchema: "events");

            migrationBuilder.RenameTable(
                name: "EventConfigs",
                schema: "app",
                newName: "EventConfigs",
                newSchema: "events");

            migrationBuilder.AlterColumn<bool>(
                name: "MustChangePassword",
                schema: "identity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "identity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                schema: "rsvps",
                table: "Rsvps",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                schema: "rsvps",
                table: "Rsvps",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "rsvps",
                table: "Rsvps",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                schema: "rsvps",
                table: "Rsvps",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvitationId",
                schema: "rsvps",
                table: "Rsvps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "rsvps",
                table: "Rsvps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "rsvps",
                table: "Rsvps",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                schema: "invitations",
                table: "InviteCodes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "invitations",
                table: "InviteCodes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "events",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "events",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                schema: "events",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<bool>(
                name: "ShowAccommodationOptions",
                schema: "events",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "AllowKids",
                schema: "events",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "AllowComments",
                schema: "events",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "AccommodationInfo",
                schema: "events",
                table: "EventConfigs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccommodationLink",
                schema: "events",
                table: "EventConfigs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rsvps",
                schema: "rsvps",
                table: "Rsvps",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CustomQuestions",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StepGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Extras"),
                    StepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ShowIf = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomQuestions_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventDays",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OffersFood = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    OffersAccommodation = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventDays_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                schema: "invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    InviteCodeId = table.Column<int>(type: "int", nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InviteEmailSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AssignedAccommodationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitations_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invitations_InviteCodes_InviteCodeId",
                        column: x => x.InviteCodeId,
                        principalSchema: "invitations",
                        principalTable: "InviteCodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RsvpCustomAnswers",
                schema: "rsvps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RsvpId = table.Column<int>(type: "int", nullable: false),
                    CustomQuestionId = table.Column<int>(type: "int", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    NumberValue = table.Column<int>(type: "int", nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SelectedOptions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RsvpCustomAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RsvpCustomAnswers_CustomQuestions_CustomQuestionId",
                        column: x => x.CustomQuestionId,
                        principalSchema: "events",
                        principalTable: "CustomQuestions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RsvpCustomAnswers_Rsvps_RsvpId",
                        column: x => x.RsvpId,
                        principalSchema: "rsvps",
                        principalTable: "Rsvps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvitedPersons",
                schema: "invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AssignedAccommodationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvitationToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InvitationTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitedPersons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitedPersons_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalSchema: "invitations",
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvitedPersons_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RsvpPersons",
                schema: "rsvps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RsvpId = table.Column<int>(type: "int", nullable: false),
                    InvitedPersonId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsPlusOne = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Attending = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DietaryRestrictions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OtherDietaryDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RsvpPersons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RsvpPersons_InvitedPersons_InvitedPersonId",
                        column: x => x.InvitedPersonId,
                        principalSchema: "invitations",
                        principalTable: "InvitedPersons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RsvpPersons_Rsvps_RsvpId",
                        column: x => x.RsvpId,
                        principalSchema: "rsvps",
                        principalTable: "Rsvps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RsvpAccommodations",
                schema: "rsvps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RsvpPersonId = table.Column<int>(type: "int", nullable: false),
                    EventDayId = table.Column<int>(type: "int", nullable: false),
                    NeedsAccommodation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RoomType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpecialRequests = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RsvpAccommodations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RsvpAccommodations_EventDays_EventDayId",
                        column: x => x.EventDayId,
                        principalSchema: "events",
                        principalTable: "EventDays",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RsvpAccommodations_RsvpPersons_RsvpPersonId",
                        column: x => x.RsvpPersonId,
                        principalSchema: "rsvps",
                        principalTable: "RsvpPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RsvpFoodPreferences",
                schema: "rsvps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RsvpPersonId = table.Column<int>(type: "int", nullable: false),
                    EventDayId = table.Column<int>(type: "int", nullable: false),
                    JoinsForBreakfast = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    JoinsForLunch = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    JoinsForDinner = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    JoinsForBrunch = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RsvpFoodPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RsvpFoodPreferences_EventDays_EventDayId",
                        column: x => x.EventDayId,
                        principalSchema: "events",
                        principalTable: "EventDays",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RsvpFoodPreferences_RsvpPersons_RsvpPersonId",
                        column: x => x.RsvpPersonId,
                        principalSchema: "rsvps",
                        principalTable: "RsvpPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rsvps_EventId",
                schema: "rsvps",
                table: "Rsvps",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Rsvps_InvitationId",
                schema: "rsvps",
                table: "Rsvps",
                column: "InvitationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomQuestions_EventId",
                schema: "events",
                table: "CustomQuestions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventDays_EventId_Date",
                schema: "events",
                table: "EventDays",
                columns: new[] { "EventId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_EventId",
                schema: "invitations",
                table: "Invitations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_InviteCodeId",
                schema: "invitations",
                table: "Invitations",
                column: "InviteCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitedPersons_InvitationId",
                schema: "invitations",
                table: "InvitedPersons",
                column: "InvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitedPersons_InvitationToken",
                schema: "invitations",
                table: "InvitedPersons",
                column: "InvitationToken",
                unique: true,
                filter: "[InvitationToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvitedPersons_UserId",
                schema: "invitations",
                table: "InvitedPersons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpAccommodations_EventDayId",
                schema: "rsvps",
                table: "RsvpAccommodations",
                column: "EventDayId");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpAccommodations_RsvpPersonId_EventDayId",
                schema: "rsvps",
                table: "RsvpAccommodations",
                columns: new[] { "RsvpPersonId", "EventDayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RsvpCustomAnswers_CustomQuestionId",
                schema: "rsvps",
                table: "RsvpCustomAnswers",
                column: "CustomQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpCustomAnswers_RsvpId_CustomQuestionId",
                schema: "rsvps",
                table: "RsvpCustomAnswers",
                columns: new[] { "RsvpId", "CustomQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RsvpFoodPreferences_EventDayId",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                column: "EventDayId");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpFoodPreferences_RsvpPersonId_EventDayId",
                schema: "rsvps",
                table: "RsvpFoodPreferences",
                columns: new[] { "RsvpPersonId", "EventDayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RsvpPersons_InvitedPersonId",
                schema: "rsvps",
                table: "RsvpPersons",
                column: "InvitedPersonId",
                unique: true,
                filter: "[InvitedPersonId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RsvpPersons_RsvpId",
                schema: "rsvps",
                table: "RsvpPersons",
                column: "RsvpId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rsvps_Events_EventId",
                schema: "rsvps",
                table: "Rsvps",
                column: "EventId",
                principalSchema: "events",
                principalTable: "Events",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rsvps_Invitations_InvitationId",
                schema: "rsvps",
                table: "Rsvps",
                column: "InvitationId",
                principalSchema: "invitations",
                principalTable: "Invitations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_Events_EventId",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropForeignKey(
                name: "FK_Rsvps_Invitations_InvitationId",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropTable(
                name: "RsvpAccommodations",
                schema: "rsvps");

            migrationBuilder.DropTable(
                name: "RsvpCustomAnswers",
                schema: "rsvps");

            migrationBuilder.DropTable(
                name: "RsvpFoodPreferences",
                schema: "rsvps");

            migrationBuilder.DropTable(
                name: "CustomQuestions",
                schema: "events");

            migrationBuilder.DropTable(
                name: "EventDays",
                schema: "events");

            migrationBuilder.DropTable(
                name: "RsvpPersons",
                schema: "rsvps");

            migrationBuilder.DropTable(
                name: "InvitedPersons",
                schema: "invitations");

            migrationBuilder.DropTable(
                name: "Invitations",
                schema: "invitations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rsvps",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropIndex(
                name: "IX_Rsvps_EventId",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropIndex(
                name: "IX_Rsvps_InvitationId",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "GroupName",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "InvitationId",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "rsvps",
                table: "Rsvps");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "events",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AccommodationInfo",
                schema: "events",
                table: "EventConfigs");

            migrationBuilder.DropColumn(
                name: "AccommodationLink",
                schema: "events",
                table: "EventConfigs");

            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.RenameTable(
                name: "Rsvps",
                schema: "rsvps",
                newName: "Rsvps",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "InviteCodes",
                schema: "invitations",
                newName: "InviteCodes",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "Events",
                schema: "events",
                newName: "Events",
                newSchema: "app");

            migrationBuilder.RenameTable(
                name: "EventConfigs",
                schema: "events",
                newName: "EventConfigs",
                newSchema: "app");

            migrationBuilder.AlterColumn<bool>(
                name: "MustChangePassword",
                schema: "identity",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "identity",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                schema: "app",
                table: "Rsvps",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                schema: "app",
                table: "Rsvps",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AccommodationDuration",
                schema: "app",
                table: "Rsvps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Attending",
                schema: "app",
                table: "Rsvps",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                schema: "app",
                table: "InviteCodes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "app",
                table: "InviteCodes",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "app",
                table: "Events",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "app",
                table: "Events",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "ShowAccommodationOptions",
                schema: "app",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "AllowKids",
                schema: "app",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "AllowComments",
                schema: "app",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPartners",
                schema: "app",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OfferBreakfast",
                schema: "app",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OfferBrunch",
                schema: "app",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OfferDinner",
                schema: "app",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OfferLunch",
                schema: "app",
                table: "EventConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rsvps",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" });

            migrationBuilder.CreateTable(
                name: "EventUsers",
                schema: "app",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    InviteCodeId = table.Column<int>(type: "int", nullable: false),
                    AssignedAccommodationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InviteEmailSent = table.Column<bool>(type: "bit", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventUsers", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EventUsers_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventUsers_InviteCodes_InviteCodeId",
                        column: x => x.InviteCodeId,
                        principalSchema: "app",
                        principalTable: "InviteCodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventUsers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventUsers_InviteCodeId",
                schema: "app",
                table: "EventUsers",
                column: "InviteCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventUsers_UserId",
                schema: "app",
                table: "EventUsers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rsvps_EventUsers_EventId_UserId",
                schema: "app",
                table: "Rsvps",
                columns: new[] { "EventId", "UserId" },
                principalSchema: "app",
                principalTable: "EventUsers",
                principalColumns: new[] { "EventId", "UserId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
