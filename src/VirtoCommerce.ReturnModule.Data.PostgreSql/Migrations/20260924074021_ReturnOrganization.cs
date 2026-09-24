using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ReturnModule.Data.PostgreSql.Migrations
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
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "Return",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            // Returns raised before this migration would otherwise never appear in their
            // organization's list. The orders table lives in the same database unless the Orders
            // module was given its own connection string, hence the guard.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF to_regclass('""CustomerOrder""') IS NOT NULL THEN
        UPDATE ""Return"" r
        SET ""OrganizationId"" = o.""OrganizationId"", ""OrganizationName"" = o.""OrganizationName""
        FROM ""CustomerOrder"" o
        WHERE o.""Id"" = r.""OrderId"" AND r.""OrganizationId"" IS NULL AND o.""OrganizationId"" IS NOT NULL;
    END IF;
END $$;");

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
