using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuvankienthuc.Migrations
{
    /// <inheritdoc />
    public partial class CleanStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoiDaps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HoiDaps",
                columns: table => new
                {
                    MaHD = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    CauHoi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TraLoi = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_HoiDaps_MaSV",
                table: "HoiDaps",
                column: "MaSV");
        }
    }
}
