using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pomona.Pwa.Server.Migrations
{
    public partial class UpdateModel8 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyRecords_Items_ItemId",
                table: "DailyRecords");

            migrationBuilder.DropIndex(
                name: "IX_DailyRecords_ItemId",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "DailyRecords");

            migrationBuilder.AddColumn<int>(
                name: "Reference",
                table: "DailyRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ConsolidatedRecord",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<int>(type: "int", nullable: false),
                    RecordType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsolidatedRecord");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "DailyRecords");

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "DailyRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyRecords_ItemId",
                table: "DailyRecords",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyRecords_Items_ItemId",
                table: "DailyRecords",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id");
        }
    }
}
