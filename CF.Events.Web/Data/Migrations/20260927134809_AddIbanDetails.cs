using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIbanDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IbanAccountOwner",
                schema: "app",
                table: "Events",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IbanTransferReference",
                schema: "app",
                table: "Events",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IbanAccountOwner",
                schema: "app",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IbanTransferReference",
                schema: "app",
                table: "Events");
        }
    }
}
