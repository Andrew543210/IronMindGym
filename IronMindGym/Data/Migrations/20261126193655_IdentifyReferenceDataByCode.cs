using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdentifyReferenceDataByCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MuscleSubGroups_MuscleGroupId",
                table: "MuscleSubGroups");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "SplitTypes",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "SetTypes",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KeyLift",
                table: "Exercises",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Rows seeded under the English names get their codes; a row added by hand gets a code from its id.
            migrationBuilder.Sql("""
                UPDATE "SplitTypes" SET "Code" = CASE "Name"
                    WHEN 'Full-body' THEN 'FULL_BODY' WHEN 'Upper-Lower' THEN 'UPPER_LOWER'
                    WHEN 'Legs-Back-Chest' THEN 'LEGS_BACK_CHEST' ELSE 'SPLIT_' || "Id" END;
                UPDATE "SetTypes" SET "Code" = CASE "Name"
                    WHEN 'Working' THEN 'WORKING' WHEN 'Warm-up' THEN 'WARM_UP'
                    WHEN 'Failure' THEN 'FAILURE' ELSE 'SET_' || "Id" END;
                ALTER TABLE "SplitTypes" ALTER COLUMN "Code" DROP DEFAULT;
                ALTER TABLE "SetTypes" ALTER COLUMN "Code" DROP DEFAULT;
                UPDATE "Exercises" SET "KeyLift" = CASE "Name"
                    WHEN 'Flat Bench Press' THEN 'BenchPress' WHEN 'Barbell Back Squat' THEN 'Squat'
                    WHEN 'Deadlift' THEN 'Deadlift' END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SplitTypes_Code",
                table: "SplitTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SplitTypes_Name",
                table: "SplitTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SetTypes_Code",
                table: "SetTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SetTypes_Name",
                table: "SetTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchItems_Title",
                table: "ResearchItems",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuscleSubGroups_MuscleGroupId_Name",
                table: "MuscleSubGroups",
                columns: new[] { "MuscleGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MuscleGroups_Name",
                table: "MuscleGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_KeyLift",
                table: "Exercises",
                column: "KeyLift",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Avatars_Name",
                table: "Avatars",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SplitTypes_Code",
                table: "SplitTypes");

            migrationBuilder.DropIndex(
                name: "IX_SplitTypes_Name",
                table: "SplitTypes");

            migrationBuilder.DropIndex(
                name: "IX_SetTypes_Code",
                table: "SetTypes");

            migrationBuilder.DropIndex(
                name: "IX_SetTypes_Name",
                table: "SetTypes");

            migrationBuilder.DropIndex(
                name: "IX_ResearchItems_Title",
                table: "ResearchItems");

            migrationBuilder.DropIndex(
                name: "IX_MuscleSubGroups_MuscleGroupId_Name",
                table: "MuscleSubGroups");

            migrationBuilder.DropIndex(
                name: "IX_MuscleGroups_Name",
                table: "MuscleGroups");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_KeyLift",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Avatars_Name",
                table: "Avatars");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "SplitTypes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "SetTypes");

            migrationBuilder.DropColumn(
                name: "KeyLift",
                table: "Exercises");

            migrationBuilder.CreateIndex(
                name: "IX_MuscleSubGroups_MuscleGroupId",
                table: "MuscleSubGroups",
                column: "MuscleGroupId");
        }
    }
}
