using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeToAuthCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InviteCodes",
                schema: "app");

            migrationBuilder.CreateTable(
                name: "AuthCodes",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthCodes_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthCodes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthCodes_EventId",
                schema: "identity",
                table: "AuthCodes",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthCodes_UserId",
                schema: "identity",
                table: "AuthCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthCodes_Value",
                schema: "identity",
                table: "AuthCodes",
                column: "Value",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthCodes",
                schema: "app");

            migrationBuilder.CreateTable(
                name: "InviteCodes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InviteCodes_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "app",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InviteCodes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_EventId",
                schema: "app",
                table: "InviteCodes",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_UserId",
                schema: "app",
                table: "InviteCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_Value",
                schema: "app",
                table: "InviteCodes",
                column: "Value",
                unique: true);
        }
    }
}
