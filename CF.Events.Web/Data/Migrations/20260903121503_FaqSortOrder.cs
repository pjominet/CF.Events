using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.Events.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class FaqSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                schema: "app",
                table: "EventFaq",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"WITH FaqWithRowNumber AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY EventId ORDER BY Id) - 1 AS NewSortOrder
                    FROM app.EventFaq
                )
                UPDATE ef
                SET ef.SortOrder = fwrn.NewSortOrder
                FROM app.EventFaq ef
                INNER JOIN FaqWithRowNumber fwrn ON ef.Id = fwrn.Id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                schema: "app",
                table: "EventFaq");
        }
    }
}
