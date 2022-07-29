using Microsoft.EntityFrameworkCore.Migrations;

namespace Pomona.Pwa.Server.Migrations
{
    public partial class UpdateModel6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DailyRecords",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "DailyRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "DailyRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordType",
                table: "DailyRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRecords_ItemId",
                table: "DailyRecords",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyRecords_Items_ItemId",
                table: "DailyRecords",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyRecords_Items_ItemId",
                table: "DailyRecords");

            migrationBuilder.DropIndex(
                name: "IX_DailyRecords_ItemId",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "DailyRecords");

            migrationBuilder.DropColumn(
                name: "RecordType",
                table: "DailyRecords");
        }
    }
}
