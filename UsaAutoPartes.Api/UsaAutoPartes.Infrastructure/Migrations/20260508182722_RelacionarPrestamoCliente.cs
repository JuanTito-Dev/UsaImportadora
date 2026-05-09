using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelacionarPrestamoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id_Producto",
                table: "Prestamo_detalle",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id_Cliente",
                table: "Prestamo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_detalle_Id_Producto",
                table: "Prestamo_detalle",
                column: "Id_Producto");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamo_Id_Cliente",
                table: "Prestamo",
                column: "Id_Cliente");

            migrationBuilder.AddForeignKey(
                name: "fk_prestamo_cliente",
                table: "Prestamo",
                column: "Id_Cliente",
                principalTable: "Cliente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_prestamo_detalle_producto",
                table: "Prestamo_detalle",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_prestamo_cliente",
                table: "Prestamo");

            migrationBuilder.DropForeignKey(
                name: "fk_prestamo_detalle_producto",
                table: "Prestamo_detalle");

            migrationBuilder.DropIndex(
                name: "IX_Prestamo_detalle_Id_Producto",
                table: "Prestamo_detalle");

            migrationBuilder.DropIndex(
                name: "IX_Prestamo_Id_Cliente",
                table: "Prestamo");

            migrationBuilder.DropColumn(
                name: "Id_Producto",
                table: "Prestamo_detalle");

            migrationBuilder.DropColumn(
                name: "Id_Cliente",
                table: "Prestamo");
        }
    }
}
