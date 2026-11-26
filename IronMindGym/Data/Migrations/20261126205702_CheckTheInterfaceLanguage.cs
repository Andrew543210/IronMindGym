using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    /// <inheritdoc />
    public partial class CheckTheInterfaceLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // An unknown code means no choice yet; the client then follows the browser.
            migrationBuilder.Sql("""UPDATE "Profiles" SET "Language" = NULL WHERE "Language" NOT IN ('en', 'uk')""");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Profiles_Language",
                table: "Profiles",
                sql: "\"Language\" IN ('en', 'uk')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Profiles_Language",
                table: "Profiles");
        }
    }
}
