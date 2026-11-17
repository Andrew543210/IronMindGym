using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IronMindGym.Data.Migrations;

[DbContext(typeof(IronMindDbContext))]
[Migration("20261117170000_ClarifyExerciseCatalog")]
public class ClarifyExerciseCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Update reference rows in place; historical sets keep their exercise IDs and values.
        migrationBuilder.Sql("""
            UPDATE "Exercises" SET "Description" = 'Biceps curl using an angled bar grip.' WHERE "Name" = 'EZ-Bar Curl';
            UPDATE "Exercises" SET "Description" = 'Elbow flexion with the upper arms supported on a preacher bench.' WHERE "Name" = 'Preacher Curl';
            UPDATE "Exercises" SET "Description" = 'Loaded walking carry. Log duration and the same load convention each session.' WHERE "Name" = 'Farmer''s Walk';
            UPDATE "Exercises" SET "Description" = 'Hang from a bar for a recorded duration. Log added external load only.' WHERE "Name" = 'Timed Dead Hang';
            UPDATE "Exercises" SET "Description" = 'Elbow extension with a dumbbell held overhead; keep the same one-arm or two-arm variation between logs.' WHERE "Name" = 'Overhead Dumbbell Extension';
            UPDATE "Exercises" SET "Description" = 'General back stabilization entry. Record the specific drill in your session notes, or select a named exercise for consistent comparisons.' WHERE "Name" = 'Tilt control and back stabilization';
            UPDATE "Exercises" SET "Description" = 'Push-up with the hands supported above the feet. Keep support height consistent between logs.' WHERE "Name" = 'Incline Push-ups (Hands on bench)';
            UPDATE "Exercises" SET "Description" = 'Seated chest fly using a machine with a guided movement path.' WHERE "Name" = 'Pec Deck Flyes';
            UPDATE "Exercises" SET "Description" = 'Seated pressing movement with a machine-guided path.' WHERE "Name" = 'Seated Machine Chest Press';
            UPDATE "Exercises" SET "Description" = 'Decline pressing movement with a guided bar path.' WHERE "Name" = 'Decline Smith Machine Press';
            UPDATE "Exercises" SET "Description" = 'Chest fly performed on an incline bench.' WHERE "Name" = 'Incline Dumbbell Flyes';
            UPDATE "Exercises" SET "Description" = 'Upward cable fly performed from low pulleys.' WHERE "Name" = 'Low Cable Crossover';
            UPDATE "Exercises" SET "Description" = 'General mobility sequence. Log duration and note the movements in your session review.' WHERE "Name" = 'Comprehensive full-body mobilization';
            UPDATE "Exercises" SET "Description" = 'Push a loaded sled. Log duration and keep distance and surface comparable between sessions.' WHERE "Name" = 'Sled Push';
            UPDATE "Exercises" SET "Description" = 'Repeated rope jumps logged by duration and perceived effort.' WHERE "Name" = 'Jump Rope';
            UPDATE "Exercises" SET "Description" = 'Isometric core stabilization. Log duration; add external load only when used.' WHERE "Name" = 'Weighted Plank';
            UPDATE "Exercises" SET "Description" = 'Hanging leg raise involving hip flexion and trunk control.' WHERE "Name" = 'Hanging Leg Raises';
            UPDATE "Exercises" SET "Description" = 'Supine leg raise involving hip flexion and trunk control.' WHERE "Name" = 'Lying Leg Raise';
            UPDATE "Exercises" SET "Description" = 'Controlled curl of the pelvis toward the rib cage.' WHERE "Name" = 'Reverse Crunch';
            UPDATE "Exercises" SET "Description" = 'Alternating leg movement while controlling the trunk.' WHERE "Name" = 'Scissor Kicks';
            UPDATE "Exercises" SET "Description" = 'Isometric oblique work. Log duration; add external load only when used.' WHERE "Name" = 'Side Plank';
            UPDATE "Exercises" SET "Description" = 'Trunk flexion from a supine position.' WHERE "Name" = 'Floor Crunches';
            UPDATE "Exercises" SET "Description" = 'Statodynamic load. Log duration; add external load only when used.' WHERE "Name" = 'Tip-Toe Farmer''s Walk';
            UPDATE "Exercises" SET "Description" = 'Hip extension with the upper back supported on a bench.' WHERE "Name" = 'Hip Thrust';
            UPDATE "Exercises" SET "Description" = 'Preparation for squats. Log duration; add external load only when used.' WHERE "Name" = 'Hip Mobilization';
            UPDATE "Exercises" SET "Description" = 'Reduced hamstring stiffness. Log duration; add external load only when used.' WHERE "Name" = 'Hamstring Stretch';
            UPDATE "Exercises" SET "Description" = 'Ankle mobility improvement. Log duration; add external load only when used.' WHERE "Name" = 'Wall Calf Stretch';
            UPDATE "Exercises" SET "Description" = 'Shoulder girdle mobility improvement. Log duration; add external load only when used.' WHERE "Name" = 'Thoracic Opening';
            UPDATE "Exercises" SET "Description" = 'Overhead pressing movement with a machine-guided path.' WHERE "Name" = 'Shoulder Press Machine';
            INSERT INTO "MuscleSubGroups" ("Name", "MuscleGroupId")
            SELECT 'General mobility', "Id" FROM "MuscleGroups" WHERE "Name" = 'Mobility'
            AND NOT EXISTS (SELECT 1 FROM "MuscleSubGroups" WHERE "Name" = 'General mobility');
            UPDATE "Exercises" SET "Type" = 2, "MuscleSubGroupId" = (SELECT "Id" FROM "MuscleSubGroups" WHERE "Name" = 'General mobility' LIMIT 1)
            WHERE "Name" = 'Comprehensive full-body mobilization';
            DELETE FROM "PersonalRecords" WHERE "ExerciseId" IN (SELECT "Id" FROM "Exercises" WHERE "Name" = 'Comprehensive full-body mobilization');
            DELETE FROM "PersonalRecords" WHERE "MaxWeightKg" <= 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Exercises" SET "Description" = 'Reduced wrist strain' WHERE "Name" = 'EZ-Bar Curl';
            UPDATE "Exercises" SET "Description" = 'Fixed position for bicep peak' WHERE "Name" = 'Preacher Curl';
            UPDATE "Exercises" SET "Description" = 'Grip strength and static load Log duration; add external load only when used.' WHERE "Name" = 'Farmer''s Walk';
            UPDATE "Exercises" SET "Description" = 'Grip endurance Log duration; add external load only when used.' WHERE "Name" = 'Timed Dead Hang';
            UPDATE "Exercises" SET "Description" = 'Single-arm isolation' WHERE "Name" = 'Overhead Dumbbell Extension';
            UPDATE "Exercises" SET "Description" = 'Tilt control and back stabilization. Track controlled sets and use a comfortable range of motion.' WHERE "Name" = 'Tilt control and back stabilization';
            UPDATE "Exercises" SET "Description" = 'Easier variation for lower chest' WHERE "Name" = 'Incline Push-ups (Hands on bench)';
            UPDATE "Exercises" SET "Description" = 'Chest isolation without heavy shoulder load' WHERE "Name" = 'Pec Deck Flyes';
            UPDATE "Exercises" SET "Description" = 'Stable work in a safe position' WHERE "Name" = 'Seated Machine Chest Press';
            UPDATE "Exercises" SET "Description" = 'Safe mechanics with fixed path' WHERE "Name" = 'Decline Smith Machine Press';
            UPDATE "Exercises" SET "Description" = 'Upper chest isolation' WHERE "Name" = 'Incline Dumbbell Flyes';
            UPDATE "Exercises" SET "Description" = 'Upper chest peak contraction' WHERE "Name" = 'Low Cable Crossover';
            UPDATE "Exercises" SET "Description" = 'Comprehensive full-body mobilization. Track controlled sets and use a comfortable range of motion.' WHERE "Name" = 'Comprehensive full-body mobilization';
            UPDATE "Exercises" SET "Description" = 'Strength endurance Log duration; add external load only when used.' WHERE "Name" = 'Sled Push';
            UPDATE "Exercises" SET "Description" = 'Dynamic calf work' WHERE "Name" = 'Jump Rope';
            UPDATE "Exercises" SET "Description" = 'Isometric core stabilization Log duration; add external load only when used.' WHERE "Name" = 'Weighted Plank';
            UPDATE "Exercises" SET "Description" = 'Strong emphasis on lower abs' WHERE "Name" = 'Hanging Leg Raises';
            UPDATE "Exercises" SET "Description" = 'Basic exercise for lower abs' WHERE "Name" = 'Lying Leg Raise';
            UPDATE "Exercises" SET "Description" = 'Lower abs isolation' WHERE "Name" = 'Reverse Crunch';
            UPDATE "Exercises" SET "Description" = 'Statodynamic lower abs work' WHERE "Name" = 'Scissor Kicks';
            UPDATE "Exercises" SET "Description" = 'Isometric oblique work Log duration; add external load only when used.' WHERE "Name" = 'Side Plank';
            UPDATE "Exercises" SET "Description" = 'Classic upper abs movement' WHERE "Name" = 'Floor Crunches';
            UPDATE "Exercises" SET "Description" = 'Statodynamic load Log duration; add external load only when used.' WHERE "Name" = 'Tip-Toe Farmer''s Walk';
            UPDATE "Exercises" SET "Description" = 'Maximum glute activation' WHERE "Name" = 'Hip Thrust';
            UPDATE "Exercises" SET "Description" = 'Preparation for squats Log duration; add external load only when used.' WHERE "Name" = 'Hip Mobilization';
            UPDATE "Exercises" SET "Description" = 'Reduced hamstring stiffness Log duration; add external load only when used.' WHERE "Name" = 'Hamstring Stretch';
            UPDATE "Exercises" SET "Description" = 'Ankle mobility improvement Log duration; add external load only when used.' WHERE "Name" = 'Wall Calf Stretch';
            UPDATE "Exercises" SET "Description" = 'Shoulder girdle mobility improvement Log duration; add external load only when used.' WHERE "Name" = 'Thoracic Opening';
            UPDATE "Exercises" SET "Description" = 'Safe trajectory control' WHERE "Name" = 'Shoulder Press Machine';
            UPDATE "Exercises" SET "Type" = 1, "MuscleSubGroupId" = (SELECT "Id" FROM "MuscleSubGroups" WHERE "Name" = 'Intervals' LIMIT 1)
            WHERE "Name" = 'Comprehensive full-body mobilization';
            DELETE FROM "MuscleSubGroups" WHERE "Name" = 'General mobility' AND NOT EXISTS (SELECT 1 FROM "Exercises" WHERE "MuscleSubGroupId" = "MuscleSubGroups"."Id");
            """);
    }
}
