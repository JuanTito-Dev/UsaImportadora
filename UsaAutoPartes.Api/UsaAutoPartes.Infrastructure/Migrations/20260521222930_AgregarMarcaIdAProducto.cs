using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMarcaIdAProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Importacion_Detalle");

            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "Importacion_Detalle",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "Importacion_Detalle");

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Importacion_Detalle",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
