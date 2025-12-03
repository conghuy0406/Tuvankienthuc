using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuvankienthuc.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFieldsToKienThuc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DoKhoAI",
                table: "KienThucs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCoreAI",
                table: "KienThucs",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaAIJson",
                table: "KienThucs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrereqCountAI",
                table: "KienThucs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoKhoAI",
                table: "KienThucs");

            migrationBuilder.DropColumn(
                name: "IsCoreAI",
                table: "KienThucs");

            migrationBuilder.DropColumn(
                name: "MetaAIJson",
                table: "KienThucs");

            migrationBuilder.DropColumn(
                name: "PrereqCountAI",
                table: "KienThucs");
        }
    }
}
