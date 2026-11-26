using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronMindGym.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceFriendshipRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_Users_User1Id",
                table: "Friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_Users_User2Id",
                table: "Friendships");

            // Rows written before these rules existed: drop pairs of an athlete with themselves, keep one row
            // per pair (the accepted one, otherwise the one already in order), store the smaller id first,
            // and credit a missing requester to the first athlete.
            migrationBuilder.Sql("""
                DELETE FROM "Friendships" WHERE "User1Id" = "User2Id";
                DELETE FROM "Friendships" dropped
                USING "Friendships" kept
                WHERE kept."User1Id" = dropped."User2Id" AND kept."User2Id" = dropped."User1Id"
                  AND (kept."Status" = 'Accepted', kept."User1Id" < kept."User2Id")
                    > (dropped."Status" = 'Accepted', dropped."User1Id" < dropped."User2Id");
                UPDATE "Friendships" SET "User1Id" = "User2Id", "User2Id" = "User1Id" WHERE "User1Id" > "User2Id";
                UPDATE "Friendships" SET "RequestedByUserId" = "User1Id" WHERE "RequestedByUserId" NOT IN ("User1Id", "User2Id");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_RequestedByUserId",
                table: "Friendships",
                column: "RequestedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Friendships_OrderedPair",
                table: "Friendships",
                sql: "\"User1Id\" < \"User2Id\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Friendships_RequesterInPair",
                table: "Friendships",
                sql: "\"RequestedByUserId\" IN (\"User1Id\", \"User2Id\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Friendships_Status",
                table: "Friendships",
                sql: "\"Status\" IN ('Pending', 'Accepted')");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_Users_RequestedByUserId",
                table: "Friendships",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_Users_User1Id",
                table: "Friendships",
                column: "User1Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_Users_User2Id",
                table: "Friendships",
                column: "User2Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_Users_RequestedByUserId",
                table: "Friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_Users_User1Id",
                table: "Friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_Users_User2Id",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_RequestedByUserId",
                table: "Friendships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Friendships_OrderedPair",
                table: "Friendships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Friendships_RequesterInPair",
                table: "Friendships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Friendships_Status",
                table: "Friendships");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_Users_User1Id",
                table: "Friendships",
                column: "User1Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_Users_User2Id",
                table: "Friendships",
                column: "User2Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
