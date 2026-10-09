using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ReturnModule.Data.MySql.Migrations
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
                type: "datetime(6)",
                nullable: true);

            // Drafts and cancelled returns stay unset: nothing recorded whether a cancelled one had been submitted
            // (VCST-6226). The rest were; CreatedDate stands in for the submit time, not recorded either.
            migrationBuilder.Sql(@"
UPDATE `Return` SET `SubmittedDate` = `CreatedDate`
WHERE LOWER(`Status`) NOT IN ('draft', 'cancelled', 'canceled');");
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
