using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuvankienthuc.Migrations
{
    /// <inheritdoc />
    public partial class AddDeXuatDanhGia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeXuatChiTiets_KienThucs_MaKT",
                table: "DeXuatChiTiets");

            migrationBuilder.DropForeignKey(
                name: "FK_DeXuats_MonHocs_MaMH",
                table: "DeXuats");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiLieuChuDe_ChuDes_MaCD",
                table: "TaiLieuChuDe");

            migrationBuilder.CreateTable(
                name: "DeXuatDanhGias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDX = table.Column<int>(type: "int", nullable: false),
                    MaUser = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeXuatDanhGias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeXuatDanhGias_DeXuats_MaDX",
                        column: x => x.MaDX,
                        principalTable: "DeXuats",
                        principalColumn: "MaDX",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeXuatDanhGias_Users_MaUser",
                        column: x => x.MaUser,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeXuatDanhGias_MaDX",
                table: "DeXuatDanhGias",
                column: "MaDX");

            migrationBuilder.CreateIndex(
                name: "IX_DeXuatDanhGias_MaUser",
                table: "DeXuatDanhGias",
                column: "MaUser");

            migrationBuilder.AddForeignKey(
                name: "FK_DeXuatChiTiets_KienThucs_MaKT",
                table: "DeXuatChiTiets",
                column: "MaKT",
                principalTable: "KienThucs",
                principalColumn: "MaKT",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeXuats_MonHocs_MaMH",
                table: "DeXuats",
                column: "MaMH",
                principalTable: "MonHocs",
                principalColumn: "MaMH",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaiLieuChuDe_ChuDes_MaCD",
                table: "TaiLieuChuDe",
                column: "MaCD",
                principalTable: "ChuDes",
                principalColumn: "MaCD",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeXuatChiTiets_KienThucs_MaKT",
                table: "DeXuatChiTiets");

            migrationBuilder.DropForeignKey(
                name: "FK_DeXuats_MonHocs_MaMH",
                table: "DeXuats");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiLieuChuDe_ChuDes_MaCD",
                table: "TaiLieuChuDe");

            migrationBuilder.DropTable(
                name: "DeXuatDanhGias");

            migrationBuilder.AddForeignKey(
                name: "FK_DeXuatChiTiets_KienThucs_MaKT",
                table: "DeXuatChiTiets",
                column: "MaKT",
                principalTable: "KienThucs",
                principalColumn: "MaKT");

            migrationBuilder.AddForeignKey(
                name: "FK_DeXuats_MonHocs_MaMH",
                table: "DeXuats",
                column: "MaMH",
                principalTable: "MonHocs",
                principalColumn: "MaMH");

            migrationBuilder.AddForeignKey(
                name: "FK_TaiLieuChuDe_ChuDes_MaCD",
                table: "TaiLieuChuDe",
                column: "MaCD",
                principalTable: "ChuDes",
                principalColumn: "MaCD");
        }
    }
}
