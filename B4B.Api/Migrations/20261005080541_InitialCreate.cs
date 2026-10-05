using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace B4B.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Firmalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirmaAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfigDegeri = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firmalar", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Firmalar",
                columns: new[] { "Id", "ConfigDegeri", "Domain", "FirmaAdi" },
                values: new object[,]
                {
                    { 1, "Firma A icin ornek config degeri", "localhost:7101", "Firma A" },
                    { 2, "Firma B icin ornek config degeri", "localhost:7201", "Firma B" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Firmalar_Domain",
                table: "Firmalar",
                column: "Domain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Firmalar");
        }
    }
}
