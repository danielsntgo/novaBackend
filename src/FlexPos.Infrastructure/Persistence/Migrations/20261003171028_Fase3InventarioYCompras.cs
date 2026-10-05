using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase3InventarioYCompras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articulos_inventario",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    codigo_normalizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unidad_base = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unidad_base_normalizada = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    maneja_fraccion = table.Column<bool>(type: "boolean", nullable: false),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    categoria_normalizada = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    existencia_actual = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    cantidad_minima = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    costo_promedio_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_promedio_codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    precio_venta_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    precio_venta_codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articulos_inventario", x => x.id);
                    table.CheckConstraint("ck_articulos_inventario_existencia", "existencia_actual >= 0");
                    table.CheckConstraint("ck_articulos_inventario_minimo", "cantidad_minima >= 0");
                });

            migrationBuilder.CreateTable(
                name: "proveedores",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    identificacion_fiscal = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "compras",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_compra_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    referencia = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    referencia_normalizada = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_confirmacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras", x => x.id);
                    table.CheckConstraint("ck_compras_total", "total_importe >= 0");
                    table.ForeignKey(
                        name: "FK_compras_proveedores_proveedor_id",
                        column: x => x.proveedor_id,
                        principalSchema: "flexpos",
                        principalTable: "proveedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalles_compra",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    compra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    articulo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_articulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    unidad_base = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    costo_unitario_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_unitario_codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_linea_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_linea_codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    vigente = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalles_compra", x => x.id);
                    table.CheckConstraint("ck_detalles_compra_cantidad", "cantidad > 0");
                    table.ForeignKey(
                        name: "FK_detalles_compra_articulos_inventario_articulo_id",
                        column: x => x.articulo_id,
                        principalSchema: "flexpos",
                        principalTable: "articulos_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalles_compra_compras_compra_id",
                        column: x => x.compra_id,
                        principalSchema: "flexpos",
                        principalTable: "compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_inventario",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    articulo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    existencia_resultante = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    costo_unitario_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_unitario_codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    motivo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    compra_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detalle_compra_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_inventario", x => x.id);
                    table.CheckConstraint("ck_movimientos_inventario_cantidad", "cantidad > 0");
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_articulos_inventario_articulo_id",
                        column: x => x.articulo_id,
                        principalSchema: "flexpos",
                        principalTable: "articulos_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_compras_compra_id",
                        column: x => x.compra_id,
                        principalSchema: "flexpos",
                        principalTable: "compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_detalles_compra_detalle_compra_id",
                        column: x => x.detalle_compra_id,
                        principalSchema: "flexpos",
                        principalTable: "detalles_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_articulos_activo_tipo_nombre",
                schema: "flexpos",
                table: "articulos_inventario",
                columns: new[] { "activo", "tipo", "nombre_normalizado" });

            migrationBuilder.CreateIndex(
                name: "ix_articulos_categoria_prefijo",
                schema: "flexpos",
                table: "articulos_inventario",
                column: "categoria_normalizada")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_articulos_nombre_prefijo",
                schema: "flexpos",
                table: "articulos_inventario",
                column: "nombre_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ux_articulos_codigo_normalizado",
                schema: "flexpos",
                table: "articulos_inventario",
                column: "codigo_normalizado",
                unique: true,
                filter: "codigo_normalizado IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_compras_estado_fecha",
                schema: "flexpos",
                table: "compras",
                columns: new[] { "estado", "fecha_compra_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_compras_proveedor",
                schema: "flexpos",
                table: "compras",
                column: "proveedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_compras_referencia_prefijo",
                schema: "flexpos",
                table: "compras",
                column: "referencia_normalizada")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_detalles_compra_articulo",
                schema: "flexpos",
                table: "detalles_compra",
                column: "articulo_id");

            migrationBuilder.CreateIndex(
                name: "ix_detalles_compra_vigentes",
                schema: "flexpos",
                table: "detalles_compra",
                columns: new[] { "compra_id", "vigente" });

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_articulo_fecha",
                schema: "flexpos",
                table: "movimientos_inventario",
                columns: new[] { "articulo_id", "fecha_creacion_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_compra",
                schema: "flexpos",
                table: "movimientos_inventario",
                column: "compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_detalle_compra_id",
                schema: "flexpos",
                table: "movimientos_inventario",
                column: "detalle_compra_id");

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_activo_nombre",
                schema: "flexpos",
                table: "proveedores",
                columns: new[] { "activo", "nombre_normalizado" });

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_nombre_prefijo",
                schema: "flexpos",
                table: "proveedores",
                column: "nombre_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.Sql("""
                DO $flexpos_rls_fase3$
                DECLARE tabla text;
                BEGIN
                    FOR tabla IN SELECT unnest(ARRAY[
                        'articulos_inventario', 'movimientos_inventario', 'proveedores', 'compras', 'detalles_compra'
                    ]) LOOP
                        EXECUTE format('ALTER TABLE flexpos.%I ENABLE ROW LEVEL SECURITY', tabla);
                    END LOOP;

                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'GRANT USAGE ON SCHEMA flexpos TO flexpos_api';
                        FOR tabla IN SELECT unnest(ARRAY[
                            'articulos_inventario', 'movimientos_inventario', 'proveedores', 'compras', 'detalles_compra'
                        ]) LOOP
                            EXECUTE format('GRANT SELECT, INSERT ON TABLE flexpos.%I TO flexpos_api', tabla);
                            EXECUTE format('CREATE POLICY pol_flexpos_api_select ON flexpos.%I FOR SELECT TO flexpos_api USING (true)', tabla);
                            IF tabla = 'compras' THEN
                                EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.compras FOR INSERT TO flexpos_api WITH CHECK (estado = ''Borrador'')';
                            ELSIF tabla = 'detalles_compra' THEN
                                EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.detalles_compra FOR INSERT TO flexpos_api WITH CHECK (EXISTS (SELECT 1 FROM flexpos.compras c WHERE c.id = compra_id AND c.estado = ''Borrador''))';
                            ELSE
                                EXECUTE format('CREATE POLICY pol_flexpos_api_insert ON flexpos.%I FOR INSERT TO flexpos_api WITH CHECK (true)', tabla);
                            END IF;

                            IF tabla <> 'movimientos_inventario' THEN
                                EXECUTE format('GRANT UPDATE ON TABLE flexpos.%I TO flexpos_api', tabla);
                                IF tabla = 'compras' THEN
                                    EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.compras FOR UPDATE TO flexpos_api USING (estado = ''Borrador'') WITH CHECK (estado IN (''Borrador'', ''Confirmada''))';
                                ELSIF tabla = 'detalles_compra' THEN
                                    EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.detalles_compra FOR UPDATE TO flexpos_api USING (EXISTS (SELECT 1 FROM flexpos.compras c WHERE c.id = compra_id AND c.estado = ''Borrador'')) WITH CHECK (EXISTS (SELECT 1 FROM flexpos.compras c WHERE c.id = compra_id AND c.estado = ''Borrador''))';
                                ELSE
                                    EXECUTE format('CREATE POLICY pol_flexpos_api_update ON flexpos.%I FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)', tabla);
                                END IF;
                            END IF;
                        END LOOP;
                    END IF;
                END
                $flexpos_rls_fase3$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimientos_inventario",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "detalles_compra",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "articulos_inventario",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "compras",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "proveedores",
                schema: "flexpos");
        }
    }
}
