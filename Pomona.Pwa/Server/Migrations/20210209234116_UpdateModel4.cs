using Microsoft.EntityFrameworkCore.Migrations;

namespace Pomona.Pwa.Server.Migrations
{
    public partial class UpdateModel4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "ANDRÉ");

            migrationBuilder.UpdateData(
                table: "IdentificationTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Cedula de Ciudadanía");

            migrationBuilder.UpdateData(
                table: "IdentificationTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Cedula de Extranjería");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "ANDR�");

            migrationBuilder.UpdateData(
                table: "IdentificationTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Cedula de Ciudadan�a");

            migrationBuilder.UpdateData(
                table: "IdentificationTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Cedula de Extranjer�a");
        }
    }
}
