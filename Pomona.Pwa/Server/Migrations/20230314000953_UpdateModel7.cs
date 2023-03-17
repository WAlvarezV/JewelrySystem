using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pomona.Pwa.Server.Migrations
{
    public partial class UpdateModel7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "DailyRecords",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                table: "DailyRecords");
        }
    }
}
