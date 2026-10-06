using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace B4B.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Urunler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirmaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UrunKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UrunAdi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Fiyat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Urunler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Urunler_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Urunler",
                columns: new[] { "Id", "FirmaId", "Fiyat", "UrunAdi", "UrunKodu" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), new Guid("11111111-1111-1111-1111-111111111111"), 25000m, "Firma A Laptop", "A-100" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), new Guid("11111111-1111-1111-1111-111111111111"), 1250m, "Firma A Klavye", "A-200" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), new Guid("22222222-2222-2222-2222-222222222222"), 8500m, "Firma B Monitör", "B-100" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), new Guid("22222222-2222-2222-2222-222222222222"), 750m, "Firma B Mouse", "B-200" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Urunler_FirmaId_UrunKodu",
                table: "Urunler",
                columns: new[] { "FirmaId", "UrunKodu" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Urunler");
        }
    }
}
