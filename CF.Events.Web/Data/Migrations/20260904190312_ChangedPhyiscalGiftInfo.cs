using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangedPhyiscalGiftInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowPhysicalGifts",
                schema: "app",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "DonationLink",
                schema: "app",
                table: "Events",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicalGiftInfo",
                schema: "app",
                table: "Events",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhysicalGiftInfo",
                schema: "app",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "DonationLink",
                schema: "app",
                table: "Events",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPhysicalGifts",
                schema: "app",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
