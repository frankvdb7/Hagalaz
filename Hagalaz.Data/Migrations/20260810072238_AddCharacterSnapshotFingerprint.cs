using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterSnapshotFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "snapshot_fingerprint",
                table: "characters",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "snapshot_fingerprint",
                table: "characters");
        }
    }
}
