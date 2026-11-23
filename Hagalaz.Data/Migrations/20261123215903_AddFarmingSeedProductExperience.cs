using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmingSeedProductExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "product_experience",
                table: "skills_farming_seed_definitions",
                type: "decimal(11,2)",
                precision: 11,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<uint>(
                name: "product_regrow_ticks",
                table: "skills_farming_seed_definitions",
                type: "int(11) unsigned",
                nullable: false,
                defaultValueSql: "'0'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "product_regrow_ticks",
                table: "skills_farming_seed_definitions");

            migrationBuilder.DropColumn(
                name: "product_experience",
                table: "skills_farming_seed_definitions");
        }
    }
}
