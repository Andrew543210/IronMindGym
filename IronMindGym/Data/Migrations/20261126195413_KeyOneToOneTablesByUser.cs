using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    public partial class KeyOneToOneTablesByUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutSessions",
                type: "character varying(800)",
                maxLength: 800,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            // The review moves into its workout, keeping blank comments out. Of several plans an athlete keeps
            // the active one, or the latest when none is active.
            migrationBuilder.Sql("""
                UPDATE "WorkoutSessions" session
                SET "Rating" = review."Rating", "Notes" = NULLIF(btrim(review."Comment"), '')
                FROM "WorkoutReviews" review WHERE review."WorkoutSessionId" = session."Id";

                DELETE FROM "WorkoutPlans" plan
                WHERE plan."Id" <> (SELECT kept."Id" FROM "WorkoutPlans" kept WHERE kept."UserId" = plan."UserId"
                                    ORDER BY kept."IsActive" DESC, kept."Id" DESC LIMIT 1);
                """);

            migrationBuilder.DropTable(
                name: "WorkoutReviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkoutPlans",
                table: "WorkoutPlans");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlans_UserId",
                table: "WorkoutPlans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Profiles",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InitialBenchmarks",
                table: "InitialBenchmarks");

            migrationBuilder.DropIndex(
                name: "IX_InitialBenchmarks_UserId",
                table: "InitialBenchmarks");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "InitialBenchmarks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkoutPlans",
                table: "WorkoutPlans",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profiles",
                table: "Profiles",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InitialBenchmarks",
                table: "InitialBenchmarks",
                column: "UserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkoutSessions_Rating",
                table: "WorkoutSessions",
                sql: "\"Rating\" BETWEEN 1 AND 5");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkoutSessions_Rating",
                table: "WorkoutSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkoutPlans",
                table: "WorkoutPlans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Profiles",
                table: "Profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InitialBenchmarks",
                table: "InitialBenchmarks");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "WorkoutPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "WorkoutPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "InitialBenchmarks",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkoutPlans",
                table: "WorkoutPlans",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profiles",
                table: "Profiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InitialBenchmarks",
                table: "InitialBenchmarks",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "WorkoutReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkoutSessionId = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutReviews", x => x.Id);
                    table.CheckConstraint("CK_WorkoutReviews_Rating", "\"Rating\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_WorkoutReviews_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_UserId",
                table: "WorkoutPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitialBenchmarks_UserId",
                table: "InitialBenchmarks",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutReviews_WorkoutSessionId",
                table: "WorkoutReviews",
                column: "WorkoutSessionId",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "WorkoutReviews" ("WorkoutSessionId", "Rating", "Comment")
                SELECT "Id", "Rating", COALESCE("Notes", '') FROM "WorkoutSessions" WHERE "Rating" IS NOT NULL;
                UPDATE "WorkoutPlans" SET "IsActive" = true;
                """);

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "WorkoutSessions");
        }
    }
}
