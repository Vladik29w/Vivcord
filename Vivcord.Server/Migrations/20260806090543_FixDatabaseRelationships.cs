using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vivcord.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixDatabaseRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM RefreshTokens WHERE UserId NOT IN (SELECT Id FROM AspNetUsers);");
            migrationBuilder.Sql("DELETE FROM PrivateMessages WHERE Sender NOT IN (SELECT Id FROM AspNetUsers) OR Target NOT IN (SELECT Id FROM AspNetUsers);");
            migrationBuilder.Sql("DELETE FROM GroupMessages WHERE Sender NOT IN (SELECT Id FROM AspNetUsers);");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_Sender",
                table: "PrivateMessages",
                column: "Sender");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_Target",
                table: "PrivateMessages",
                column: "Target");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMessages_Sender",
                table: "GroupMessages",
                column: "Sender");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMessages_AspNetUsers_Sender",
                table: "GroupMessages",
                column: "Sender",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateMessages_AspNetUsers_Sender",
                table: "PrivateMessages",
                column: "Sender",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateMessages_AspNetUsers_Target",
                table: "PrivateMessages",
                column: "Target",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMessages_AspNetUsers_Sender",
                table: "GroupMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateMessages_AspNetUsers_Sender",
                table: "PrivateMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_PrivateMessages_AspNetUsers_Target",
                table: "PrivateMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AspNetUsers_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_PrivateMessages_Sender",
                table: "PrivateMessages");

            migrationBuilder.DropIndex(
                name: "IX_PrivateMessages_Target",
                table: "PrivateMessages");

            migrationBuilder.DropIndex(
                name: "IX_GroupMessages_Sender",
                table: "GroupMessages");
        }
    }
}
