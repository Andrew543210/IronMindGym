using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    public partial class CheckExerciseOrderAndKeyLift : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_PerformedExercises_OrderInWorkout",
                table: "PerformedExercises",
                sql: "\"OrderInWorkout\" >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Exercises_KeyLift",
                table: "Exercises",
                sql: "\"KeyLift\" IN ('BenchPress', 'Squat', 'Deadlift')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PerformedExercises_OrderInWorkout",
                table: "PerformedExercises");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Exercises_KeyLift",
                table: "Exercises");
        }
    }
}
