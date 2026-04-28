using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class agregarhistorialdeproductosaalabasededatosconfleteinternacionaladuanaytrasportecondescuento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Descuento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    CantDescuento = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descuento", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "Id_nombre_descuento",
                table: "Descuento",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Descuento");
        }
    }
}
