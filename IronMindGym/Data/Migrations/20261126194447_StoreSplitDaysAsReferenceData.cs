using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreSplitDaysAsReferenceData : Migration
    {
        // The days of the seeded splits; startup keeps their names current afterwards.
        private const string SeededDays = """
            (VALUES
                ('FULL_BODY', 'FULL_BODY', 1, 'Full Body', 'Усе тіло', 'Full Body day', 'День на все тіло'),
                ('UPPER_LOWER', 'UPPER', 1, 'Upper', 'Верх тіла', 'Upper day', 'День верху тіла'),
                ('UPPER_LOWER', 'LOWER', 2, 'Lower', 'Низ тіла', 'Lower day', 'День низу тіла'),
                ('LEGS_BACK_CHEST', 'LEGS', 1, 'Legs', 'Ноги', 'Legs day', 'День ніг'),
                ('LEGS_BACK_CHEST', 'BACK', 2, 'Back', 'Спина', 'Back day', 'День спини'),
                ('LEGS_BACK_CHEST', 'CHEST', 3, 'Chest', 'Груди', 'Chest day', 'День грудей'))
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SplitDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SplitTypeId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    NameUk = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    WorkoutName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    WorkoutNameUk = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SplitDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SplitDays_SplitTypes_SplitTypeId",
                        column: x => x.SplitTypeId,
                        principalTable: "SplitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SplitDays_Code",
                table: "SplitDays",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SplitDays_SplitTypeId_Position",
                table: "SplitDays",
                columns: new[] { "SplitTypeId", "Position" },
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitDayId",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            // A saved title such as "Legs day" names its split day; any other title was a general training day.
            migrationBuilder.Sql($"""
                INSERT INTO "SplitDays" ("SplitTypeId", "Code", "Position", "Name", "NameUk", "WorkoutName", "WorkoutNameUk")
                SELECT split."Id", day.code, day.position, day.name, day.name_uk, day.workout_name, day.workout_name_uk
                FROM {SeededDays} AS day (split, code, position, name, name_uk, workout_name, workout_name_uk)
                JOIN "SplitTypes" split ON split."Code" = day.split
                ORDER BY split."Id", day.position;

                UPDATE "WorkoutSessions" session SET "SplitDayId" = day."Id"
                FROM "SplitDays" day WHERE lower(day."WorkoutName") = lower(trim(session."Name"));
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_SplitTypes_SplitTypeId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_SplitTypeId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "SplitTypeId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "WorkoutSessions");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_SplitDayId",
                table: "WorkoutSessions",
                column: "SplitDayId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_SplitDays_SplitDayId",
                table: "WorkoutSessions",
                column: "SplitDayId",
                principalTable: "SplitDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WorkoutSessions",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Training Day");

            migrationBuilder.AddColumn<int>(
                name: "SplitTypeId",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "WorkoutSessions" session SET "Name" = day."WorkoutName", "SplitTypeId" = day."SplitTypeId"
                FROM "SplitDays" day WHERE day."Id" = session."SplitDayId";
                ALTER TABLE "WorkoutSessions" ALTER COLUMN "Name" DROP DEFAULT;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_SplitDays_SplitDayId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_SplitDayId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "SplitDayId",
                table: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "SplitDays");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_SplitTypeId",
                table: "WorkoutSessions",
                column: "SplitTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_SplitTypes_SplitTypeId",
                table: "WorkoutSessions",
                column: "SplitTypeId",
                principalTable: "SplitTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
