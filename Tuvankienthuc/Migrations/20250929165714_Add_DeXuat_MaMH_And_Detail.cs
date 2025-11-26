using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuvankienthuc.Migrations
{
    /// <inheritdoc />
    public partial class Add_DeXuat_MaMH_And_Detail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NoiDung",
                table: "DeXuats",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Nguon",
                table: "DeXuats",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "MaMH",
                table: "DeXuats",
                type: "int",
                maxLength: 10,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DeXuatChiTiets",
                columns: table => new
                {
                    MaDX = table.Column<int>(type: "int", nullable: false),
                    MaKT = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RankIndex = table.Column<int>(type: "int", nullable: false),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeXuatChiTiets", x => new { x.MaDX, x.MaKT });
                    table.ForeignKey(
                        name: "FK_DeXuatChiTiets_DeXuats_MaDX",
                        column: x => x.MaDX,
                        principalTable: "DeXuats",
                        principalColumn: "MaDX",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeXuatChiTiets_KienThucs_MaKT",
                        column: x => x.MaKT,
                        principalTable: "KienThucs",
                        principalColumn: "MaKT");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeXuats_MaMH",
                table: "DeXuats",
                column: "MaMH");

            migrationBuilder.CreateIndex(
                name: "IX_DeXuatChiTiets_MaKT",
                table: "DeXuatChiTiets",
                column: "MaKT");

            migrationBuilder.AddForeignKey(
                name: "FK_DeXuats_MonHocs_MaMH",
                table: "DeXuats",
                column: "MaMH",
                principalTable: "MonHocs",
                principalColumn: "MaMH");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeXuats_MonHocs_MaMH",
                table: "DeXuats");

            migrationBuilder.DropTable(
                name: "DeXuatChiTiets");

            migrationBuilder.DropIndex(
                name: "IX_DeXuats_MaMH",
                table: "DeXuats");

            migrationBuilder.DropColumn(
                name: "MaMH",
                table: "DeXuats");

            migrationBuilder.AlterColumn<string>(
                name: "NoiDung",
                table: "DeXuats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Nguon",
                table: "DeXuats",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
