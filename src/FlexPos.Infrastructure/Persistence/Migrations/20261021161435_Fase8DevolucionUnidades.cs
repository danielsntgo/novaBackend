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

            migrationBuilder.Sql(
                """
                ALTER TABLE flexpos.detalles_venta ENABLE ROW LEVEL SECURITY;

                DO $flexpos_rls_fase8$
                BEGIN
                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.detalles_venta TO flexpos_api';

                        IF NOT EXISTS (
                            SELECT 1 FROM pg_policies
                            WHERE schemaname = 'flexpos'
                              AND tablename = 'detalles_venta'
                              AND policyname = 'pol_flexpos_api_select'
                        ) THEN
                            EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.detalles_venta FOR SELECT TO flexpos_api USING (true)';
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM pg_policies
                            WHERE schemaname = 'flexpos'
                              AND tablename = 'detalles_venta'
                              AND policyname = 'pol_flexpos_api_insert'
                        ) THEN
                            EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.detalles_venta FOR INSERT TO flexpos_api WITH CHECK (true)';
                        END IF;
                    END IF;
                END
                $flexpos_rls_fase8$;
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
