using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    public partial class NameColumnsAfterWhatTheyHold : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PerformedExercises_ExerciseId",
                table: "PerformedExercises");

            migrationBuilder.RenameColumn(
                name: "GoalType",
                table: "UserGoals",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PersonalRecords",
                newName: "AchievedAt");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "FriendComments",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.Sql("""
                UPDATE "Users" account SET "CreatedAt" = profile."AccountCreatedAt"
                FROM "Profiles" profile WHERE profile."UserId" = account."Id";
                ALTER TABLE "Users" ALTER COLUMN "CreatedAt" DROP DEFAULT;
                """);

            migrationBuilder.DropColumn(
                name: "AccountCreatedAt",
                table: "Profiles");
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_EmailLength",
                table: "Users",
                sql: "char_length(\"Email\") <= 120");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_UsernameLength",
                table: "Users",
                sql: "char_length(\"Username\") <= 50");

            migrationBuilder.CreateIndex(
                name: "IX_PerformedExercises_ExerciseId_WorkoutSessionId",
                table: "PerformedExercises",
                columns: new[] { "ExerciseId", "WorkoutSessionId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_EmailLength",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_UsernameLength",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_PerformedExercises_ExerciseId_WorkoutSessionId",
                table: "PerformedExercises");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "UserGoals",
                newName: "GoalType");

            migrationBuilder.RenameColumn(
                name: "AchievedAt",
                table: "PersonalRecords",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "FriendComments",
                newName: "Date");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountCreatedAt",
                table: "Profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.Sql("""
                UPDATE "Profiles" profile SET "AccountCreatedAt" = account."CreatedAt"
                FROM "Users" account WHERE account."Id" = profile."UserId";
                ALTER TABLE "Profiles" ALTER COLUMN "AccountCreatedAt" DROP DEFAULT;
                """);

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");
            migrationBuilder.CreateIndex(
                name: "IX_PerformedExercises_ExerciseId",
                table: "PerformedExercises",
                column: "ExerciseId");
        }
    }
}
