using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class agregarhistorialdeproductosaalabasededatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Importaciones_Proveedor_Id_Proveedor",
                table: "Importaciones");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Nombre",
                table: "Producto");

            migrationBuilder.RenameIndex(
                name: "IX_Importaciones_Codigo",
                table: "Importaciones",
                newName: "IX_Importacion_Codigo");

            migrationBuilder.CreateTable(
                name: "HistorialPrecio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_producto = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Costo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ConversionABs = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPrecio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialPrecio_Producto",
                        column: x => x.Id_producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_UserName",
                table: "Usuario",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Nombre",
                table: "Producto",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPrecio_Fecha",
                table: "HistorialPrecio",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPrecio_IdProducto",
                table: "HistorialPrecio",
                column: "Id_producto");

            migrationBuilder.AddForeignKey(
                name: "FK_Importacion_Proveedor",
                table: "Importaciones",
                column: "Id_Proveedor",
                principalTable: "Proveedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Importacion_Proveedor",
                table: "Importaciones");

            migrationBuilder.DropTable(
                name: "HistorialPrecio");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_UserName",
                table: "Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Nombre",
                table: "Producto");

            migrationBuilder.RenameIndex(
                name: "IX_Importacion_Codigo",
                table: "Importaciones",
                newName: "IX_Importaciones_Codigo");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Nombre",
                table: "Producto",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Importaciones_Proveedor_Id_Proveedor",
                table: "Importaciones",
                column: "Id_Proveedor",
                principalTable: "Proveedor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
