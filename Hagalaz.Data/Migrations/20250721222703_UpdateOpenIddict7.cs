using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hagalaz.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOpenIddict7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL limits utf8mb4 index keys to 3072 bytes. Drop and recreate
            // the composite index with explicit prefixes while widening Type;
            // otherwise MySQL rebuilds the existing full-width index and fails.
            migrationBuilder.Sql(
                "CREATE INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type_Migration` " +
                "ON `OpenIddictTokens` (`ApplicationId`, `Status`(50), `Subject`(191), `Type`(50));");
            migrationBuilder.Sql(
                "DROP INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type` ON `OpenIddictTokens`;" );

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "OpenIddictTokens",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySQL:Charset", "utf8mb4")
                .OldAnnotation("MySQL:Charset", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.Sql(
                "CREATE INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type` " +
                "ON `OpenIddictTokens` (`ApplicationId`, `Status`(50), `Subject`(191), `Type`(150));");
            migrationBuilder.Sql(
                "DROP INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type_Migration` ON `OpenIddictTokens`;" );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type_Migration` " +
                "ON `OpenIddictTokens` (`ApplicationId`, `Status`(50), `Subject`(191), `Type`(150));");
            migrationBuilder.Sql(
                "DROP INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type` ON `OpenIddictTokens`;" );

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "OpenIddictTokens",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8mb4_0900_ai_ci",
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150,
                oldNullable: true)
                .Annotation("MySQL:Charset", "utf8mb4")
                .OldAnnotation("MySQL:Charset", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_0900_ai_ci");

            migrationBuilder.Sql(
                "CREATE INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type` " +
                "ON `OpenIddictTokens` (`ApplicationId`, `Status`(50), `Subject`(191), `Type`(50));");
            migrationBuilder.Sql(
                "DROP INDEX `IX_OpenIddictTokens_ApplicationId_Status_Subject_Type_Migration` ON `OpenIddictTokens`;" );
        }
    }
}
