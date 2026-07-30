using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vivcord.Server.Migrations
{
    /// <inheritdoc />
    public partial class FileSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "UserMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "UserMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "UserMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "UserMessages");
        }
    }
}
