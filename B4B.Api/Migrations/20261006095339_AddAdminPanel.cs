using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B4B.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPanel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AdminMi",
                table: "Kullanicilar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirmaKodu",
                table: "Firmalar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Firmalar
                SET FirmaKodu = CASE
                    WHEN Id = '11111111-1111-1111-1111-111111111111' THEN N'FIRMAA'
                    WHEN Id = '22222222-2222-2222-2222-222222222222' THEN N'FIRMAB'
                    ELSE LEFT(N'FIRMA' + REPLACE(CONVERT(nvarchar(36), Id), N'-', N''), 20)
                END
                WHERE FirmaKodu = N'';
                """);

            migrationBuilder.UpdateData(
                table: "Firmalar",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "FirmaKodu",
                value: "FIRMAA");

            migrationBuilder.UpdateData(
                table: "Firmalar",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "FirmaKodu",
                value: "FIRMAB");

            migrationBuilder.CreateIndex(
                name: "IX_Firmalar_FirmaKodu",
                table: "Firmalar",
                column: "FirmaKodu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Firmalar_FirmaKodu",
                table: "Firmalar");

            migrationBuilder.DropColumn(
                name: "AdminMi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "FirmaKodu",
                table: "Firmalar");
        }
    }
}
