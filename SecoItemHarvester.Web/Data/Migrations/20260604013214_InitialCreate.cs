using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecoItemHarvester.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemLookup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ItemDescription = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemLookup", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemLookup_ItemNumber",
                table: "ItemLookup",
                column: "ItemNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLookup_Status",
                table: "ItemLookup",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemLookup");
        }
    }
}
