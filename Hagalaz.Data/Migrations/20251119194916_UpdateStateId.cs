using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "state_id",
                table: "gameobject_lodestones",
                type: "varchar(255)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(uint),
                oldType: "int(11) unsigned")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "state_id",
                table: "characters_states",
                type: "varchar(255)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:CharSet", "utf8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<uint>(
                name: "state_id",
                table: "gameobject_lodestones",
                type: "int(11) unsigned",
                nullable: false,
                defaultValue: 0u,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8")
                .OldAnnotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.AlterColumn<int>(
                name: "state_id",
                table: "characters_states",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .OldAnnotation("MySql:CharSet", "utf8")
                .OldAnnotation("Relational:Collation", "utf8_general_ci");
        }
    }
}
