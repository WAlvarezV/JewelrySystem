using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pomona.Pwa.Server.Migrations
{
    public partial class UpdateModel11 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BalanceValue",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreditValue",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentLimitDate",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FatherId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ProviderId",
                table: "Invoices",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_FatherId",
                table: "Contracts",
                column: "FatherId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ProviderId",
                table: "Contracts",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Contracts_FatherId",
                table: "Contracts",
                column: "FatherId",
                principalTable: "Contracts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Persons_ProviderId",
                table: "Contracts",
                column: "ProviderId",
                principalTable: "Persons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Persons_ProviderId",
                table: "Invoices",
                column: "ProviderId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Contracts_FatherId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Persons_ProviderId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Persons_ProviderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ProviderId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_FatherId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ProviderId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "BalanceValue",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreditValue",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentLimitDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FatherId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Contracts");
        }
    }
}
