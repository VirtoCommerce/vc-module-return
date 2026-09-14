using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ReturnModule.Data.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class B2bReturnFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedQuantity",
                table: "ReturnLineItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ReturnLineItem",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemState",
                table: "ReturnLineItem",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeasureUnit",
                table: "ReturnLineItem",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ReturnLineItem",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderedQuantity",
                table: "ReturnLineItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProductId",
                table: "ReturnLineItem",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonCode",
                table: "ReturnLineItem",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonComment",
                table: "ReturnLineItem",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "ReturnLineItem",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "ReturnLineItem",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "ReturnLineItem",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Return",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerComment",
                table: "Return",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "Return",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Return",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerReference",
                table: "Return",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Return",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Return",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                table: "Return",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReturnAttachment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReturnLineItemId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnAttachment_ReturnLineItem_ReturnLineItemId",
                        column: x => x.ReturnLineItemId,
                        principalTable: "ReturnLineItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnAttachment_ReturnLineItemId",
                table: "ReturnAttachment",
                column: "ReturnLineItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnAttachment");

            migrationBuilder.DropColumn(
                name: "ApprovedQuantity",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "ItemState",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "MeasureUnit",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "OrderedQuantity",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "ReasonCode",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "ReasonComment",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "ReturnLineItem");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "CustomerComment",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "CustomerReference",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Return");
        }
    }
}
