using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pomona.Pwa.Server.Migrations
{
    public partial class UpdateModel10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Reference",
                table: "Contracts",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Contracts");
        }
    }
}
