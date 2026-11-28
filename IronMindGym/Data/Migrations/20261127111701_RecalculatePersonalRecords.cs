using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    // Rebuilds every record from the logged sets: the heaviest strength set of an exercise, dated by the first workout that reached it.
    public partial class RecalculatePersonalRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "PersonalRecords";
                INSERT INTO "PersonalRecords" ("UserId", "ExerciseId", "MaxWeightKg", "AchievedAt")
                SELECT DISTINCT ON (session."UserId", performed."ExerciseId")
                    session."UserId", performed."ExerciseId", sets."WeightKg", session."SessionDate"
                FROM "Sets" sets
                JOIN "PerformedExercises" performed ON performed."Id" = sets."PerformedExerciseId"
                JOIN "WorkoutSessions" session ON session."Id" = performed."WorkoutSessionId"
                JOIN "Exercises" exercise ON exercise."Id" = performed."ExerciseId"
                WHERE exercise."Type" = 0 AND sets."WeightKg" > 0
                ORDER BY session."UserId", performed."ExerciseId", sets."WeightKg" DESC, session."SessionDate";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The rebuilt records follow from the sets, so there is nothing to restore.
        }
    }
}
