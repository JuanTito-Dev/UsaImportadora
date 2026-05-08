using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPiezaKit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsKit",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PiezaKit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    CodigoUniversal = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    CantidadPorKit = table.Column<int>(type: "integer", nullable: false),
                    StockActual = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiezaKit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PiezaKit_Producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PiezaKit_CodigoUniversal",
                table: "PiezaKit",
                column: "CodigoUniversal");

            migrationBuilder.CreateIndex(
                name: "IX_PiezaKit_Id_Producto",
                table: "PiezaKit",
                column: "Id_Producto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PiezaKit");

            migrationBuilder.DropColumn(
                name: "EsKit",
                table: "Producto");
        }
    }
}
