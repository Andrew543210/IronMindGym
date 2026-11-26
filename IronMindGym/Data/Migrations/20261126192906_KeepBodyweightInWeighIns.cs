using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    /// <inheritdoc />
    public partial class KeepBodyweightInWeighIns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "HeightCm",
                table: "Profiles",
                type: "double precision",
                nullable: true);

            // The latest measurement gives the height. Each measurement becomes a weigh-in on its day in the
            // athlete's time zone (UTC when the zone is unknown), the latest of a day winning; a weigh-in
            // already logged for that day is kept.
            migrationBuilder.Sql("""
                INSERT INTO "Profiles" ("UserId", "Age", "AccountCreatedAt", "EnergyEquation")
                SELECT DISTINCT a."UserId", 0, now(), 'None' FROM "AnthropometryLogs" a
                WHERE NOT EXISTS (SELECT 1 FROM "Profiles" p WHERE p."UserId" = a."UserId");

                UPDATE "Profiles" p SET "HeightCm" = latest."HeightCm"
                FROM (SELECT DISTINCT ON ("UserId") "UserId", "HeightCm" FROM "AnthropometryLogs"
                      WHERE "HeightCm" > 0 ORDER BY "UserId", "RecordedAt" DESC, "Id" DESC) latest
                WHERE p."UserId" = latest."UserId";

                INSERT INTO "WeightLogs" ("UserId", "Date", "ValueKg")
                SELECT DISTINCT ON (measured."UserId", measured."Day") measured."UserId", measured."Day", measured."WeightKg"
                FROM (SELECT a."UserId", a."WeightKg", a."RecordedAt", a."Id",
                             (a."RecordedAt" AT TIME ZONE COALESCE(zone.name, 'UTC'))::date AS "Day"
                      FROM "AnthropometryLogs" a
                      LEFT JOIN "Profiles" p ON p."UserId" = a."UserId"
                      LEFT JOIN pg_timezone_names zone ON zone.name = p."TimeZone"
                      WHERE a."WeightKg" > 0) measured
                WHERE NOT EXISTS (SELECT 1 FROM "WeightLogs" w WHERE w."UserId" = measured."UserId" AND w."Date" = measured."Day")
                ORDER BY measured."UserId", measured."Day", measured."RecordedAt" DESC, measured."Id" DESC;
                """);

            migrationBuilder.DropTable(
                name: "AnthropometryLogs");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Profiles_HeightCm",
                table: "Profiles",
                sql: "\"HeightCm\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Profiles_HeightCm",
                table: "Profiles");

            migrationBuilder.CreateTable(
                name: "AnthropometryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    HeightCm = table.Column<double>(type: "double precision", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnthropometryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnthropometryLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnthropometryLogs_UserId",
                table: "AnthropometryLogs",
                column: "UserId");

            // Every weigh-in becomes a measurement at noon UTC of its day, with the profile's height.
            migrationBuilder.Sql("""
                INSERT INTO "AnthropometryLogs" ("UserId", "WeightKg", "HeightCm", "RecordedAt")
                SELECT w."UserId", w."ValueKg", p."HeightCm", (w."Date" + time '12:00') AT TIME ZONE 'UTC'
                FROM "WeightLogs" w JOIN "Profiles" p ON p."UserId" = w."UserId"
                WHERE p."HeightCm" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Profiles");
        }
    }
}
