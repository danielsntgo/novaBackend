using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase6MonedaComision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "codigo_moneda",
                schema: "flexpos",
                table: "comisiones",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE flexpos.comisiones AS comision
                SET codigo_moneda = venta.codigo_moneda
                FROM flexpos.ventas AS venta
                WHERE venta.id = comision.venta_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "codigo_moneda",
                schema: "flexpos",
                table: "comisiones",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "codigo_moneda",
                schema: "flexpos",
                table: "comisiones");
        }
    }
}
