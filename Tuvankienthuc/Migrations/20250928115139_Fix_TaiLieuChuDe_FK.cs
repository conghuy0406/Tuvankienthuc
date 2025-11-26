using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuvankienthuc.Migrations
{
    /// <inheritdoc />
    public partial class Fix_TaiLieuChuDe_FK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonHocs",
                columns: table => new
                {
                    MaMH = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenMH = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonHocs", x => x.MaMH);
                });

            migrationBuilder.CreateTable(
                name: "TaiLieus",
                columns: table => new
                {
                    MaTL = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LoaiTL = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayThem = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieus", x => x.MaTL);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Lop = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NamHoc = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChuDes",
                columns: table => new
                {
                    MaCD = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaMH = table.Column<int>(type: "int", nullable: false),
                    TenCD = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsKienThucCoBan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuDes", x => x.MaCD);
                    table.ForeignKey(
                        name: "FK_ChuDes_MonHocs_MaMH",
                        column: x => x.MaMH,
                        principalTable: "MonHocs",
                        principalColumn: "MaMH",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeXuats",
                columns: table => new
                {
                    MaDX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nguon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeXuats", x => x.MaDX);
                    table.ForeignKey(
                        name: "FK_DeXuats_Users_MaSV",
                        column: x => x.MaSV,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoiDaps",
                columns: table => new
                {
                    MaHD = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    CauHoi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TraLoi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoiDaps", x => x.MaHD);
                    table.ForeignKey(
                        name: "FK_HoiDaps_Users_MaSV",
                        column: x => x.MaSV,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KienThucs",
                columns: table => new
                {
                    MaKT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCD = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoKho = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KienThucs", x => x.MaKT);
                    table.ForeignKey(
                        name: "FK_KienThucs_ChuDes_MaCD",
                        column: x => x.MaCD,
                        principalTable: "ChuDes",
                        principalColumn: "MaCD",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaiLieuChuDe",
                columns: table => new
                {
                    MaTL = table.Column<int>(type: "int", nullable: false),
                    MaCD = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieuChuDe", x => new { x.MaTL, x.MaCD });
                    table.ForeignKey(
                        name: "FK_TaiLieuChuDe_ChuDes_MaCD",
                        column: x => x.MaCD,
                        principalTable: "ChuDes",
                        principalColumn: "MaCD");
                    table.ForeignKey(
                        name: "FK_TaiLieuChuDe_TaiLieus_MaTL",
                        column: x => x.MaTL,
                        principalTable: "TaiLieus",
                        principalColumn: "MaTL",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KienThucSinhViens",
                columns: table => new
                {
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    MaKT = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KienThucSinhViens", x => new { x.MaSV, x.MaKT });
                    table.ForeignKey(
                        name: "FK_KienThucSinhViens_KienThucs_MaKT",
                        column: x => x.MaKT,
                        principalTable: "KienThucs",
                        principalColumn: "MaKT",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KienThucSinhViens_Users_MaSV",
                        column: x => x.MaSV,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChuDes_MaMH",
                table: "ChuDes",
                column: "MaMH");

            migrationBuilder.CreateIndex(
                name: "IX_DeXuats_MaSV",
                table: "DeXuats",
                column: "MaSV");

            migrationBuilder.CreateIndex(
                name: "IX_HoiDaps_MaSV",
                table: "HoiDaps",
                column: "MaSV");

            migrationBuilder.CreateIndex(
                name: "IX_KienThucs_MaCD",
                table: "KienThucs",
                column: "MaCD");

            migrationBuilder.CreateIndex(
                name: "IX_KienThucSinhViens_MaKT",
                table: "KienThucSinhViens",
                column: "MaKT");

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuChuDe_MaCD",
                table: "TaiLieuChuDe",
                column: "MaCD");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeXuats");

            migrationBuilder.DropTable(
                name: "HoiDaps");

            migrationBuilder.DropTable(
                name: "KienThucSinhViens");

            migrationBuilder.DropTable(
                name: "TaiLieuChuDe");

            migrationBuilder.DropTable(
                name: "KienThucs");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "TaiLieus");

            migrationBuilder.DropTable(
                name: "ChuDes");

            migrationBuilder.DropTable(
                name: "MonHocs");
        }
    }
}
