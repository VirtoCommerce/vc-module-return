using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ReturnModule.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class ReturnOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "Return",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "Return",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Returns raised before this migration would otherwise never appear in their
            // organization's list. The orders table lives in the same database unless the Orders
            // module was given its own connection string, hence the guard.
            migrationBuilder.Sql(@"
SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'CustomerOrder') > 0,
    'UPDATE `Return` r INNER JOIN `CustomerOrder` o ON o.`Id` = r.`OrderId`
     SET r.`OrganizationId` = o.`OrganizationId`, r.`OrganizationName` = o.`OrganizationName`
     WHERE r.`OrganizationId` IS NULL AND o.`OrganizationId` IS NOT NULL',
    'DO 0');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;");

            migrationBuilder.CreateIndex(
                name: "IX_Return_OrganizationId_StoreId_CreatedDate",
                table: "Return",
                columns: new[] { "OrganizationId", "StoreId", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Return_OrganizationId_StoreId_CreatedDate",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "Return");
        }
    }
}
