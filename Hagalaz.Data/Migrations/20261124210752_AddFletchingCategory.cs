using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFletchingCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ushort>(
                name: "animation_id",
                table: "skills_fletching_definitions",
                type: "smallint(6) unsigned",
                nullable: true,
                oldClrType: typeof(ushort),
                oldType: "smallint(6) unsigned");

            migrationBuilder.AddColumn<byte>(
                name: "category",
                table: "skills_fletching_definitions",
                type: "tinyint(3) unsigned",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "skills_fletching_definitions");

            migrationBuilder.AlterColumn<ushort>(
                name: "animation_id",
                table: "skills_fletching_definitions",
                type: "smallint(6) unsigned",
                nullable: false,
                defaultValue: (ushort)0,
                oldClrType: typeof(ushort),
                oldType: "smallint(6) unsigned",
                oldNullable: true);
        }
    }
}
