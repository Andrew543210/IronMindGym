using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestrictReferenceDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseProgress_Exercises_ExerciseId",
                table: "ExerciseProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_MuscleSubGroups_MuscleSubGroupId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_MuscleSubGroups_MuscleGroups_MuscleGroupId",
                table: "MuscleSubGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_PerformedExercises_Exercises_ExerciseId",
                table: "PerformedExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalRecords_Exercises_ExerciseId",
                table: "PersonalRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Sets_SetTypes_SetTypeId",
                table: "Sets");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_SplitTypes_SplitTypeId",
                table: "WorkoutPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_SplitTypes_SplitTypeId",
                table: "WorkoutSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseProgress_Exercises_ExerciseId",
                table: "ExerciseProgress",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_MuscleSubGroups_MuscleSubGroupId",
                table: "Exercises",
                column: "MuscleSubGroupId",
                principalTable: "MuscleSubGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MuscleSubGroups_MuscleGroups_MuscleGroupId",
                table: "MuscleSubGroups",
                column: "MuscleGroupId",
                principalTable: "MuscleGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformedExercises_Exercises_ExerciseId",
                table: "PerformedExercises",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalRecords_Exercises_ExerciseId",
                table: "PersonalRecords",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sets_SetTypes_SetTypeId",
                table: "Sets",
                column: "SetTypeId",
                principalTable: "SetTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId",
                principalTable: "Achievements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlans_SplitTypes_SplitTypeId",
                table: "WorkoutPlans",
                column: "SplitTypeId",
                principalTable: "SplitTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_SplitTypes_SplitTypeId",
                table: "WorkoutSessions",
                column: "SplitTypeId",
                principalTable: "SplitTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseProgress_Exercises_ExerciseId",
                table: "ExerciseProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_MuscleSubGroups_MuscleSubGroupId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_MuscleSubGroups_MuscleGroups_MuscleGroupId",
                table: "MuscleSubGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_PerformedExercises_Exercises_ExerciseId",
                table: "PerformedExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalRecords_Exercises_ExerciseId",
                table: "PersonalRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Sets_SetTypes_SetTypeId",
                table: "Sets");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_SplitTypes_SplitTypeId",
                table: "WorkoutPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_SplitTypes_SplitTypeId",
                table: "WorkoutSessions");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseProgress_Exercises_ExerciseId",
                table: "ExerciseProgress",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_MuscleSubGroups_MuscleSubGroupId",
                table: "Exercises",
                column: "MuscleSubGroupId",
                principalTable: "MuscleSubGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MuscleSubGroups_MuscleGroups_MuscleGroupId",
                table: "MuscleSubGroups",
                column: "MuscleGroupId",
                principalTable: "MuscleGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PerformedExercises_Exercises_ExerciseId",
                table: "PerformedExercises",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalRecords_Exercises_ExerciseId",
                table: "PersonalRecords",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sets_SetTypes_SetTypeId",
                table: "Sets",
                column: "SetTypeId",
                principalTable: "SetTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievements_Achievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId",
                principalTable: "Achievements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlans_SplitTypes_SplitTypeId",
                table: "WorkoutPlans",
                column: "SplitTypeId",
                principalTable: "SplitTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_SplitTypes_SplitTypeId",
                table: "WorkoutSessions",
                column: "SplitTypeId",
                principalTable: "SplitTypes",
                principalColumn: "Id");
        }
    }
}
