using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    public partial class CheckTheInterfaceLanguage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Profiles" SET "Language" = NULL WHERE "Language" NOT IN ('en', 'uk')""");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Profiles_Language",
                table: "Profiles",
                sql: "\"Language\" IN ('en', 'uk')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Profiles_Language",
                table: "Profiles");
        }
    }
}
