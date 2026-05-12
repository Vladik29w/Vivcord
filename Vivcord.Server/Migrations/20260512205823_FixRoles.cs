using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vivcord.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c7b013f0-5201-4317-bcc8-c21ff591658d",
                column: "ConcurrencyStamp",
                value: "2");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "fab4fac1-c546-41de-aebc-a17da9526085",
                column: "ConcurrencyStamp",
                value: "1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c7b013f0-5201-4317-bcc8-c21ff591658d",
                column: "ConcurrencyStamp",
                value: "12c0c362-b1d4-4d33-8e62-0fdb8d473d42");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "fab4fac1-c546-41de-aebc-a17da9526085",
                column: "ConcurrencyStamp",
                value: "db4fdb39-cb0d-45ab-91b4-e4639c9cd40a");
        }
    }
}
