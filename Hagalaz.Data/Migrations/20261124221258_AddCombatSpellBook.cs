using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatSpellBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "spell_book",
                table: "skills_magic_combat_definitions",
                type: "enum('StandartBook','AncientBook','LunarBook','DungeoneeringBook')",
                nullable: false,
                defaultValueSql: "'StandartBook'");

            // The MySQL provider ignores Drop/AddPrimaryKey operations, so the key change is spelled out.
            migrationBuilder.Sql("ALTER TABLE `skills_magic_combat_definitions` DROP PRIMARY KEY, ADD PRIMARY KEY (`button_id`, `spell_book`);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only standard-book rows can survive the narrower key, so the Ancient rows are removed first.
            migrationBuilder.Sql("DELETE FROM `skills_magic_combat_definitions` WHERE `spell_book` <> 'StandartBook';");
            migrationBuilder.Sql("ALTER TABLE `skills_magic_combat_definitions` DROP PRIMARY KEY, ADD PRIMARY KEY (`button_id`);");

            migrationBuilder.DropColumn(
                name: "spell_book",
                table: "skills_magic_combat_definitions");
        }
    }
}
