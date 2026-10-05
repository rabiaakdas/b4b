using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B4B.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTurkishVisibleTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Firmalar",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConfigDegeri",
                value: "Firma A için örnek yapılandırma değeri");

            migrationBuilder.UpdateData(
                table: "Firmalar",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConfigDegeri",
                value: "Firma B için örnek yapılandırma değeri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Firmalar",
                keyColumn: "Id",
                keyValue: 1,
                column: "ConfigDegeri",
                value: "Firma A icin ornek config degeri");

            migrationBuilder.UpdateData(
                table: "Firmalar",
                keyColumn: "Id",
                keyValue: 2,
                column: "ConfigDegeri",
                value: "Firma B icin ornek config degeri");
        }
    }
}
