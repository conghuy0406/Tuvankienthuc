using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuvankienthuc.Migrations
{
    /// <inheritdoc />
    public partial class AddBaocao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaoCaoVanDes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaUser = table.Column<int>(type: "int", nullable: false),
                    MaDX = table.Column<int>(type: "int", nullable: true),
                    LoaiVanDe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoCaoVanDes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaoCaoVanDes_DeXuats_MaDX",
                        column: x => x.MaDX,
                        principalTable: "DeXuats",
                        principalColumn: "MaDX",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BaoCaoVanDes_Users_MaUser",
                        column: x => x.MaUser,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoVanDes_MaDX",
                table: "BaoCaoVanDes",
                column: "MaDX");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoVanDes_MaUser",
                table: "BaoCaoVanDes",
                column: "MaUser");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaoCaoVanDes");
        }
    }
}
