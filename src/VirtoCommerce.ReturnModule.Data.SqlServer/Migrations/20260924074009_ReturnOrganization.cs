using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ReturnModule.Data.SqlServer.Migrations
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
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "Return",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            // Returns raised before this migration would otherwise never appear in their
            // organization's list. The orders table lives in the same database unless the Orders
            // module was given its own connection string, hence the guard.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[CustomerOrder]', N'U') IS NOT NULL
    EXEC(N'UPDATE r SET r.[OrganizationId] = o.[OrganizationId], r.[OrganizationName] = o.[OrganizationName]
           FROM [Return] r INNER JOIN [CustomerOrder] o ON o.[Id] = r.[OrderId]
           WHERE r.[OrganizationId] IS NULL AND o.[OrganizationId] IS NOT NULL')");

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
