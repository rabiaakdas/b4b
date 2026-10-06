using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B4B.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertIdsToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Firmalar_FirmaId",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_FirmaId_KullaniciAdi",
                table: "Kullanicilar");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Kullanicilar",
                table: "Kullanicilar");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Firmalar",
                table: "Firmalar");

            migrationBuilder.AddColumn<Guid>(
                name: "GuidId",
                table: "Firmalar",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GuidId",
                table: "Kullanicilar",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GuidFirmaId",
                table: "Kullanicilar",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Firmalar]
                SET [GuidId] = CASE [Id]
                    WHEN 1 THEN '11111111-1111-1111-1111-111111111111'
                    WHEN 2 THEN '22222222-2222-2222-2222-222222222222'
                    ELSE NEWID()
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE [Kullanicilar]
                SET [GuidId] = NEWID();
                """);

            migrationBuilder.Sql("""
                UPDATE kullanici
                SET [GuidFirmaId] = firma.[GuidId]
                FROM [Kullanicilar] AS kullanici
                INNER JOIN [Firmalar] AS firma ON kullanici.[FirmaId] = firma.[Id];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Firmalar] WHERE [GuidId] IS NULL)
                    THROW 51000, 'Firma GUID dönüşümü tamamlanamadı.', 1;

                IF EXISTS (SELECT 1 FROM [Kullanicilar] WHERE [GuidId] IS NULL OR [GuidFirmaId] IS NULL)
                    THROW 51001, 'Kullanıcı GUID dönüşümü tamamlanamadı.', 1;
                """);

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "FirmaId",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Firmalar");

            migrationBuilder.RenameColumn(
                name: "GuidId",
                table: "Firmalar",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "GuidId",
                table: "Kullanicilar",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "GuidFirmaId",
                table: "Kullanicilar",
                newName: "FirmaId");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Firmalar",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Kullanicilar",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FirmaId",
                table: "Kullanicilar",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Firmalar",
                table: "Firmalar",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Kullanicilar",
                table: "Kullanicilar",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_FirmaId_KullaniciAdi",
                table: "Kullanicilar",
                columns: new[] { "FirmaId", "KullaniciAdi" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Firmalar_FirmaId",
                table: "Kullanicilar",
                column: "FirmaId",
                principalTable: "Firmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("GUID ID migration geri alınamaz. Eski int ID değerleri kalıcı olarak GUID değerlere taşınır.");
        }
    }
}
