using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHerblorePotionDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "skills_herblore_potion_definitions",
                columns: table => new
                {
                    potion_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    unfinished_potion_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    primary_item_ids = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    secondary_item_ids = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    secondary_item_count = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.potion_id);
                })
                .Annotation("MySQL:Charset", "utf8")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skills_herblore_potion_definitions");
        }
    }
}
