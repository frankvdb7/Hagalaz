using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoleteCharacterPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "characters_permissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "characters_permissions",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    permission = table.Column<string>(type: "enum('SystemAdministrator','GameAdministrator','GameModerator','Donator')", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySQL:Charset", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.master_id, x.permission })
                        .Annotation("MySQL:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "master_id_foreign_key_7",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");
        }
    }
}
