using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    minimum_x = table.Column<short>(type: "smallint(6)", nullable: false),
                    maximum_x = table.Column<short>(type: "smallint(6)", nullable: false),
                    minimum_y = table.Column<short>(type: "smallint(6)", nullable: false),
                    maximum_y = table.Column<short>(type: "smallint(6)", nullable: false),
                    minimum_z = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    maximum_z = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    minimum_dimension = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    maximum_dimension = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    pvp_allowed = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    multicombat_allowed = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    cannon_allowed = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false, defaultValueSql: "'1'"),
                    familiar_allowed = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "aspnetroles",
                columns: table => new
                {
                    Id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aspnetroles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    display_name = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'#Player'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    previous_display_name = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    display_name_last_changed = table.Column<DateTime>(type: "datetime", nullable: true),
                    register_ip = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    register_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    last_ip = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    last_lobby_login = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    last_game_login = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    birthyear = table.Column<short>(type: "smallint(6)", nullable: false),
                    coord_x = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'3100'"),
                    coord_y = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'3499'"),
                    coord_z = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    username = table.Column<string>(type: "varchar(32)", nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    NormalizedUserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    EmailConfirmed = table.Column<ulong>(type: "bit(1)", nullable: false, defaultValueSql: "b'0'"),
                    password = table.Column<string>(type: "longtext", nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    SecurityStamp = table.Column<string>(type: "longtext", nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    PhoneNumberConfirmed = table.Column<ulong>(type: "bit(1)", nullable: false, defaultValueSql: "b'0'"),
                    TwoFactorEnabled = table.Column<ulong>(type: "bit(1)", nullable: false, defaultValueSql: "b'0'"),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime(6)", maxLength: 6, nullable: true),
                    LockoutEnabled = table.Column<ulong>(type: "bit(1)", nullable: false, defaultValueSql: "b'0'"),
                    AccessFailedCount = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_preferences",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    single_mouse = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    chat_effects = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    split_chat = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    accept_aid = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    filter_profanity = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    running = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    bank_tabs = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValueSql: "'0,0,0,0,0,0,0,0'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    bankx = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'1'"),
                    fc_name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    fc_last_entered = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    fc_rank_enter = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    fc_rank_talk = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    fc_rank_kick = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'7'"),
                    fc_rank_loot = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-2'"),
                    fc_loot_share = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    cc_last_entered = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    guest_cc_last_entered = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    pm_availability = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    defensive_casting = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    magic_book = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false, defaultValueSql: "'192'"),
                    prayer_book = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    attack_style_option_id = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    auto_retaliating = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    hide_combat_spells = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    hide_teleport_spells = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    hide_misc_spells = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    hide_skill_spells = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    sum_left_click_option = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    game_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    friends_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    clan_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    assist_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    trade_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    public_filter = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    quick_prayers = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'0'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    right_click_reporting = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    xp_counter_popup = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    xp_counter_display = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    money_pouch_display = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_tickets",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ticket_text = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    response_text = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ticket_lastchange = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    moderator_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    completed = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters_tickets", x => x.id);
                },
                comment: "Player System")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "characters_variables",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    variable = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    int_value = table.Column<int>(type: "int", nullable: true),
                    string_value = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, defaultValueSql: "''", collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.master_id, x.variable })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "characterscreateinfo_items",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    container_type = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'2'"),
                    count = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.item_id, x.container_type })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "clans",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValueSql: "''", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    creation_date = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clans", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "configurations",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(255)", nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    type = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    value = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.name);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "equipment_bonuses",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    attack_stab = table.Column<int>(type: "int(32)", nullable: false),
                    attack_slash = table.Column<int>(type: "int(32)", nullable: false),
                    attack_crush = table.Column<int>(type: "int(32)", nullable: false),
                    attack_magic = table.Column<int>(type: "int(32)", nullable: false),
                    attack_ranged = table.Column<int>(type: "int(32)", nullable: false),
                    defence_stab = table.Column<int>(type: "int(32)", nullable: false),
                    defence_slash = table.Column<int>(type: "int(32)", nullable: false),
                    defence_crush = table.Column<int>(type: "int(32)", nullable: false),
                    defence_magic = table.Column<int>(type: "int(32)", nullable: false),
                    defence_ranged = table.Column<int>(type: "int(32)", nullable: false),
                    defence_summoning = table.Column<int>(type: "int(32)", nullable: false),
                    absorb_melee = table.Column<int>(type: "int(32)", nullable: false),
                    absorb_magic = table.Column<int>(type: "int(32)", nullable: false),
                    absorb_range = table.Column<int>(type: "int(32)", nullable: false),
                    strength = table.Column<int>(type: "int(32)", nullable: false),
                    ranged_strength = table.Column<int>(type: "int(32)", nullable: false),
                    prayer = table.Column<int>(type: "int(32)", nullable: false),
                    magic_damage = table.Column<int>(type: "int(32)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "equipment_definitions",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValueSql: "''", comment: "This field is updated automatically by the server.", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    fullbody = table.Column<string>(type: "enum('false','true')", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fullhat = table.Column<string>(type: "enum('false','true')", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fullmask = table.Column<string>(type: "enum('false','true')", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    defence_anim = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attackanim1 = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attackanim2 = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attackanim3 = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attackanim4 = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attackgfx1 = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attackgfx2 = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attackgfx3 = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attackgfx4 = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    attack_distance = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_definitions", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "game_events",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    start_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_events", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "gameobject_lodestones",
                columns: table => new
                {
                    gameobject_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    button_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    state_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    coord_x = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    coord_y = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    coord_z = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.gameobject_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "gameobject_loot",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    maximum_loot_count = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    randomize_loot_count = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gameobject_loot", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "gameobject_spawns",
                columns: table => new
                {
                    spawn_id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    gameobject_id = table.Column<uint>(type: "int(8) unsigned", nullable: false),
                    coord_x = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'3200'"),
                    coord_y = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'3200'"),
                    coord_z = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    face = table.Column<short>(type: "smallint(6)", nullable: false),
                    type = table.Column<short>(type: "smallint(11)", nullable: false, defaultValueSql: "'10'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.spawn_id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "item_combines",
                columns: table => new
                {
                    req_item_id_1 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_item_id_2 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    rew_item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_item_count_1 = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    req_item_count_2 = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    rew_item_count = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    req_skill_id_1 = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    req_skill_id_2 = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    req_skill_id_3 = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    rew_skill_id_1 = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    rew_skill_id_2 = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    rew_skill_id_3 = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    req_skill_count_1 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    req_skill_count_2 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    req_skill_count_3 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    rew_skill_exp_1 = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    rew_skill_exp_2 = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    rew_skill_exp_3 = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    graphic_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    anim_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.req_item_id_1);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "item_definitions",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValueSql: "'unknown'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    examine = table.Column<string>(type: "text", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    trade_price = table.Column<int>(type: "int(10)", nullable: false),
                    low_alchemy_value = table.Column<int>(type: "int(10)", nullable: false),
                    high_alchemy_value = table.Column<int>(type: "int(10)", nullable: false),
                    tradeable = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false, defaultValueSql: "'1'"),
                    weight = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_definitions", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "item_loot",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    maximum_loot_count = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    randomize_loot_count = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_loot", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "item_spawns",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    coord_x = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'3200'"),
                    coord_y = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'3200'"),
                    coord_z = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    count = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'1'"),
                    respawn_ticks = table.Column<uint>(type: "int unsigned", nullable: false, defaultValueSql: "'100'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.item_id, x.coord_x, x.coord_y, x.coord_z })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 0, 0 });
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "logs_activities",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(19) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    date = table.Column<DateTime>(type: "datetime", nullable: false),
                    short_desc = table.Column<string>(type: "tinytext", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    full_desc = table.Column<string>(type: "tinytext", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs_activities", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "logs_chat",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(19) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    date = table.Column<DateTime>(type: "datetime", nullable: true),
                    type = table.Column<byte>(type: "tinyint(4) unsigned", nullable: false),
                    receiver_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    message = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs_chat", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "logs_connections",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    port = table.Column<int>(type: "int", nullable: false),
                    ip = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, defaultValueSql: "''", collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    time = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.port })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "logs_display_name_changes",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(19) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    previous_display_name = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    new_display_name = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    date_changed = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs_display_name_changes", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "logs_login_attempts",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(19) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    date = table.Column<DateTime>(type: "datetime", nullable: true),
                    ip = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    attempt = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    type = table.Column<sbyte>(type: "tinyint(4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs_login_attempts", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "minigames_tzhaar_cave_waves",
                columns: table => new
                {
                    wave_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    npc_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    count = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.wave_id, x.npc_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "music_definitions",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    hint = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_music_definitions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "npc_bonuses",
                columns: table => new
                {
                    npc_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    attack_stab = table.Column<short>(type: "smallint(8)", nullable: false),
                    attack_slash = table.Column<short>(type: "smallint(8)", nullable: false),
                    attack_crush = table.Column<short>(type: "smallint(8)", nullable: false),
                    attack_magic = table.Column<short>(type: "smallint(8)", nullable: false),
                    attack_ranged = table.Column<short>(type: "smallint(8)", nullable: false),
                    defence_stab = table.Column<short>(type: "smallint(8)", nullable: false),
                    defence_slash = table.Column<short>(type: "smallint(8)", nullable: false),
                    defence_crush = table.Column<short>(type: "smallint(8)", nullable: false),
                    defence_magic = table.Column<short>(type: "smallint(8)", nullable: false),
                    defence_ranged = table.Column<short>(type: "smallint(8)", nullable: false),
                    defence_summoning = table.Column<short>(type: "smallint(8)", nullable: false),
                    absorb_melee = table.Column<short>(type: "smallint(8)", nullable: false),
                    absorb_magic = table.Column<short>(type: "smallint(8)", nullable: false),
                    absorb_range = table.Column<short>(type: "smallint(8)", nullable: false),
                    strength = table.Column<short>(type: "smallint(8)", nullable: false),
                    ranged_strength = table.Column<short>(type: "smallint(8)", nullable: false),
                    prayer = table.Column<short>(type: "smallint(8)", nullable: false),
                    magic = table.Column<short>(type: "smallint(8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.npc_id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "npc_loot",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    maximum_loot_count = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'1'"),
                    randomize_loot_count = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false, defaultValueSql: "'1'"),
                    always = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_loot", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "npc_spawns",
                columns: table => new
                {
                    spawn_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    npc_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    coord_x = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'3222'"),
                    coord_y = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'3222'"),
                    coord_z = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    min_coord_x = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'3200'"),
                    min_coord_y = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'3200'"),
                    min_coord_z = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    max_coord_x = table.Column<short>(type: "smallint", nullable: false),
                    max_coord_y = table.Column<short>(type: "smallint", nullable: false),
                    max_coord_z = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    spawn_direction = table.Column<sbyte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.spawn_id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "npc_statistics",
                columns: table => new
                {
                    npc_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    max_lifepoints = table.Column<ushort>(type: "smallint(8) unsigned", nullable: false, defaultValueSql: "'10'"),
                    attack_level = table.Column<ushort>(type: "smallint(8) unsigned", nullable: false, defaultValueSql: "'1'"),
                    defence_level = table.Column<ushort>(type: "smallint(8) unsigned", nullable: false, defaultValueSql: "'1'"),
                    strength_level = table.Column<ushort>(type: "smallint(8) unsigned", nullable: false, defaultValueSql: "'1'"),
                    ranged_level = table.Column<ushort>(type: "smallint(8) unsigned", nullable: false, defaultValueSql: "'1'"),
                    magic_level = table.Column<ushort>(type: "smallint(8) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.npc_id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "OpenIddictApplications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApplicationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientSecret = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyToken = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayNames = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JsonWebKeySet = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Permissions = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PostLogoutRedirectUris = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Properties = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedirectUris = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Requirements = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Settings = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictApplications", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "OpenIddictScopes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyToken = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descriptions = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayNames = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Properties = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Resources = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictScopes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "profanity_words",
                columns: table => new
                {
                    word = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.word);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "quests",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    req_quest_id_1 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_quest_id_2 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_quest_id_3 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_quest_id_4 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    min_skill_id_1 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    min_skill_id_2 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    min_skill_id_3 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    min_skill_id_4 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    min_skill_count_1 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    min_skill_count_2 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    min_skill_count_3 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    min_skill_count_4 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    req_item_id_1 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_item_id_2 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_item_id_3 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_item_id_4 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_item_count_1 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    req_item_count_2 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    req_item_count_3 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    req_item_count_4 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    req_npc_id_1 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_npc_id_2 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_npc_id_3 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_npc_id_4 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    req_npc_count_1 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    req_npc_count_2 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    req_npc_count_3 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    req_npc_count_4 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_skill_id_1 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    rew_skill_id_2 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    rew_skill_id_3 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    rew_skill_id_4 = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    rew_skill_exp_1 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_skill_exp_2 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_skill_exp_3 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_skill_exp_4 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_item_id_1 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    rew_item_id_2 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    rew_item_id_3 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    rew_item_id_4 = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    rew_item_count_1 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_item_count_2 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_item_count_3 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_item_count_4 = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rew_quest_points = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quests", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "reserved_names",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, defaultValueSql: "''", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.name);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "shops",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    capacity = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'1'"),
                    currency_id = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValueSql: "'995'"),
                    main_stock_items = table.Column<string>(type: "text", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    sample_stock_items = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    general_store = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shops", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "skills_cooking_food_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    heal_amount = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    left_item_id = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    eating_time = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'3'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_cooking_raw_food_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    cooked_item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    burnt_item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    stop_burning_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_crafting_gem_definitions",
                columns: table => new
                {
                    resource_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    animation_id = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.resource_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_crafting_jewelry_definitions",
                columns: table => new
                {
                    child_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    type = table.Column<string>(type: "enum('Amulet','Necklace','Bracelet','Ring')", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    resource_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.child_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_crafting_leather_definitions",
                columns: table => new
                {
                    product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    resource_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_resource_count = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.product_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_crafting_pottery_definitions",
                columns: table => new
                {
                    formed_product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    baked_product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    form_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    bake_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.formed_product_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_crafting_silver_definitions",
                columns: table => new
                {
                    child_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    mould_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.child_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_crafting_spin_definitions",
                columns: table => new
                {
                    resource_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.resource_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_crafting_tan_definitions",
                columns: table => new
                {
                    resource_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    base_price = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.resource_id, x.product_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_farming_patch_definitions",
                columns: table => new
                {
                    object_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    type = table.Column<string>(type: "enum('Herb','Tree','Bush','FruitTree','Flower','Hop','Allotment')", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.object_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_farming_seed_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    minimum_product_count = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    maximum_product_count = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    planting_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    harvest_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    varpbit_index = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    max_cycles = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'4'"),
                    cycle_ticks = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'100'"),
                    type = table.Column<string>(type: "enum('Herb','Bush','FruitTree','Flower','Hop','Tree','Allotment')", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_firemaking_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    fire_object_id = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'2732'"),
                    ticks = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'100'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_fishing_tool_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    fish_animation_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    cast_animation_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_fletching_definitions",
                columns: table => new
                {
                    resource_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    tool_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    product_ids = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'0,0,0,0'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    product_counts = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'1,1,1,1'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    product_experiences = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'0,0,0,0'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    required_levels = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'1,1,1,1'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    animation_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.resource_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_herblore_herb_definitions",
                columns: table => new
                {
                    grimy_item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    clean_item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.grimy_item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_magic_combat_definitions",
                columns: table => new
                {
                    button_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    base_damage = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    base_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    required_runes = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    required_runes_counts = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    cast_animation_id = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    cast_graphic_id = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    end_graphic_id = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    projectile_id = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    autocast_config = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.button_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_magic_enchant_definitions",
                columns: table => new
                {
                    button_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_runes = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    required_runes_counts = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    graphic_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.button_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_magic_teleport_definitions",
                columns: table => new
                {
                    button_id = table.Column<short>(type: "smallint", nullable: false),
                    spell_book = table.Column<string>(type: "enum('StandartBook','AncientBook','LunarBook','DungeoneeringBook')", nullable: false, defaultValueSql: "'StandartBook'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    coord_x = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValueSql: "'3222'"),
                    coord_y = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValueSql: "'3222'"),
                    coord_z = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    distance = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    required_runes = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    required_runes_count = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.button_id, x.spell_book })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_mining_ore_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    respawn_time = table.Column<decimal>(type: "decimal(11,3)", precision: 11, scale: 3, nullable: false, defaultValueSql: "'1.000'"),
                    exhaust_chance = table.Column<decimal>(type: "decimal(11,3)", nullable: false),
                    base_harvest_chance = table.Column<decimal>(type: "decimal(11,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_mining_pickaxe_definitions",
                columns: table => new
                {
                    type = table.Column<string>(type: "enum('Dragon','Rune','Adamant','Mithril','Steel','Iron','Bronze')", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    animation_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    base_harvest_chance = table.Column<decimal>(type: "decimal(11,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.type, x.item_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_prayer_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    experience = table.Column<decimal>(type: "decimal(11,2)", nullable: false),
                    type = table.Column<string>(type: "enum('Ashes','Bones')", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_runecrafting_definitions",
                columns: table => new
                {
                    altar_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    ruin_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    portal_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rift_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    rune_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    talisman_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    tiara_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    level_count_multipliers = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValueSql: "'-1'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    altar_location = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'3222,3222,0'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    ruin_location = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'3222,3222,0'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    rift_location = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'3222,3222,0'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.altar_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_slayer_master_definitions",
                columns: table => new
                {
                    npc_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    base_slayer_reward_points = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.npc_id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "skills_summoning_definitions",
                columns: table => new
                {
                    npc_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    pouch_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    scroll_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    config_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    summon_spawn_cost = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    summon_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    summon_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    create_pouch_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    scroll_experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    ticks = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'100'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.npc_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_woodcutting_hatchet_definitions",
                columns: table => new
                {
                    type = table.Column<string>(type: "enum('Bronze','Iron','Black','Steel','Mithril','Adamant','Rune','Dragon')", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    chop_animation_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    canoue_animation_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    base_harvest_chance = table.Column<decimal>(type: "decimal(11,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.type, x.item_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_woodcutting_log_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    respawn_time = table.Column<decimal>(type: "decimal(11,3)", precision: 11, scale: 3, nullable: false, defaultValueSql: "'1.000'", comment: "Respawn time in minutes"),
                    fall_chance = table.Column<decimal>(type: "decimal(11,3)", precision: 11, scale: 3, nullable: false, defaultValueSql: "'0.150'"),
                    base_harvest_chance = table.Column<decimal>(type: "decimal(11,3)", precision: 11, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "world_configurations",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    value = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_configurations", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "worlds",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "tinytext", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    members_only = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'1'"),
                    quick_chat_allowed = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    high_risk = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    skill_req = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    loot_share_allowed = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    highlight = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    game_admins_only = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    lobby_admins_only = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    account_creation_enabled = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'1'"),
                    direct_login_enabled = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    region = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false, defaultValueSql: "'PT'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    country = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValueSql: "'12'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worlds", x => x.id);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "aspnetroleclaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aspnetroleclaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "aspnetroles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "AspnetroleCharacter",
                columns: table => new
                {
                    RolesId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    UsersId = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspnetroleCharacter", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_AspnetroleCharacter_aspnetroles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "aspnetroles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspnetroleCharacter_characters_UsersId",
                        column: x => x.UsersId,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "aspnetuserclaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aspnetuserclaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_characters_UserId",
                        column: x => x.UserId,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "aspnetuserlogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderKey = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderDisplayName = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.LoginProvider, x.ProviderKey })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_characters_UserId",
                        column: x => x.UserId,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "aspnetuserroles",
                columns: table => new
                {
                    UserId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    RoleId = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aspnetuserroles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "aspnetroles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_characters_UserId",
                        column: x => x.UserId,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "aspnetusertokens",
                columns: table => new
                {
                    UserId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.UserId, x.LoginProvider, x.Name })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_characters_UserId",
                        column: x => x.UserId,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "characters_appeals",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    data = table.Column<string>(type: "text", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters_appeals", x => x.id);
                    table.ForeignKey(
                        name: "master_foreign_key",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_contacts",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    contact_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    type = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    fc_rank = table.Column<sbyte>(type: "tinyint(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.master_id, x.contact_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "contact_id_foreign_key",
                        column: x => x.contact_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "master_id_foreign_key_14",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_items",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    item_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    count = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'1'"),
                    slot_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    container_type = table.Column<sbyte>(type: "tinyint", nullable: false),
                    extra_data = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters_items", x => x.id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_15",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_items_look",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    item_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    male_worn_model_1 = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'-1'"),
                    male_worn_model_2 = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'-1'"),
                    female_worn_model_1 = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'-1'"),
                    female_worn_model_2 = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'-1'"),
                    male_worn_model_3 = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'-1'"),
                    female_worn_model_3 = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'-1'"),
                    model_colours = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    texture_colours = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.master_id, x.item_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "master_id_foreign_key_2",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_look",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    gender = table.Column<short>(type: "smallint(6)", nullable: false),
                    hair_color = table.Column<short>(type: "smallint(6)", nullable: false),
                    torso_color = table.Column<short>(type: "smallint(6)", nullable: false),
                    leg_color = table.Column<short>(type: "smallint(6)", nullable: false),
                    feet_color = table.Column<short>(type: "smallint(6)", nullable: false),
                    skin_color = table.Column<short>(type: "smallint(6)", nullable: false),
                    hair_look = table.Column<int>(type: "int(10)", nullable: false),
                    beard_look = table.Column<int>(type: "int(10)", nullable: false),
                    torso_look = table.Column<int>(type: "int(10)", nullable: false),
                    arms_look = table.Column<int>(type: "int(10)", nullable: false),
                    wrist_look = table.Column<int>(type: "int(10)", nullable: false),
                    legs_look = table.Column<int>(type: "int(10)", nullable: false),
                    feet_look = table.Column<int>(type: "int(10)", nullable: false),
                    display_title = table.Column<byte>(type: "tinyint(4) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_13",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_music",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    unlocked_music = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_3",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "characters_music_playlists",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    playlist_toggled = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    shuffle_toggled = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    playlist = table.Column<string>(type: "varchar(56)", maxLength: 56, nullable: false, defaultValueSql: "''", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_4",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "characters_notes",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    note_id = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    colour = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    text = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValueSql: "''", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.master_id, x.note_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "master_id_foreign_key_6",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "characters_offences",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    offence_type = table.Column<byte>(type: "tinyint(4) unsigned", nullable: false),
                    date = table.Column<DateTime>(type: "datetime", nullable: false),
                    expire_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    moderator_id = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    appeal_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    expired = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters_offences", x => x.id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_5",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "moderator_id_foreign_key",
                        column: x => x.moderator_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_permissions",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    permission = table.Column<string>(type: "enum('SystemAdministrator','GameAdministrator','GameModerator','Donator')", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.master_id, x.permission })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "master_id_foreign_key_7",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_profiles",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    data = table.Column<string>(type: "json", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_24",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_reports",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    reporter_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    reported_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    type = table.Column<byte>(type: "tinyint(4) unsigned", nullable: false),
                    date = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters_reports", x => x.id);
                    table.ForeignKey(
                        name: "reported_id_foreign_key",
                        column: x => x.reported_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "reporter_id_foreign_key",
                        column: x => x.reporter_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_rewards",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    item_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    count = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'1'"),
                    extra_data = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    loaded = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters_rewards", x => x.id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_10",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_states",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    state_id = table.Column<int>(type: "int(11)", nullable: false),
                    ticks_left = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.master_id, x.state_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "master_id_foreign_key_17",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "characters_statistics",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    attack_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    attack_exp = table.Column<int>(type: "int(11)", nullable: false),
                    defence_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    defence_exp = table.Column<int>(type: "int(11)", nullable: false),
                    strength_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    strength_exp = table.Column<int>(type: "int(11)", nullable: false),
                    constitution_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'10'"),
                    constitution_exp = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'1154'"),
                    range_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    range_exp = table.Column<int>(type: "int(11)", nullable: false),
                    prayer_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    prayer_exp = table.Column<int>(type: "int(11)", nullable: false),
                    magic_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    magic_exp = table.Column<int>(type: "int(11)", nullable: false),
                    cooking_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    cooking_exp = table.Column<int>(type: "int(11)", nullable: false),
                    woodcutting_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    woodcutting_exp = table.Column<int>(type: "int(11)", nullable: false),
                    fletching_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    fletching_exp = table.Column<int>(type: "int(11)", nullable: false),
                    fishing_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    fishing_exp = table.Column<int>(type: "int(11)", nullable: false),
                    firemaking_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    firemaking_exp = table.Column<int>(type: "int(11)", nullable: false),
                    crafting_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    crafting_exp = table.Column<int>(type: "int(11)", nullable: false),
                    smithing_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    smithing_exp = table.Column<int>(type: "int(11)", nullable: false),
                    mining_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    mining_exp = table.Column<int>(type: "int(11)", nullable: false),
                    herblore_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    herblore_exp = table.Column<int>(type: "int(11)", nullable: false),
                    agility_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    agility_exp = table.Column<int>(type: "int(11)", nullable: false),
                    thieving_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    thieving_exp = table.Column<int>(type: "int(11)", nullable: false),
                    slayer_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    slayer_exp = table.Column<int>(type: "int(11)", nullable: false),
                    farming_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    farming_exp = table.Column<int>(type: "int(11)", nullable: false),
                    runecrafting_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    runecrafting_exp = table.Column<int>(type: "int(11)", nullable: false),
                    construction_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    construction_exp = table.Column<int>(type: "int(11)", nullable: false),
                    hunter_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    hunter_exp = table.Column<int>(type: "int(11)", nullable: false),
                    summoning_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    summoning_exp = table.Column<int>(type: "int(11)", nullable: false),
                    dungeoneering_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    dungeoneering_exp = table.Column<int>(type: "int(11)", nullable: false),
                    life_points = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false, defaultValueSql: "'100'"),
                    prayer_points = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false, defaultValueSql: "'100'"),
                    run_energy = table.Column<byte>(type: "tinyint(6) unsigned", nullable: false, defaultValueSql: "'100'"),
                    special_energy = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false, defaultValueSql: "'1000'"),
                    poison_amount = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    play_time = table.Column<ulong>(type: "bigint(19) unsigned", nullable: false),
                    xp_counters = table.Column<string>(type: "varchar(72)", maxLength: 72, nullable: false, defaultValueSql: "'0,0,0'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    tracked_xp_counters = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'30,0,0'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    enabled_xp_counters = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, defaultValueSql: "'1,0,0'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    target_skill_levels = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    target_skill_experiences = table.Column<string>(type: "varchar(312)", maxLength: 312, nullable: false, defaultValueSql: "'-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_11",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

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
                    kill_count = table.Column<int>(type: "int(11)", nullable: false),
                    crypt_start_index = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    tunnel_index = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    looted_chest = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false)
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
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "minigames_duel_arena",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    previous_rules = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    favourite_rules = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
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
                .Annotation("MySql:CharSet", "utf8")
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
                .Annotation("MySql:CharSet", "utf8")
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
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "clans_bans",
                columns: table => new
                {
                    clan_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.clan_id, x.master_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "clan_id_foreign_key_3",
                        column: x => x.clan_id,
                        principalTable: "clans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "master_id_foreign_key_18",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "clans_members",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    clan_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    recruiter_id = table.Column<uint>(type: "int(11) unsigned", nullable: true),
                    rank = table.Column<sbyte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "clan_id_foreign_key_46",
                        column: x => x.clan_id,
                        principalTable: "clans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "master_id_foreign_key_19",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "recruiter_id_foreign_key",
                        column: x => x.recruiter_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "clans_settings",
                columns: table => new
                {
                    clan_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    world_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false, defaultValueSql: "'1'"),
                    recruiting = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    motto = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    national_flag = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    thread_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    time_zone = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    clan_time = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    mottif_top = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    mottif_bottom = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    mottif_colour_left_top = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    mottif_colour_right_bottom = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    primary_clan_colour = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    secondary_clan_colour = table.Column<short>(type: "smallint(6)", nullable: false, defaultValueSql: "'-1'"),
                    rank_to_talk = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'"),
                    rank_to_kick = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'100'"),
                    rank_to_enter_cc = table.Column<sbyte>(type: "tinyint(3)", nullable: false, defaultValueSql: "'-1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.clan_id);
                    table.ForeignKey(
                        name: "clan_id_foreign_key_6",
                        column: x => x.clan_id,
                        principalTable: "clans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "gameobject_definitions",
                columns: table => new
                {
                    gameobject_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValueSql: "'unknown'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    examine = table.Column<string>(type: "text", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    gameobject_loot_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: true, defaultValueSql: "'0'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.gameobject_id);
                    table.ForeignKey(
                        name: "gameobject_loot_id_foreign_key",
                        column: x => x.gameobject_loot_id,
                        principalTable: "gameobject_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "gameobject_loot_items",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    gameobject_loot_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    minimum_count = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    maximum_count = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    probability = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    always = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gameobject_loot_items", x => x.id);
                    table.ForeignKey(
                        name: "gameobject_loot_id_foreign_key_1",
                        column: x => x.gameobject_loot_id,
                        principalTable: "gameobject_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "gameobject_lootrecursion",
                columns: table => new
                {
                    gameobject_loot_child_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    gameobject_loot_parent_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.gameobject_loot_child_id, x.gameobject_loot_parent_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "gameobject_loot_child_id_foreign_key",
                        column: x => x.gameobject_loot_child_id,
                        principalTable: "gameobject_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "gameobject_loot_parent_id_foreign_key",
                        column: x => x.gameobject_loot_parent_id,
                        principalTable: "gameobject_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "item_loot_items",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    item_loot_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    minimum_count = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    maximum_count = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    probability = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    always = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_loot_items", x => x.id);
                    table.ForeignKey(
                        name: "item_loot_id_foreign_key",
                        column: x => x.item_loot_id,
                        principalTable: "item_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "item_lootrecursion",
                columns: table => new
                {
                    item_loot_child_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    item_loot_parent_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.item_loot_child_id, x.item_loot_parent_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "item_loot_table_child_id_foreign_key",
                        column: x => x.item_loot_child_id,
                        principalTable: "item_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "item_loot_table_parent_id_foreign_key",
                        column: x => x.item_loot_parent_id,
                        principalTable: "item_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "music_locations",
                columns: table => new
                {
                    music_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    region_id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.music_id, x.region_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "music_id_foreign_key",
                        column: x => x.music_id,
                        principalTable: "music_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "npc_definitions",
                columns: table => new
                {
                    npc_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValueSql: "'unknown'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    examine = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValueSql: "'It''s an npc.'", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    respawn_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValueSql: "'100'"),
                    combat_level = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValueSql: "'1'"),
                    reaction_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    bounds_type = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'1'"),
                    attackable = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'1'"),
                    walks_randomly = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'1'"),
                    attack_speed = table.Column<uint>(type: "int unsigned", nullable: false, defaultValueSql: "'8'"),
                    attack_animation = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'422'"),
                    attack_graphic = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    defence_animation = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'404'"),
                    defence_graphic = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    death_animation = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'7197'"),
                    death_graphic = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'-1'"),
                    death_ticks = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValueSql: "'7'"),
                    npc_loot_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: true, defaultValueSql: "'0'"),
                    npc_pickpocketing_loot_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: true, defaultValueSql: "'0'"),
                    slayer_level_required = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.npc_id);
                    table.ForeignKey(
                        name: "npc_loot_id_foreign_key",
                        column: x => x.npc_loot_id,
                        principalTable: "npc_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "npc_pickpocket_loot_id_foreign_key",
                        column: x => x.npc_pickpocketing_loot_id,
                        principalTable: "npc_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "npc_loot_items",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    npc_loot_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    minimum_count = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    maximum_count = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "'1'"),
                    probability = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    always = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_loot_items", x => x.id);
                    table.ForeignKey(
                        name: "npc_loot_id_foreign_key_1",
                        column: x => x.npc_loot_id,
                        principalTable: "npc_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "npc_lootrecursion",
                columns: table => new
                {
                    npc_loot_child_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    npc_loot_parent_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.npc_loot_child_id, x.npc_loot_parent_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "npc_loot_child_foreign_key",
                        column: x => x.npc_loot_child_id,
                        principalTable: "npc_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "npc_loot_parent_foreign_key",
                        column: x => x.npc_loot_parent_id,
                        principalTable: "npc_loot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "OpenIddictAuthorizations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApplicationId = table.Column<string>(type: "varchar(255)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyToken = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Properties = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Scopes = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subject = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "OpenIddictApplications",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "characters_quests",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    quest_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    status = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    stage = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_8",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "quest_id_foreign_key",
                        column: x => x.quest_id,
                        principalTable: "quests",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "characters_farming_patches",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    patch_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    seed_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    condition_flag = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    current_cycle = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    current_cycle_ticks = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    product_count = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.master_id, x.patch_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "farming_patch_foreign_key",
                        column: x => x.patch_id,
                        principalTable: "skills_farming_patch_definitions",
                        principalColumn: "object_id");
                    table.ForeignKey(
                        name: "master_id_foreign_key_1",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "seed_id_foreign_key",
                        column: x => x.seed_id,
                        principalTable: "skills_farming_seed_definitions",
                        principalColumn: "item_id");
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_fishing_spot_definitions",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    click_type = table.Column<string>(type: "enum('Option1Click','Option2Click','Option3Click','Option4Click','Option5Click')", nullable: false, defaultValueSql: "'Option1Click'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    exhaust_chance = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    base_catch_chance = table.Column<decimal>(type: "decimal(11,3)", precision: 11, scale: 3, nullable: false),
                    respawn_time = table.Column<decimal>(type: "decimal(11,2)", nullable: false, defaultValueSql: "'1.00'"),
                    minimum_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    tool_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false, defaultValueSql: "'309'"),
                    bait_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills_fishing_spot_definitions", x => x.id);
                    table.ForeignKey(
                        name: "tool_id_foreign_key",
                        column: x => x.tool_id,
                        principalTable: "skills_fishing_tool_definitions",
                        principalColumn: "item_id");
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_magic_enchant_products",
                columns: table => new
                {
                    resource_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    product_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    button_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.resource_id);
                    table.ForeignKey(
                        name: "skills_magic_enchant_products_ibfk_1",
                        column: x => x.button_id,
                        principalTable: "skills_magic_enchant_definitions",
                        principalColumn: "button_id");
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_slayer_task_definitions",
                columns: table => new
                {
                    id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    slayer_master_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    npc_ids = table.Column<string>(type: "text", nullable: false, comment: "The NPC IDs that can be slayed for slayer experience.", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    minimum_count = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "'1'"),
                    maximum_count = table.Column<int>(type: "int(6)", nullable: false, defaultValueSql: "'1'"),
                    level_requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    combat_requirement = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'3'"),
                    coin_count = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills_slayer_task_definitions", x => x.id);
                    table.ForeignKey(
                        name: "slayer_master_foreign_key",
                        column: x => x.slayer_master_id,
                        principalTable: "skills_slayer_master_definitions",
                        principalColumn: "npc_id");
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "blacklist",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ip_address = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, defaultValueSql: "''", collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    moderator_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false, collation: "latin1_swedish_ci")
                        .Annotation("MySql:CharSet", "latin1"),
                    date = table.Column<DateTime>(type: "datetime", nullable: false),
                    appeal_id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blacklist", x => x.id);
                    table.ForeignKey(
                        name: "appeal_id_foreign_key",
                        column: x => x.appeal_id,
                        principalTable: "characters_appeals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "moderator_id_foreign_key_2",
                        column: x => x.moderator_id,
                        principalTable: "characters",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "skills_mining_rock_definitions",
                columns: table => new
                {
                    rock_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    exhaust_rock_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    ore_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.rock_id);
                    table.ForeignKey(
                        name: "rock_id_foreign_key",
                        column: x => x.rock_id,
                        principalTable: "gameobject_definitions",
                        principalColumn: "gameobject_id");
                    table.ForeignKey(
                        name: "skills_mining_rock_definitions_ibfk_1",
                        column: x => x.ore_id,
                        principalTable: "skills_mining_ore_definitions",
                        principalColumn: "item_id");
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_woodcutting_tree_definitions",
                columns: table => new
                {
                    tree_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    stump_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    log_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.tree_id);
                    table.ForeignKey(
                        name: "skills_woodcutting_log_id",
                        column: x => x.log_id,
                        principalTable: "skills_woodcutting_log_definitions",
                        principalColumn: "item_id");
                    table.ForeignKey(
                        name: "skills_woodcutting_tree_id",
                        column: x => x.tree_id,
                        principalTable: "gameobject_definitions",
                        principalColumn: "gameobject_id");
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "characters_familiars",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    familiar_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    special_move_points = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false, defaultValueSql: "'60'"),
                    using_special_move = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false),
                    ticks_remaining = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "familiar_id_foreign_key",
                        column: x => x.familiar_id,
                        principalTable: "npc_definitions",
                        principalColumn: "npc_id");
                    table.ForeignKey(
                        name: "master_id_foreign_key",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateTable(
                name: "OpenIddictTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApplicationId = table.Column<string>(type: "varchar(255)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthorizationId = table.Column<string>(type: "varchar(255)", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyToken = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Payload = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Properties = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedemptionDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReferenceId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subject = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_0900_ai_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "OpenIddictApplications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalTable: "OpenIddictAuthorizations",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.CreateTable(
                name: "skills_fishing_fish_definitions",
                columns: table => new
                {
                    item_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    required_level = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValueSql: "'1'"),
                    experience = table.Column<decimal>(type: "decimal(11,2)", precision: 11, scale: 2, nullable: false),
                    spot_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    probability = table.Column<decimal>(type: "decimal(11,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.item_id);
                    table.ForeignKey(
                        name: "spot_id_foreign_key",
                        column: x => x.spot_id,
                        principalTable: "skills_fishing_spot_definitions",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "skills_fishing_spot_npc_definitions",
                columns: table => new
                {
                    spot_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    npc_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.spot_id, x.npc_id })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "spot_id_foreign_key_11",
                        column: x => x.spot_id,
                        principalTable: "skills_fishing_spot_definitions",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "characters_slayer_tasks",
                columns: table => new
                {
                    master_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    slayer_task_id = table.Column<ushort>(type: "smallint(6) unsigned", nullable: false),
                    kill_count = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.master_id);
                    table.ForeignKey(
                        name: "master_id_foreign_key_16",
                        column: x => x.master_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "slayer_task_id_foreign_key",
                        column: x => x.slayer_task_id,
                        principalTable: "skills_slayer_task_definitions",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "latin1")
                .Annotation("Relational:Collation", "latin1_swedish_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AspnetroleCharacter_UsersId",
                table: "AspnetroleCharacter",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "aspnetroleclaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "aspnetroles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "aspnetuserclaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "aspnetuserlogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "aspnetuserroles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "appeal_id_foreign_key",
                table: "blacklist",
                column: "appeal_id");

            migrationBuilder.CreateIndex(
                name: "moderator_id_foreign_key_2",
                table: "blacklist",
                column: "moderator_id");

            migrationBuilder.CreateIndex(
                name: "display_name",
                table: "characters",
                column: "display_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "characters",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "master_id",
                table: "characters",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "username",
                table: "characters",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "characters",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "master_foreign_key",
                table: "characters_appeals",
                column: "master_id");

            migrationBuilder.CreateIndex(
                name: "contact_id_foreign_key",
                table: "characters_contacts",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "master_id_foreign_key_14",
                table: "characters_contacts",
                column: "master_id");

            migrationBuilder.CreateIndex(
                name: "familiar_id_foreign_key",
                table: "characters_familiars",
                column: "familiar_id");

            migrationBuilder.CreateIndex(
                name: "farming_patch_foreign_key",
                table: "characters_farming_patches",
                column: "patch_id");

            migrationBuilder.CreateIndex(
                name: "seed_id_foreign_key",
                table: "characters_farming_patches",
                column: "seed_id");

            migrationBuilder.CreateIndex(
                name: "master_id_foreign_key_15",
                table: "characters_items",
                column: "master_id");

            migrationBuilder.CreateIndex(
                name: "LOOK_MASTER_ID_FOREIGN",
                table: "characters_look",
                column: "master_id");

            migrationBuilder.CreateIndex(
                name: "master_id_foreign_key_5",
                table: "characters_offences",
                column: "master_id");

            migrationBuilder.CreateIndex(
                name: "moderator_id_foreign_key",
                table: "characters_offences",
                column: "moderator_id");

            migrationBuilder.CreateIndex(
                name: "master_foreign_key_24",
                table: "characters_profiles",
                column: "master_id");

            migrationBuilder.CreateIndex(
                name: "quest_id_foreign_key",
                table: "characters_quests",
                column: "quest_id");

            migrationBuilder.CreateIndex(
                name: "reported_id_foreign_key",
                table: "characters_reports",
                column: "reported_id");

            migrationBuilder.CreateIndex(
                name: "reporter_id_foreign_key",
                table: "characters_reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "master_id_foreign_key_10",
                table: "characters_rewards",
                column: "master_id");

            migrationBuilder.CreateIndex(
                name: "slayer_task_id_foreign_key",
                table: "characters_slayer_tasks",
                column: "slayer_task_id");

            migrationBuilder.CreateIndex(
                name: "master_id_foreign_key_18",
                table: "clans_bans",
                column: "master_id");

            migrationBuilder.CreateIndex(
                name: "clan_id_foreign_key_46",
                table: "clans_members",
                column: "clan_id");

            migrationBuilder.CreateIndex(
                name: "recruiter_id_foreign_key",
                table: "clans_members",
                column: "recruiter_id");

            migrationBuilder.CreateIndex(
                name: "gameobject_loot_id_foreign_key",
                table: "gameobject_definitions",
                column: "gameobject_loot_id");

            migrationBuilder.CreateIndex(
                name: "gameobject_loot_id_foreign_key_1",
                table: "gameobject_loot_items",
                column: "gameobject_loot_id");

            migrationBuilder.CreateIndex(
                name: "gameobject_loot_parent_id_foreign_key",
                table: "gameobject_lootrecursion",
                column: "gameobject_loot_parent_id");

            migrationBuilder.CreateIndex(
                name: "item_loot_id_foreign_key",
                table: "item_loot_items",
                column: "item_loot_id");

            migrationBuilder.CreateIndex(
                name: "item_loot_table_parent_id_foreign_key",
                table: "item_lootrecursion",
                column: "item_loot_parent_id");

            migrationBuilder.CreateIndex(
                name: "current_wave_id_foreign_key",
                table: "minigames_tzhaar_cave",
                column: "current_wave_id");

            migrationBuilder.CreateIndex(
                name: "wave_id",
                table: "minigames_tzhaar_cave_waves",
                column: "wave_id");

            migrationBuilder.CreateIndex(
                name: "npc_loot_id_foreign_key",
                table: "npc_definitions",
                column: "npc_loot_id");

            migrationBuilder.CreateIndex(
                name: "npc_pickpocket_loot_id_foreign_key",
                table: "npc_definitions",
                column: "npc_pickpocketing_loot_id");

            migrationBuilder.CreateIndex(
                name: "npc_loot_id_foreign_key_1",
                table: "npc_loot_items",
                column: "npc_loot_id");

            migrationBuilder.CreateIndex(
                name: "npc_loot_parent_foreign_key",
                table: "npc_lootrecursion",
                column: "npc_loot_parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictApplications_ClientId",
                table: "OpenIddictApplications",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type",
                table: "OpenIddictAuthorizations",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictScopes_Name",
                table: "OpenIddictScopes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type",
                table: "OpenIddictTokens",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_AuthorizationId",
                table: "OpenIddictTokens",
                column: "AuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_ReferenceId",
                table: "OpenIddictTokens",
                column: "ReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "spot_id_foreign_key",
                table: "skills_fishing_fish_definitions",
                column: "spot_id");

            migrationBuilder.CreateIndex(
                name: "tool_id_foreign_key",
                table: "skills_fishing_spot_definitions",
                column: "tool_id");

            migrationBuilder.CreateIndex(
                name: "child_id_foreign_key",
                table: "skills_magic_enchant_products",
                column: "button_id");

            migrationBuilder.CreateIndex(
                name: "ore_foreign_key",
                table: "skills_mining_rock_definitions",
                column: "ore_id");

            migrationBuilder.CreateIndex(
                name: "slayer_master_foreign_key",
                table: "skills_slayer_task_definitions",
                column: "slayer_master_id");

            migrationBuilder.CreateIndex(
                name: "skills_woodcutting_log_id",
                table: "skills_woodcutting_tree_definitions",
                column: "log_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__efmigrationshistory");

            migrationBuilder.DropTable(
                name: "areas");

            migrationBuilder.DropTable(
                name: "AspnetroleCharacter");

            migrationBuilder.DropTable(
                name: "aspnetroleclaims");

            migrationBuilder.DropTable(
                name: "aspnetuserclaims");

            migrationBuilder.DropTable(
                name: "aspnetuserlogins");

            migrationBuilder.DropTable(
                name: "aspnetuserroles");

            migrationBuilder.DropTable(
                name: "aspnetusertokens");

            migrationBuilder.DropTable(
                name: "blacklist");

            migrationBuilder.DropTable(
                name: "characters_contacts");

            migrationBuilder.DropTable(
                name: "characters_familiars");

            migrationBuilder.DropTable(
                name: "characters_farming_patches");

            migrationBuilder.DropTable(
                name: "characters_items");

            migrationBuilder.DropTable(
                name: "characters_items_look");

            migrationBuilder.DropTable(
                name: "characters_look");

            migrationBuilder.DropTable(
                name: "characters_music");

            migrationBuilder.DropTable(
                name: "characters_music_playlists");

            migrationBuilder.DropTable(
                name: "characters_notes");

            migrationBuilder.DropTable(
                name: "characters_offences");

            migrationBuilder.DropTable(
                name: "characters_permissions");

            migrationBuilder.DropTable(
                name: "characters_preferences");

            migrationBuilder.DropTable(
                name: "characters_profiles");

            migrationBuilder.DropTable(
                name: "characters_quests");

            migrationBuilder.DropTable(
                name: "characters_reports");

            migrationBuilder.DropTable(
                name: "characters_rewards");

            migrationBuilder.DropTable(
                name: "characters_slayer_tasks");

            migrationBuilder.DropTable(
                name: "characters_states");

            migrationBuilder.DropTable(
                name: "characters_statistics");

            migrationBuilder.DropTable(
                name: "characters_tickets");

            migrationBuilder.DropTable(
                name: "characters_variables");

            migrationBuilder.DropTable(
                name: "characterscreateinfo_items");

            migrationBuilder.DropTable(
                name: "clans_bans");

            migrationBuilder.DropTable(
                name: "clans_members");

            migrationBuilder.DropTable(
                name: "clans_settings");

            migrationBuilder.DropTable(
                name: "configurations");

            migrationBuilder.DropTable(
                name: "equipment_bonuses");

            migrationBuilder.DropTable(
                name: "equipment_definitions");

            migrationBuilder.DropTable(
                name: "game_events");

            migrationBuilder.DropTable(
                name: "gameobject_lodestones");

            migrationBuilder.DropTable(
                name: "gameobject_loot_items");

            migrationBuilder.DropTable(
                name: "gameobject_lootrecursion");

            migrationBuilder.DropTable(
                name: "gameobject_spawns");

            migrationBuilder.DropTable(
                name: "item_combines");

            migrationBuilder.DropTable(
                name: "item_definitions");

            migrationBuilder.DropTable(
                name: "item_loot_items");

            migrationBuilder.DropTable(
                name: "item_lootrecursion");

            migrationBuilder.DropTable(
                name: "item_spawns");

            migrationBuilder.DropTable(
                name: "logs_activities");

            migrationBuilder.DropTable(
                name: "logs_chat");

            migrationBuilder.DropTable(
                name: "logs_connections");

            migrationBuilder.DropTable(
                name: "logs_display_name_changes");

            migrationBuilder.DropTable(
                name: "logs_login_attempts");

            migrationBuilder.DropTable(
                name: "minigames_barrows");

            migrationBuilder.DropTable(
                name: "minigames_duel_arena");

            migrationBuilder.DropTable(
                name: "minigames_godwars");

            migrationBuilder.DropTable(
                name: "minigames_tzhaar_cave");

            migrationBuilder.DropTable(
                name: "minigames_tzhaar_cave_waves");

            migrationBuilder.DropTable(
                name: "music_locations");

            migrationBuilder.DropTable(
                name: "npc_bonuses");

            migrationBuilder.DropTable(
                name: "npc_loot_items");

            migrationBuilder.DropTable(
                name: "npc_lootrecursion");

            migrationBuilder.DropTable(
                name: "npc_spawns");

            migrationBuilder.DropTable(
                name: "npc_statistics");

            migrationBuilder.DropTable(
                name: "OpenIddictScopes");

            migrationBuilder.DropTable(
                name: "OpenIddictTokens");

            migrationBuilder.DropTable(
                name: "profanity_words");

            migrationBuilder.DropTable(
                name: "reserved_names");

            migrationBuilder.DropTable(
                name: "shops");

            migrationBuilder.DropTable(
                name: "skills_cooking_food_definitions");

            migrationBuilder.DropTable(
                name: "skills_cooking_raw_food_definitions");

            migrationBuilder.DropTable(
                name: "skills_crafting_gem_definitions");

            migrationBuilder.DropTable(
                name: "skills_crafting_jewelry_definitions");

            migrationBuilder.DropTable(
                name: "skills_crafting_leather_definitions");

            migrationBuilder.DropTable(
                name: "skills_crafting_pottery_definitions");

            migrationBuilder.DropTable(
                name: "skills_crafting_silver_definitions");

            migrationBuilder.DropTable(
                name: "skills_crafting_spin_definitions");

            migrationBuilder.DropTable(
                name: "skills_crafting_tan_definitions");

            migrationBuilder.DropTable(
                name: "skills_firemaking_definitions");

            migrationBuilder.DropTable(
                name: "skills_fishing_fish_definitions");

            migrationBuilder.DropTable(
                name: "skills_fishing_spot_npc_definitions");

            migrationBuilder.DropTable(
                name: "skills_fletching_definitions");

            migrationBuilder.DropTable(
                name: "skills_herblore_herb_definitions");

            migrationBuilder.DropTable(
                name: "skills_magic_combat_definitions");

            migrationBuilder.DropTable(
                name: "skills_magic_enchant_products");

            migrationBuilder.DropTable(
                name: "skills_magic_teleport_definitions");

            migrationBuilder.DropTable(
                name: "skills_mining_pickaxe_definitions");

            migrationBuilder.DropTable(
                name: "skills_mining_rock_definitions");

            migrationBuilder.DropTable(
                name: "skills_prayer_definitions");

            migrationBuilder.DropTable(
                name: "skills_runecrafting_definitions");

            migrationBuilder.DropTable(
                name: "skills_summoning_definitions");

            migrationBuilder.DropTable(
                name: "skills_woodcutting_hatchet_definitions");

            migrationBuilder.DropTable(
                name: "skills_woodcutting_tree_definitions");

            migrationBuilder.DropTable(
                name: "world_configurations");

            migrationBuilder.DropTable(
                name: "worlds");

            migrationBuilder.DropTable(
                name: "aspnetroles");

            migrationBuilder.DropTable(
                name: "characters_appeals");

            migrationBuilder.DropTable(
                name: "npc_definitions");

            migrationBuilder.DropTable(
                name: "skills_farming_patch_definitions");

            migrationBuilder.DropTable(
                name: "skills_farming_seed_definitions");

            migrationBuilder.DropTable(
                name: "quests");

            migrationBuilder.DropTable(
                name: "skills_slayer_task_definitions");

            migrationBuilder.DropTable(
                name: "clans");

            migrationBuilder.DropTable(
                name: "item_loot");

            migrationBuilder.DropTable(
                name: "music_definitions");

            migrationBuilder.DropTable(
                name: "OpenIddictAuthorizations");

            migrationBuilder.DropTable(
                name: "skills_fishing_spot_definitions");

            migrationBuilder.DropTable(
                name: "skills_magic_enchant_definitions");

            migrationBuilder.DropTable(
                name: "skills_mining_ore_definitions");

            migrationBuilder.DropTable(
                name: "skills_woodcutting_log_definitions");

            migrationBuilder.DropTable(
                name: "gameobject_definitions");

            migrationBuilder.DropTable(
                name: "characters");

            migrationBuilder.DropTable(
                name: "npc_loot");

            migrationBuilder.DropTable(
                name: "skills_slayer_master_definitions");

            migrationBuilder.DropTable(
                name: "OpenIddictApplications");

            migrationBuilder.DropTable(
                name: "skills_fishing_tool_definitions");

            migrationBuilder.DropTable(
                name: "gameobject_loot");
        }
    }
}
