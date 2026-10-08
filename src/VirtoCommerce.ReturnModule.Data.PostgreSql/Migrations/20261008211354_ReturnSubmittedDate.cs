using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ReturnModule.Data.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class ReturnSubmittedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedDate",
                table: "Return",
                type: "timestamp with time zone",
                nullable: true);

            // Before this column, nothing kept whether a cancelled return had been sent or was a draft its buyer
            // dropped (VCST-6226), so cancelled returns stay out of their organization's list, as drafts do. The
            // rest were submitted; the created date stands in for the submit time, which was not kept either.
            migrationBuilder.Sql(@"
UPDATE ""Return"" SET ""SubmittedDate"" = ""CreatedDate""
WHERE LOWER(""Status"") NOT IN ('draft', 'cancelled', 'canceled');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedDate",
                table: "Return");
        }
    }
}
