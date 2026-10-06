using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B4B.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sepetler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirmaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KullaniciId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sepetler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sepetler_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sepetler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SepetKalemleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SepetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UrunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Miktar = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SepetKalemleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SepetKalemleri_Sepetler_SepetId",
                        column: x => x.SepetId,
                        principalTable: "Sepetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SepetKalemleri_Urunler_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urunler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SepetKalemleri_SepetId_UrunId",
                table: "SepetKalemleri",
                columns: new[] { "SepetId", "UrunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SepetKalemleri_UrunId",
                table: "SepetKalemleri",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_Sepetler_FirmaId_KullaniciId",
                table: "Sepetler",
                columns: new[] { "FirmaId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sepetler_KullaniciId",
                table: "Sepetler",
                column: "KullaniciId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SepetKalemleri");

            migrationBuilder.DropTable(
                name: "Sepetler");
        }
    }
}
