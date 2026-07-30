using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterSnapshotRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "snapshot_revision",
                table: "characters",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "snapshot_revision",
                table: "characters");
        }
    }
}
