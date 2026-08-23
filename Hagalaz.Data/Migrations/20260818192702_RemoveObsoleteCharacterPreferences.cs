using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoleteCharacterPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "characters_preferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "characters_preferences",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    accept_aid = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    assist_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    attack_style_option_id = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    auto_retaliating = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    bank_tabs = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValueSql: "'0,0,0,0,0,0,0,0'"),
                    bankx = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'1'"),
                    cc_last_entered = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "''"),
                    chat_effects = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    clan_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    defensive_casting = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    fc_last_entered = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, defaultValueSql: "''"),
                    fc_loot_share = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    fc_name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "''"),
                    fc_rank_enter = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    fc_rank_kick = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'7'"),
                    fc_rank_loot = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-2'"),
                    fc_rank_talk = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    filter_profanity = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    friends_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    game_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    guest_cc_last_entered = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "''"),
                    hide_combat_spells = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    hide_misc_spells = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    hide_skill_spells = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    hide_teleport_spells = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    magic_book = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false, defaultValueSql: "'192'"),
                    money_pouch_display = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    pm_availability = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    prayer_book = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    public_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    quick_prayers = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'0'"),
                    right_click_reporting = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    running = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    single_mouse = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    split_chat = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    sum_left_click_option = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    trade_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    xp_counter_display = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    xp_counter_popup = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                })
                .Annotation("MySQL:Charset", "latin1");
        }
    }
}
