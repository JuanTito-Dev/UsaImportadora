using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaMarca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Producto_Codigo",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Producto");

            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Marca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marca", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Codigo_Marca",
                table: "Producto",
                columns: new[] { "Codigo", "MarcaId" },
                unique: true,
                filter: "\"MarcaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Codigo_SinMarca",
                table: "Producto",
                column: "Codigo",
                unique: true,
                filter: "\"MarcaId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_MarcaId",
                table: "Producto",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Marca_Nombre",
                table: "Marca",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_Marca_MarcaId",
                table: "Producto",
                column: "MarcaId",
                principalTable: "Marca",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Producto_Marca_MarcaId",
                table: "Producto");

            migrationBuilder.DropTable(
                name: "Marca");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Codigo_Marca",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Codigo_SinMarca",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_MarcaId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "Producto");

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Producto",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Codigo",
                table: "Producto",
                column: "Codigo",
                unique: true);
        }
    }
}
