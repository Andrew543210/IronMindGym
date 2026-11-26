using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkoutSessions_DurationMinutes",
                table: "WorkoutSessions",
                sql: "\"DurationMinutes\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkoutReviews_Rating",
                table: "WorkoutReviews",
                sql: "\"Rating\" BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkoutPlans_WorkoutsPerWeek",
                table: "WorkoutPlans",
                sql: "\"WorkoutsPerWeek\" BETWEEN 1 AND 7");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WeightLogs_ValueKg",
                table: "WeightLogs",
                sql: "\"ValueKg\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sets_Intensity",
                table: "Sets",
                sql: "\"Intensity\" BETWEEN 0 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sets_SetNumber",
                table: "Sets",
                sql: "\"SetNumber\" >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sets_Values",
                table: "Sets",
                sql: "\"WeightKg\" >= 0 AND \"Reps\" >= 0 AND \"DurationSeconds\" >= 0 AND \"RestSeconds\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Profiles_Age",
                table: "Profiles",
                sql: "\"Age\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Profiles_EnergyEquation",
                table: "Profiles",
                sql: "\"EnergyEquation\" IN ('None', 'Male', 'Female')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Profiles_Targets",
                table: "Profiles",
                sql: "\"CalorieTarget\" > 0 AND \"ProteinTarget\" > 0 AND \"FatTarget\" > 0 AND \"CarbsTarget\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PersonalRecords_MaxWeightKg",
                table: "PersonalRecords",
                sql: "\"MaxWeightKg\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InitialBenchmarks_Loads",
                table: "InitialBenchmarks",
                sql: "\"BenchPressKg\" >= 0 AND \"SquatKg\" >= 0 AND \"DeadliftKg\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExerciseProgress_Intensity",
                table: "ExerciseProgress",
                sql: "\"InitialIntensity\" BETWEEN 0 AND 10 AND \"CurrentIntensity\" BETWEEN 0 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExerciseProgress_Values",
                table: "ExerciseProgress",
                sql: "\"InitialWeightKg\" >= 0 AND \"InitialReps\" >= 0 AND \"InitialDurationSeconds\" >= 0 AND \"CurrentWeightKg\" >= 0 AND \"CurrentReps\" >= 0 AND \"CurrentDurationSeconds\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DailyNutritions_Intake",
                table: "DailyNutritions",
                sql: "\"CaloriesKcal\" >= 0 AND \"ProteinG\" >= 0 AND \"FatG\" >= 0 AND \"CarbsG\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Avatars_MinStrengthCoefficient",
                table: "Avatars",
                sql: "\"MinStrengthCoefficient\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkoutSessions_DurationMinutes",
                table: "WorkoutSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkoutReviews_Rating",
                table: "WorkoutReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkoutPlans_WorkoutsPerWeek",
                table: "WorkoutPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WeightLogs_ValueKg",
                table: "WeightLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sets_Intensity",
                table: "Sets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sets_SetNumber",
                table: "Sets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sets_Values",
                table: "Sets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Profiles_Age",
                table: "Profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Profiles_EnergyEquation",
                table: "Profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Profiles_Targets",
                table: "Profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PersonalRecords_MaxWeightKg",
                table: "PersonalRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InitialBenchmarks_Loads",
                table: "InitialBenchmarks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExerciseProgress_Intensity",
                table: "ExerciseProgress");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExerciseProgress_Values",
                table: "ExerciseProgress");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DailyNutritions_Intake",
                table: "DailyNutritions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Avatars_MinStrengthCoefficient",
                table: "Avatars");
        }
    }
}
