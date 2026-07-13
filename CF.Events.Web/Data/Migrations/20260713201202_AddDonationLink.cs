using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DonationLink",
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
                name: "DonationLink",
                schema: "app",
                table: "Events");
        }
    }
}
