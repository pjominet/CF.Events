using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMailSendingOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailWithLink",
                schema: "app",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailWithLink",
                schema: "app",
                table: "Events");
        }
    }
}
