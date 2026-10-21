using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase8DevolucionUnidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "maneja_fraccion",
                schema: "flexpos",
                table: "detalles_venta",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE flexpos.detalles_venta AS detalle
                SET maneja_fraccion = articulo.maneja_fraccion
                FROM flexpos.articulos_inventario AS articulo
                WHERE detalle.tipo = 'Producto'
                  AND detalle.articulo_inventario_id = articulo.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "maneja_fraccion",
                schema: "flexpos",
                table: "detalles_venta");
        }
    }
}
