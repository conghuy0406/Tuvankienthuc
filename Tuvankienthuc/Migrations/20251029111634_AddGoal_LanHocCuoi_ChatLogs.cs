using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tuvankienthuc.Migrations
{
    /// <inheritdoc />
    public partial class AddGoal_LanHocCuoi_ChatLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LanHocCuoi",
                table: "KienThucSinhViens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Goal",
                table: "DeXuats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSV = table.Column<int>(type: "int", nullable: false),
                    MaDX = table.Column<int>(type: "int", nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TraLoi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatLogs_DeXuats_MaDX",
                        column: x => x.MaDX,
                        principalTable: "DeXuats",
                        principalColumn: "MaDX",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatLogs_Users_MaSV",
                        column: x => x.MaSV,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatLogs_MaDX",
                table: "ChatLogs",
                column: "MaDX");

            migrationBuilder.CreateIndex(
                name: "IX_ChatLogs_MaSV",
                table: "ChatLogs",
                column: "MaSV");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatLogs");

            migrationBuilder.DropColumn(
                name: "LanHocCuoi",
                table: "KienThucSinhViens");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "DeXuats");
        }
    }
}
