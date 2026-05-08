using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOrdenVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdenVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cajero = table.Column<Guid>(type: "uuid", nullable: false),
                    Id_Almacenero = table.Column<Guid>(type: "uuid", nullable: true),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: true),
                    Id_Caja = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCompletada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotaCancelacion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenVenta_Almacenero",
                        column: x => x.Id_Almacenero,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenVenta_Caja",
                        column: x => x.Id_Caja,
                        principalTable: "Caja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenVenta_Cajero",
                        column: x => x.Id_Cajero,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenVenta_Cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenVentaItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Orden = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    EsParcial = table.Column<bool>(type: "boolean", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NotaIncompleto = table.Column<string>(type: "text", nullable: true),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Id_Descuento = table.Column<int>(type: "integer", nullable: true),
                    MontoDescuento = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenVentaItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenVentaItem_Descuento",
                        column: x => x.Id_Descuento,
                        principalTable: "Descuento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenVentaItem_Orden",
                        column: x => x.Id_Orden,
                        principalTable: "OrdenVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenVentaItem_Producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenVentaItemPieza",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Item = table.Column<int>(type: "integer", nullable: false),
                    Id_Pieza = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Confirmado = table.Column<bool>(type: "boolean", nullable: false),
                    NotaIncompleto = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenVentaItemPieza", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenVentaItemPieza_Item",
                        column: x => x.Id_Item,
                        principalTable: "OrdenVentaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenVentaItemPieza_Pieza",
                        column: x => x.Id_Pieza,
                        principalTable: "PiezaKit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_Cajero",
                table: "OrdenVenta",
                column: "Id_Cajero");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_Estado",
                table: "OrdenVenta",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_Id_Almacenero",
                table: "OrdenVenta",
                column: "Id_Almacenero");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_Id_Caja",
                table: "OrdenVenta",
                column: "Id_Caja");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVenta_Id_Cliente",
                table: "OrdenVenta",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVentaItem_Id_Descuento",
                table: "OrdenVentaItem",
                column: "Id_Descuento");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVentaItem_Id_Orden",
                table: "OrdenVentaItem",
                column: "Id_Orden");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVentaItem_Id_Producto",
                table: "OrdenVentaItem",
                column: "Id_Producto");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVentaItemPieza_Id_Item",
                table: "OrdenVentaItemPieza",
                column: "Id_Item");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenVentaItemPieza_Id_Pieza",
                table: "OrdenVentaItemPieza",
                column: "Id_Pieza");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdenVentaItemPieza");

            migrationBuilder.DropTable(
                name: "OrdenVentaItem");

            migrationBuilder.DropTable(
                name: "OrdenVenta");
        }
    }
}
