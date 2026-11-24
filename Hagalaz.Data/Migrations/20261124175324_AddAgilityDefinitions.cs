using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgilityDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "skills_agility_course_definitions",
                columns: table => new
                {
                    id = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    completion_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "skills_agility_obstacle_definitions",
                columns: table => new
                {
                    object_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.object_id);
                })
                .Annotation("MySQL:Charset", "utf8")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skills_agility_course_definitions");

            migrationBuilder.DropTable(
                name: "skills_agility_obstacle_definitions");
        }
    }
}
