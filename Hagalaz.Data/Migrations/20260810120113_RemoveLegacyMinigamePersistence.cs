using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyMinigamePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "minigames_barrows");

            migrationBuilder.DropTable(
                name: "minigames_duel_arena");

            migrationBuilder.DropTable(
                name: "minigames_godwars");

            migrationBuilder.DropTable(
                name: "minigames_tzhaar_cave");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "minigames_barrows",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    brother_killed_0 = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    brother_killed_1 = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    brother_killed_2 = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    brother_killed_3 = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    brother_killed_4 = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    brother_killed_5 = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    brother_killed_6 = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    crypt_start_index = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    kill_count = table.Column<int>(type: "int(11)", nullable: false),
                    looted_chest = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    tunnel_index = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_20",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "minigames_duel_arena",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    favourite_rules = table.Column<string>(type: "text", nullable: false),
                    previous_rules = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_21",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "minigames_godwars",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    armadyl_kill_count = table.Column<short>(type: "smallint(6)", nullable: false),
                    bandos_kill_count = table.Column<short>(type: "smallint(6)", nullable: false),
                    saradomin_kill_count = table.Column<short>(type: "smallint(6)", nullable: false),
                    zamorak_kill_count = table.Column<short>(type: "smallint(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_22",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "minigames_tzhaar_cave",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    current_wave_id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_23",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateIndex(
                name: "current_wave_id_foreign_key",
                table: "minigames_tzhaar_cave",
                column: "current_wave_id");
        }
    }
}
