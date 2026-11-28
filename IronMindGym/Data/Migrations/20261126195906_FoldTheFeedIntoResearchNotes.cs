using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    public partial class FoldTheFeedIntoResearchNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PublishedAt",
                table: "ResearchItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "ResearchItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "ResearchItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            // "Name|https://…" splits into its two parts, and the feed row gives the publication date. A note
            // without an https link is removed; the notes are content, and startup adds the current ones again.
            migrationBuilder.Sql("""
                UPDATE "ResearchItems" item SET
                    "PublishedAt" = COALESCE(
                        (SELECT min(feed."PublishedAt" AT TIME ZONE 'UTC')::date FROM "ActivityFeeds" feed WHERE feed."ResearchItemId" = item."Id"),
                        CURRENT_DATE),
                    "SourceName" = CASE WHEN position('|' IN item."Source") > 0
                        THEN COALESCE(NULLIF(btrim(split_part(item."Source", '|', 1)), ''), 'Research source')
                        ELSE 'Research source' END,
                    "SourceUrl" = CASE WHEN position('|' IN item."Source") > 0
                        THEN btrim(split_part(item."Source", '|', 2))
                        ELSE btrim(item."Source") END;
                DELETE FROM "ResearchItems" WHERE "SourceUrl" NOT LIKE 'https://%';
                ALTER TABLE "ResearchItems" ALTER COLUMN "PublishedAt" SET NOT NULL;
                ALTER TABLE "ResearchItems" ALTER COLUMN "SourceName" SET NOT NULL;
                ALTER TABLE "ResearchItems" ALTER COLUMN "SourceUrl" SET NOT NULL;
                """);

            migrationBuilder.DropTable(
                name: "ActivityFeeds");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ResearchItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ResearchItems_SourceUrl",
                table: "ResearchItems",
                sql: "\"SourceUrl\" LIKE 'https://%'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ResearchItems_SourceUrl",
                table: "ResearchItems");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ResearchItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ActivityFeeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResearchItemId = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityFeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityFeeds_ResearchItems_ResearchItemId",
                        column: x => x.ResearchItemId,
                        principalTable: "ResearchItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFeeds_ResearchItemId",
                table: "ActivityFeeds",
                column: "ResearchItemId");

            migrationBuilder.Sql("""
                UPDATE "ResearchItems" SET "Source" = left("SourceName" || '|' || "SourceUrl", 300);
                ALTER TABLE "ResearchItems" ALTER COLUMN "Source" DROP DEFAULT;
                INSERT INTO "ActivityFeeds" ("ResearchItemId", "PublishedAt")
                SELECT "Id", "PublishedAt"::timestamp AT TIME ZONE 'UTC' FROM "ResearchItems";
                """);

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "ResearchItems");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "ResearchItems");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "ResearchItems");
        }
    }
}
