using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase6Comisiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "liquidacion_comision_id",
                schema: "flexpos",
                table: "movimientos_caja",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "liquidaciones_comision",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empleado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo_pago_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo_pago_nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    es_efectivo = table.Column<bool>(type: "boolean", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    referencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    caja_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liquidaciones_comision", x => x.id);
                    table.CheckConstraint("ck_liquidaciones_comision_importe", "importe > 0");
                    table.ForeignKey(
                        name: "FK_liquidaciones_comision_cajas_caja_id",
                        column: x => x.caja_id,
                        principalSchema: "flexpos",
                        principalTable: "cajas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_comision_empleados_empleado_id",
                        column: x => x.empleado_id,
                        principalSchema: "flexpos",
                        principalTable: "empleados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_liquidaciones_comision_metodos_pago_metodo_pago_id",
                        column: x => x.metodo_pago_id,
                        principalSchema: "flexpos",
                        principalTable: "metodos_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reglas_comision",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empleado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servicio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reglas_comision", x => x.id);
                    table.CheckConstraint("ck_reglas_comision_valor", "valor > 0 AND (tipo <> 'Porcentaje' OR valor <= 100)");
                    table.ForeignKey(
                        name: "FK_reglas_comision_empleados_empleado_id",
                        column: x => x.empleado_id,
                        principalSchema: "flexpos",
                        principalTable: "empleados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reglas_comision_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalSchema: "flexpos",
                        principalTable: "servicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comisiones",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empleado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servicio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    detalle_venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    tipo_tarifa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor_tarifa = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    base_calculo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    comision_original_id = table.Column<Guid>(type: "uuid", nullable: true),
                    devolucion_venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    liquidacion_comision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comisiones", x => x.id);
                    table.CheckConstraint("ck_comisiones_importes", "base_calculo >= 0 AND cantidad > 0 AND importe > 0");
                    table.ForeignKey(
                        name: "FK_comisiones_comisiones_comision_original_id",
                        column: x => x.comision_original_id,
                        principalSchema: "flexpos",
                        principalTable: "comisiones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comisiones_detalles_venta_detalle_venta_id",
                        column: x => x.detalle_venta_id,
                        principalSchema: "flexpos",
                        principalTable: "detalles_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comisiones_devoluciones_venta_devolucion_venta_id",
                        column: x => x.devolucion_venta_id,
                        principalSchema: "flexpos",
                        principalTable: "devoluciones_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comisiones_empleados_empleado_id",
                        column: x => x.empleado_id,
                        principalSchema: "flexpos",
                        principalTable: "empleados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comisiones_liquidaciones_comision_liquidacion_comision_id",
                        column: x => x.liquidacion_comision_id,
                        principalSchema: "flexpos",
                        principalTable: "liquidaciones_comision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comisiones_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalSchema: "flexpos",
                        principalTable: "servicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comisiones_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "flexpos",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_liquidacion_comision_id",
                schema: "flexpos",
                table: "movimientos_caja",
                column: "liquidacion_comision_id");

            migrationBuilder.CreateIndex(
                name: "IX_comisiones_devolucion_venta_id",
                schema: "flexpos",
                table: "comisiones",
                column: "devolucion_venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_comisiones_empleado_estado_fecha",
                schema: "flexpos",
                table: "comisiones",
                columns: new[] { "empleado_id", "estado", "fecha_creacion_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_comisiones_liquidacion",
                schema: "flexpos",
                table: "comisiones",
                column: "liquidacion_comision_id");

            migrationBuilder.CreateIndex(
                name: "ix_comisiones_original",
                schema: "flexpos",
                table: "comisiones",
                column: "comision_original_id");

            migrationBuilder.CreateIndex(
                name: "IX_comisiones_servicio_id",
                schema: "flexpos",
                table: "comisiones",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "IX_comisiones_venta_id",
                schema: "flexpos",
                table: "comisiones",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ux_comisiones_devengo_detalle",
                schema: "flexpos",
                table: "comisiones",
                column: "detalle_venta_id",
                unique: true,
                filter: "tipo_movimiento = 'DevengoServicio'");

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_comision_caja_id",
                schema: "flexpos",
                table: "liquidaciones_comision",
                column: "caja_id");

            migrationBuilder.CreateIndex(
                name: "ix_liquidaciones_comision_empleado_fecha",
                schema: "flexpos",
                table: "liquidaciones_comision",
                columns: new[] { "empleado_id", "fecha_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_liquidaciones_comision_metodo_pago_id",
                schema: "flexpos",
                table: "liquidaciones_comision",
                column: "metodo_pago_id");

            migrationBuilder.CreateIndex(
                name: "ix_reglas_comision_empleado_activa",
                schema: "flexpos",
                table: "reglas_comision",
                columns: new[] { "empleado_id", "activa" });

            migrationBuilder.CreateIndex(
                name: "IX_reglas_comision_servicio_id",
                schema: "flexpos",
                table: "reglas_comision",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "ux_reglas_comision_empleado_servicio",
                schema: "flexpos",
                table: "reglas_comision",
                columns: new[] { "empleado_id", "servicio_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_caja_liquidaciones_comision_liquidacion_comisio~",
                schema: "flexpos",
                table: "movimientos_caja",
                column: "liquidacion_comision_id",
                principalSchema: "flexpos",
                principalTable: "liquidaciones_comision",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                ALTER TABLE flexpos.reglas_comision ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.comisiones ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.liquidaciones_comision ENABLE ROW LEVEL SECURITY;

                DO $flexpos_rls_fase6$
                BEGIN
                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'REVOKE ALL ON TABLE flexpos.reglas_comision FROM flexpos_api';
                        EXECUTE 'REVOKE ALL ON TABLE flexpos.comisiones FROM flexpos_api';
                        EXECUTE 'REVOKE ALL ON TABLE flexpos.liquidaciones_comision FROM flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.reglas_comision TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.comisiones TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.liquidaciones_comision TO flexpos_api';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.reglas_comision FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.reglas_comision FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.reglas_comision FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.comisiones FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.comisiones FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.comisiones FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.liquidaciones_comision FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.liquidaciones_comision FOR INSERT TO flexpos_api WITH CHECK (true)';
                    END IF;
                END
                $flexpos_rls_fase6$;
                """);

            migrationBuilder.Sql("""
                UPDATE flexpos.configuracion_negocio
                SET comision_productos_habilitada = false
                WHERE comision_productos_habilitada = true;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_caja_liquidaciones_comision_liquidacion_comisio~",
                schema: "flexpos",
                table: "movimientos_caja");

            migrationBuilder.DropTable(
                name: "comisiones",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "reglas_comision",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "liquidaciones_comision",
                schema: "flexpos");

            migrationBuilder.DropIndex(
                name: "IX_movimientos_caja_liquidacion_comision_id",
                schema: "flexpos",
                table: "movimientos_caja");

            migrationBuilder.DropColumn(
                name: "liquidacion_comision_id",
                schema: "flexpos",
                table: "movimientos_caja");
        }
    }
}
