using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ReturnModule.Data.Migrations
{
    public partial class LineItemFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ReturnLineItem",
                type: "Money",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ReturnLineItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ReturnLineItem",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ReturnLineItem");
        }
    }
}
