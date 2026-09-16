using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ReturnModule.Data.MySql.Migrations
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
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ItemState",
                table: "ReturnLineItem",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MeasureUnit",
                table: "ReturnLineItem",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ReturnLineItem",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "OrderedQuantity",
                table: "ReturnLineItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProductId",
                table: "ReturnLineItem",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReasonCode",
                table: "ReturnLineItem",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReasonComment",
                table: "ReturnLineItem",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "ReturnLineItem",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "ReturnLineItem",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "ReturnLineItem",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Return",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Return",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerComment",
                table: "Return",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "Return",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Return",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerReference",
                table: "Return",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Return",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Return",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                table: "Return",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReturnAttachment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReturnLineItemId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MimeType = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifiedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Return_CustomerId_StoreId_CreatedDate",
                table: "Return",
                columns: new[] { "CustomerId", "StoreId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Return_OrderId",
                table: "Return",
                column: "OrderId");

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

            migrationBuilder.DropIndex(
                name: "IX_Return_CustomerId_StoreId_CreatedDate",
                table: "Return");

            migrationBuilder.DropIndex(
                name: "IX_Return_OrderId",
                table: "Return");

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
                name: "CancelReason",
                table: "Return");

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
