using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class agregarhistorialdeproductosaalabasededatosconfleteinternacionaladuanaytrasporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Aduana_Arancel",
                table: "Importaciones",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "F_Internacional",
                table: "Importaciones",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Trasporte_Interno",
                table: "Importaciones",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Importacion_Detalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Importacion = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    CodigoAux = table.Column<string>(type: "text", nullable: false),
                    CodigoAux2 = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Marca = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Unidad_Medida = table.Column<string>(type: "text", nullable: false),
                    Ubicacion = table.Column<string>(type: "text", nullable: false),
                    Stock_Actual = table.Column<int>(type: "integer", nullable: false),
                    Stock_Minimo = table.Column<int>(type: "integer", nullable: false),
                    Costo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ConversionABs = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Importacion_Detalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Importacion_Detalle_Importacion",
                        column: x => x.Id_Importacion,
                        principalTable: "Importaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacionDetalle_Codigo",
                table: "Importacion_Detalle",
                column: "Codigo");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacionDetalle_IdImportacion",
                table: "Importacion_Detalle",
                column: "Id_Importacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Importacion_Detalle");

            migrationBuilder.DropColumn(
                name: "Aduana_Arancel",
                table: "Importaciones");

            migrationBuilder.DropColumn(
                name: "F_Internacional",
                table: "Importaciones");

            migrationBuilder.DropColumn(
                name: "Trasporte_Interno",
                table: "Importaciones");
        }
    }
}
