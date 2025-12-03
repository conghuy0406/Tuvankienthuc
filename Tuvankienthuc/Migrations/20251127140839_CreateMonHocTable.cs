using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuvankienthuc.Migrations
{
    /// <inheritdoc />
    public partial class CreateMonHocTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GiangVienId",
                table: "MonHocs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonHocs_GiangVienId",
                table: "MonHocs",
                column: "GiangVienId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonHocs_Users_GiangVienId",
                table: "MonHocs",
                column: "GiangVienId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonHocs_Users_GiangVienId",
                table: "MonHocs");

            migrationBuilder.DropIndex(
                name: "IX_MonHocs_GiangVienId",
                table: "MonHocs");

            migrationBuilder.DropColumn(
                name: "GiangVienId",
                table: "MonHocs");
        }
    }
}
