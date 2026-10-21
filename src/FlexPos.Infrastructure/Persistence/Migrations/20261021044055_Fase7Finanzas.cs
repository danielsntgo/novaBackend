using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase7Finanzas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_venta",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "cuenta_financiera_nombre",
                schema: "flexpos",
                table: "pagos_venta",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_devolucion_venta",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "cuenta_financiera_nombre",
                schema: "flexpos",
                table: "pagos_devolucion_venta",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "cuenta_financiera_id",
                schema: "flexpos",
                table: "metodos_pago",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "cuenta_financiera_id",
                schema: "flexpos",
                table: "cajas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "cuentas_financieras",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuracion_negocio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tipo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    entidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ultimos_digitos = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    cupo_credito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_financieras", x => x.id);
                    table.CheckConstraint("ck_cuentas_financieras_cupo", "cupo_credito IS NULL OR cupo_credito > 0");
                    table.ForeignKey(
                        name: "FK_cuentas_financieras_configuracion_negocio_configuracion_neg~",
                        column: x => x.configuracion_negocio_id,
                        principalSchema: "flexpos",
                        principalTable: "configuracion_negocio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transferencias_financieras",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_destino_id = table.Column<Guid>(type: "uuid", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    concepto = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    referencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    fecha_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias_financieras", x => x.id);
                    table.CheckConstraint("ck_transferencias_financieras_importe", "importe > 0");
                    table.ForeignKey(
                        name: "FK_transferencias_financieras_cuentas_financieras_cuenta_desti~",
                        column: x => x.cuenta_destino_id,
                        principalSchema: "flexpos",
                        principalTable: "cuentas_financieras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_financieras_cuentas_financieras_cuenta_orige~",
                        column: x => x.cuenta_origen_id,
                        principalSchema: "flexpos",
                        principalTable: "cuentas_financieras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_financieros",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_financiera_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sentido = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    concepto = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    fecha_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    caja_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metodo_pago_id = table.Column<Guid>(type: "uuid", nullable: true),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    devolucion_venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    compra_id = table.Column<Guid>(type: "uuid", nullable: true),
                    liquidacion_comision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transferencia_financiera_id = table.Column<Guid>(type: "uuid", nullable: true),
                    migrado = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_financieros", x => x.id);
                    table.CheckConstraint("ck_movimientos_financieros_importe", "importe > 0");
                    table.ForeignKey(
                        name: "FK_movimientos_financieros_cajas_caja_id",
                        column: x => x.caja_id,
                        principalSchema: "flexpos",
                        principalTable: "cajas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_financieros_compras_compra_id",
                        column: x => x.compra_id,
                        principalSchema: "flexpos",
                        principalTable: "compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_financieros_cuentas_financieras_cuenta_financie~",
                        column: x => x.cuenta_financiera_id,
                        principalSchema: "flexpos",
                        principalTable: "cuentas_financieras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_financieros_devoluciones_venta_devolucion_venta~",
                        column: x => x.devolucion_venta_id,
                        principalSchema: "flexpos",
                        principalTable: "devoluciones_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_financieros_liquidaciones_comision_liquidacion_~",
                        column: x => x.liquidacion_comision_id,
                        principalSchema: "flexpos",
                        principalTable: "liquidaciones_comision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_financieros_metodos_pago_metodo_pago_id",
                        column: x => x.metodo_pago_id,
                        principalSchema: "flexpos",
                        principalTable: "metodos_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_financieros_transferencias_financieras_transfer~",
                        column: x => x.transferencia_financiera_id,
                        principalSchema: "flexpos",
                        principalTable: "transferencias_financieras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_financieros_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "flexpos",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_venta_cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_venta",
                column: "cuenta_financiera_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_devolucion_venta_cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_devolucion_venta",
                column: "cuenta_financiera_id");

            migrationBuilder.CreateIndex(
                name: "IX_metodos_pago_cuenta_financiera_id",
                schema: "flexpos",
                table: "metodos_pago",
                column: "cuenta_financiera_id");

            migrationBuilder.CreateIndex(
                name: "IX_cajas_cuenta_financiera_id",
                schema: "flexpos",
                table: "cajas",
                column: "cuenta_financiera_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_financieras_tipo",
                schema: "flexpos",
                table: "cuentas_financieras",
                columns: new[] { "configuracion_negocio_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "ux_cuentas_financieras_configuracion_nombre",
                schema: "flexpos",
                table: "cuentas_financieras",
                columns: new[] { "configuracion_negocio_id", "nombre_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_cuentas_financieras_efectivo_activa",
                schema: "flexpos",
                table: "cuentas_financieras",
                column: "configuracion_negocio_id",
                unique: true,
                filter: "tipo = 'Efectivo' AND activa = true");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_financieros_caja",
                schema: "flexpos",
                table: "movimientos_financieros",
                column: "caja_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_financieros_compra_id",
                schema: "flexpos",
                table: "movimientos_financieros",
                column: "compra_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_financieros_cuenta_fecha",
                schema: "flexpos",
                table: "movimientos_financieros",
                columns: new[] { "cuenta_financiera_id", "fecha_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_financieros_devolucion",
                schema: "flexpos",
                table: "movimientos_financieros",
                column: "devolucion_venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_financieros_liquidacion",
                schema: "flexpos",
                table: "movimientos_financieros",
                column: "liquidacion_comision_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_financieros_metodo_pago_id",
                schema: "flexpos",
                table: "movimientos_financieros",
                column: "metodo_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_financieros_transferencia_financiera_id",
                schema: "flexpos",
                table: "movimientos_financieros",
                column: "transferencia_financiera_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_financieros_venta",
                schema: "flexpos",
                table: "movimientos_financieros",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_financieras_cuenta_destino_id",
                schema: "flexpos",
                table: "transferencias_financieras",
                column: "cuenta_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_financieras_cuenta_origen_id",
                schema: "flexpos",
                table: "transferencias_financieras",
                column: "cuenta_origen_id");

            migrationBuilder.CreateIndex(
                name: "ix_transferencias_financieras_fecha",
                schema: "flexpos",
                table: "transferencias_financieras",
                column: "fecha_utc");

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION flexpos.generar_uuid_v7_migracion() RETURNS uuid
                LANGUAGE plpgsql AS $$
                DECLARE
                    marca_tiempo text;
                    aleatorio text;
                    hexadecimal text;
                BEGIN
                    marca_tiempo := lpad(to_hex(floor(extract(epoch FROM clock_timestamp()) * 1000)::bigint), 12, '0');
                    aleatorio := md5(random()::text || clock_timestamp()::text || pg_backend_pid()::text) ||
                                 md5(random()::text || clock_timestamp()::text);
                    hexadecimal := marca_tiempo || '7' || substring(aleatorio, 1, 3) ||
                                    '8' || substring(aleatorio, 4, 15);
                    RETURN (substring(hexadecimal, 1, 8) || '-' || substring(hexadecimal, 9, 4) || '-' ||
                            substring(hexadecimal, 13, 4) || '-' || substring(hexadecimal, 17, 4) || '-' ||
                            substring(hexadecimal, 21, 12))::uuid;
                END
                $$;

                INSERT INTO flexpos.cuentas_financieras
                    (id, configuracion_negocio_id, nombre, nombre_normalizado, tipo, codigo_moneda,
                     activa, fecha_creacion_utc)
                SELECT flexpos.generar_uuid_v7_migracion(), c.id, 'Caja principal', 'CAJA PRINCIPAL', 'Efectivo',
                       c.codigo_moneda, true, now()
                FROM flexpos.configuracion_negocio c;

                INSERT INTO flexpos.cuentas_financieras
                    (id, configuracion_negocio_id, nombre, nombre_normalizado, tipo, codigo_moneda,
                     activa, fecha_creacion_utc)
                SELECT flexpos.generar_uuid_v7_migracion(), c.id, 'Cuenta por asignar', 'CUENTA POR ASIGNAR', 'Otro',
                       c.codigo_moneda, true, now()
                FROM flexpos.configuracion_negocio c;

                UPDATE flexpos.metodos_pago m
                SET cuenta_financiera_id = CASE WHEN m.es_efectivo THEN efectivo.id ELSE asignar.id END
                FROM flexpos.cuentas_financieras efectivo
                JOIN flexpos.cuentas_financieras asignar
                  ON asignar.configuracion_negocio_id = efectivo.configuracion_negocio_id
                 AND asignar.tipo = 'Otro' AND asignar.nombre = 'Cuenta por asignar'
                WHERE m.configuracion_negocio_id = efectivo.configuracion_negocio_id
                  AND efectivo.tipo = 'Efectivo' AND efectivo.nombre = 'Caja principal';

                UPDATE flexpos.cajas c
                SET cuenta_financiera_id = efectivo.id
                FROM flexpos.cuentas_financieras efectivo
                WHERE efectivo.tipo = 'Efectivo' AND efectivo.nombre = 'Caja principal';

                UPDATE flexpos.pagos_venta p
                SET cuenta_financiera_id = CASE WHEN p.es_efectivo THEN efectivo.id ELSE asignar.id END,
                    cuenta_financiera_nombre = CASE WHEN p.es_efectivo THEN efectivo.nombre ELSE asignar.nombre END
                FROM flexpos.metodos_pago m
                JOIN flexpos.cuentas_financieras efectivo
                  ON efectivo.configuracion_negocio_id = m.configuracion_negocio_id
                 AND efectivo.tipo = 'Efectivo' AND efectivo.nombre = 'Caja principal'
                JOIN flexpos.cuentas_financieras asignar
                  ON asignar.configuracion_negocio_id = m.configuracion_negocio_id
                 AND asignar.tipo = 'Otro' AND asignar.nombre = 'Cuenta por asignar'
                WHERE p.metodo_pago_id = m.id;

                UPDATE flexpos.pagos_devolucion_venta p
                SET cuenta_financiera_id = CASE WHEN p.es_efectivo THEN efectivo.id ELSE asignar.id END,
                    cuenta_financiera_nombre = CASE WHEN p.es_efectivo THEN efectivo.nombre ELSE asignar.nombre END
                FROM flexpos.metodos_pago m
                JOIN flexpos.cuentas_financieras efectivo
                  ON efectivo.configuracion_negocio_id = m.configuracion_negocio_id
                 AND efectivo.tipo = 'Efectivo' AND efectivo.nombre = 'Caja principal'
                JOIN flexpos.cuentas_financieras asignar
                  ON asignar.configuracion_negocio_id = m.configuracion_negocio_id
                 AND asignar.tipo = 'Otro' AND asignar.nombre = 'Cuenta por asignar'
                WHERE p.metodo_pago_id = m.id;

                INSERT INTO flexpos.movimientos_financieros
                    (id, cuenta_financiera_id, tipo, sentido, importe, codigo_moneda, concepto, fecha_utc,
                     caja_id, metodo_pago_id, venta_id, devolucion_venta_id, liquidacion_comision_id,
                     migrado, fecha_creacion_utc, fecha_modificacion_utc, creado_por_id, modificado_por_id)
                SELECT flexpos.generar_uuid_v7_migracion(), caja.cuenta_financiera_id, m.tipo,
                       CASE WHEN m.tipo IN ('PagoVenta', 'IngresoManual') THEN 'Entrada' ELSE 'Salida' END,
                       m.importe, m.codigo_moneda, m.concepto, m.fecha_creacion_utc, m.caja_id,
                       CASE
                           WHEN m.tipo = 'PagoVenta' THEN (
                               SELECT p.metodo_pago_id FROM flexpos.pagos_venta p
                               WHERE p.venta_id = m.venta_id AND p.es_efectivo
                               ORDER BY p.fecha_creacion_utc LIMIT 1)
                           WHEN m.tipo = 'Reembolso' THEN (
                               SELECT p.metodo_pago_id FROM flexpos.pagos_devolucion_venta p
                               WHERE p.devolucion_venta_id = m.devolucion_venta_id AND p.es_efectivo
                               ORDER BY p.fecha_creacion_utc LIMIT 1)
                           WHEN m.tipo = 'PagoComision' THEN (
                               SELECT l.metodo_pago_id FROM flexpos.liquidaciones_comision l
                               WHERE l.id = m.liquidacion_comision_id)
                           ELSE NULL
                       END,
                       m.venta_id, m.devolucion_venta_id, m.liquidacion_comision_id, true,
                       m.fecha_creacion_utc, m.fecha_modificacion_utc, m.creado_por_id, m.modificado_por_id
                FROM flexpos.movimientos_caja m
                JOIN flexpos.cajas caja ON caja.id = m.caja_id;

                INSERT INTO flexpos.movimientos_financieros
                    (id, cuenta_financiera_id, tipo, sentido, importe, codigo_moneda, fecha_utc,
                     metodo_pago_id, venta_id, migrado, fecha_creacion_utc, creado_por_id)
                SELECT flexpos.generar_uuid_v7_migracion(), asignar.id, 'PagoVenta', 'Entrada', p.importe,
                       v.codigo_moneda, p.fecha_creacion_utc, p.metodo_pago_id, p.venta_id, true,
                       p.fecha_creacion_utc, p.creado_por_id
                FROM flexpos.pagos_venta p
                JOIN flexpos.ventas v ON v.id = p.venta_id
                JOIN flexpos.metodos_pago m ON m.id = p.metodo_pago_id
                JOIN flexpos.cuentas_financieras asignar
                  ON asignar.configuracion_negocio_id = m.configuracion_negocio_id
                 AND asignar.tipo = 'Otro' AND asignar.nombre = 'Cuenta por asignar'
                WHERE NOT p.es_efectivo;

                INSERT INTO flexpos.movimientos_financieros
                    (id, cuenta_financiera_id, tipo, sentido, importe, codigo_moneda, fecha_utc,
                     metodo_pago_id, devolucion_venta_id, migrado, fecha_creacion_utc, creado_por_id)
                SELECT flexpos.generar_uuid_v7_migracion(), asignar.id, 'Reembolso', 'Salida', p.importe,
                       v.codigo_moneda, p.fecha_creacion_utc, p.metodo_pago_id,
                       p.devolucion_venta_id, true, p.fecha_creacion_utc, p.creado_por_id
                FROM flexpos.pagos_devolucion_venta p
                JOIN flexpos.devoluciones_venta d ON d.id = p.devolucion_venta_id
                JOIN flexpos.ventas v ON v.id = d.venta_id
                JOIN flexpos.metodos_pago m ON m.id = p.metodo_pago_id
                JOIN flexpos.cuentas_financieras asignar
                  ON asignar.configuracion_negocio_id = m.configuracion_negocio_id
                 AND asignar.tipo = 'Otro' AND asignar.nombre = 'Cuenta por asignar'
                WHERE NOT p.es_efectivo;

                INSERT INTO flexpos.movimientos_financieros
                    (id, cuenta_financiera_id, tipo, sentido, importe, codigo_moneda, concepto, fecha_utc,
                     metodo_pago_id, liquidacion_comision_id, migrado, fecha_creacion_utc, creado_por_id)
                SELECT flexpos.generar_uuid_v7_migracion(), asignar.id, 'PagoComision', 'Salida', l.importe,
                       l.codigo_moneda, 'Liquidación histórica migrada', l.fecha_utc, l.metodo_pago_id,
                       l.id, true, l.fecha_creacion_utc, l.creado_por_id
                FROM flexpos.liquidaciones_comision l
                JOIN flexpos.metodos_pago m ON m.id = l.metodo_pago_id
                JOIN flexpos.cuentas_financieras asignar
                  ON asignar.configuracion_negocio_id = m.configuracion_negocio_id
                 AND asignar.tipo = 'Otro' AND asignar.nombre = 'Cuenta por asignar'
                WHERE NOT l.es_efectivo;

                DO $$
                DECLARE
                    asignar_id uuid;
                    saldo_asignar numeric(18,2);
                    efectivo_id uuid;
                    saldo_efectivo numeric(18,2);
                    efectivo_objetivo numeric(18,2);
                    caja_abierta record;
                    caja_cerrada record;
                    diferencia numeric(18,2);
                    fecha_ajuste timestamptz;
                BEGIN
                    SELECT id INTO asignar_id FROM flexpos.cuentas_financieras
                    WHERE tipo = 'Otro' AND nombre = 'Cuenta por asignar' LIMIT 1;
                    SELECT COALESCE(SUM(CASE WHEN sentido = 'Entrada' THEN importe ELSE -importe END), 0)
                      INTO saldo_asignar FROM flexpos.movimientos_financieros
                     WHERE cuenta_financiera_id = asignar_id;
                    IF saldo_asignar < 0 THEN
                        RAISE EXCEPTION 'La migración financiera se canceló: Cuenta por asignar quedaría en saldo negativo (%).', saldo_asignar;
                    END IF;

                    SELECT id INTO efectivo_id FROM flexpos.cuentas_financieras
                    WHERE tipo = 'Efectivo' AND nombre = 'Caja principal' LIMIT 1;
                    SELECT * INTO caja_abierta FROM flexpos.cajas WHERE estado = 'Abierta'
                    ORDER BY fecha_apertura_utc DESC, id DESC LIMIT 1;
                    IF FOUND THEN
                        SELECT COALESCE(SUM(CASE WHEN sentido = 'Entrada' THEN importe ELSE -importe END), 0)
                          INTO saldo_efectivo FROM flexpos.movimientos_financieros
                         WHERE caja_id = caja_abierta.id
                           AND tipo NOT IN ('AjusteApertura', 'AjusteCierre', 'AjusteMigracion');
                        efectivo_objetivo := caja_abierta.efectivo_apertura + saldo_efectivo;
                        fecha_ajuste := now();
                    ELSE
                        SELECT * INTO caja_cerrada FROM flexpos.cajas WHERE estado = 'Cerrada'
                        ORDER BY fecha_cierre_utc DESC NULLS LAST, id DESC LIMIT 1;
                        efectivo_objetivo := COALESCE(caja_cerrada.efectivo_contado_cierre,
                                                      caja_cerrada.efectivo_esperado_cierre, 0);
                        fecha_ajuste := COALESCE(caja_cerrada.fecha_cierre_utc, now());
                    END IF;
                    SELECT COALESCE(SUM(CASE WHEN sentido = 'Entrada' THEN importe ELSE -importe END), 0)
                      INTO saldo_efectivo FROM flexpos.movimientos_financieros
                     WHERE cuenta_financiera_id = efectivo_id;
                    diferencia := efectivo_objetivo - saldo_efectivo;
                    IF diferencia <> 0 THEN
                        INSERT INTO flexpos.movimientos_financieros
                            (id, cuenta_financiera_id, tipo, sentido, importe, codigo_moneda, concepto,
                             fecha_utc, migrado, fecha_creacion_utc)
                        SELECT flexpos.generar_uuid_v7_migracion(), efectivo_id, 'AjusteMigracion',
                               CASE WHEN diferencia > 0 THEN 'Entrada' ELSE 'Salida' END,
                               abs(diferencia), codigo_moneda, 'Ajuste de saldo inicial migrado',
                               fecha_ajuste, true, fecha_ajuste
                        FROM flexpos.cuentas_financieras WHERE id = efectivo_id;
                    END IF;
                END
                $$;

                CREATE OR REPLACE FUNCTION flexpos.movimientos_financieros_inmutables()
                RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'Los movimientos financieros son inmutables; registre un movimiento de corrección.';
                END
                $$;
                CREATE TRIGGER trg_movimientos_financieros_inmutables
                BEFORE UPDATE OR DELETE ON flexpos.movimientos_financieros
                FOR EACH ROW EXECUTE FUNCTION flexpos.movimientos_financieros_inmutables();
                DROP FUNCTION flexpos.generar_uuid_v7_migracion();
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_cajas_cuentas_financieras_cuenta_financiera_id",
                schema: "flexpos",
                table: "cajas",
                column: "cuenta_financiera_id",
                principalSchema: "flexpos",
                principalTable: "cuentas_financieras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_metodos_pago_cuentas_financieras_cuenta_financiera_id",
                schema: "flexpos",
                table: "metodos_pago",
                column: "cuenta_financiera_id",
                principalSchema: "flexpos",
                principalTable: "cuentas_financieras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pagos_devolucion_venta_cuentas_financieras_cuenta_financier~",
                schema: "flexpos",
                table: "pagos_devolucion_venta",
                column: "cuenta_financiera_id",
                principalSchema: "flexpos",
                principalTable: "cuentas_financieras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pagos_venta_cuentas_financieras_cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_venta",
                column: "cuenta_financiera_id",
                principalSchema: "flexpos",
                principalTable: "cuentas_financieras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                ALTER TABLE flexpos.cuentas_financieras ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.movimientos_financieros ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.transferencias_financieras ENABLE ROW LEVEL SECURITY;

                DO $flexpos_rls_fase7$
                BEGIN
                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'REVOKE ALL ON TABLE flexpos.cuentas_financieras FROM flexpos_api';
                        EXECUTE 'REVOKE ALL ON TABLE flexpos.movimientos_financieros FROM flexpos_api';
                        EXECUTE 'REVOKE ALL ON TABLE flexpos.transferencias_financieras FROM flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.cuentas_financieras TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.movimientos_financieros TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.transferencias_financieras TO flexpos_api';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.cuentas_financieras FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.cuentas_financieras FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.cuentas_financieras FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.movimientos_financieros FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.movimientos_financieros FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.transferencias_financieras FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.transferencias_financieras FOR INSERT TO flexpos_api WITH CHECK (true)';
                    END IF;
                END
                $flexpos_rls_fase7$;
                """);

            migrationBuilder.DropTable(
                name: "movimientos_caja",
                schema: "flexpos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_movimientos_financieros_inmutables ON flexpos.movimientos_financieros;
                DROP FUNCTION IF EXISTS flexpos.movimientos_financieros_inmutables();
                CREATE TEMP TABLE movimientos_caja_revertidos ON COMMIT DROP AS
                SELECT id, caja_id, codigo_moneda, concepto, creado_por_id, devolucion_venta_id,
                       fecha_utc AS fecha_creacion_utc, fecha_modificacion_utc, importe,
                       liquidacion_comision_id, modificado_por_id, tipo, venta_id
                FROM flexpos.movimientos_financieros
                WHERE caja_id IS NOT NULL
                  AND tipo IN ('PagoVenta', 'PagoCompra', 'PagoComision', 'Reembolso', 'IngresoManual', 'EgresoManual');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_cajas_cuentas_financieras_cuenta_financiera_id",
                schema: "flexpos",
                table: "cajas");

            migrationBuilder.DropForeignKey(
                name: "FK_metodos_pago_cuentas_financieras_cuenta_financiera_id",
                schema: "flexpos",
                table: "metodos_pago");

            migrationBuilder.DropForeignKey(
                name: "FK_pagos_devolucion_venta_cuentas_financieras_cuenta_financier~",
                schema: "flexpos",
                table: "pagos_devolucion_venta");

            migrationBuilder.DropForeignKey(
                name: "FK_pagos_venta_cuentas_financieras_cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_venta");

            migrationBuilder.DropTable(
                name: "movimientos_financieros",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "transferencias_financieras",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "cuentas_financieras",
                schema: "flexpos");

            migrationBuilder.DropIndex(
                name: "IX_pagos_venta_cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_venta");

            migrationBuilder.DropIndex(
                name: "IX_pagos_devolucion_venta_cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_devolucion_venta");

            migrationBuilder.DropIndex(
                name: "IX_metodos_pago_cuenta_financiera_id",
                schema: "flexpos",
                table: "metodos_pago");

            migrationBuilder.DropIndex(
                name: "IX_cajas_cuenta_financiera_id",
                schema: "flexpos",
                table: "cajas");

            migrationBuilder.DropColumn(
                name: "cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_venta");

            migrationBuilder.DropColumn(
                name: "cuenta_financiera_nombre",
                schema: "flexpos",
                table: "pagos_venta");

            migrationBuilder.DropColumn(
                name: "cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_devolucion_venta");

            migrationBuilder.DropColumn(
                name: "cuenta_financiera_nombre",
                schema: "flexpos",
                table: "pagos_devolucion_venta");

            migrationBuilder.DropColumn(
                name: "cuenta_financiera_id",
                schema: "flexpos",
                table: "metodos_pago");

            migrationBuilder.DropColumn(
                name: "cuenta_financiera_id",
                schema: "flexpos",
                table: "cajas");

            migrationBuilder.CreateTable(
                name: "movimientos_caja",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    caja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    concepto = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    devolucion_venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    liquidacion_comision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_caja", x => x.id);
                    table.CheckConstraint("ck_movimientos_caja_importe", "importe > 0");
                    table.ForeignKey(
                        name: "FK_movimientos_caja_cajas_caja_id",
                        column: x => x.caja_id,
                        principalSchema: "flexpos",
                        principalTable: "cajas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_devoluciones_venta_devolucion_venta_id",
                        column: x => x.devolucion_venta_id,
                        principalSchema: "flexpos",
                        principalTable: "devoluciones_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_liquidaciones_comision_liquidacion_comisio~",
                        column: x => x.liquidacion_comision_id,
                        principalSchema: "flexpos",
                        principalTable: "liquidaciones_comision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "flexpos",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_devolucion_venta_id",
                schema: "flexpos",
                table: "movimientos_caja",
                column: "devolucion_venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_caja_fecha",
                schema: "flexpos",
                table: "movimientos_caja",
                columns: new[] { "caja_id", "fecha_creacion_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_liquidacion_comision_id",
                schema: "flexpos",
                table: "movimientos_caja",
                column: "liquidacion_comision_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_venta_id",
                schema: "flexpos",
                table: "movimientos_caja",
                column: "venta_id");

            migrationBuilder.Sql("""
                INSERT INTO flexpos.movimientos_caja
                    (id, caja_id, codigo_moneda, concepto, creado_por_id, devolucion_venta_id,
                     fecha_creacion_utc, fecha_modificacion_utc, importe, liquidacion_comision_id,
                     modificado_por_id, tipo, venta_id)
                SELECT id, caja_id, codigo_moneda, concepto, creado_por_id, devolucion_venta_id,
                       fecha_creacion_utc, fecha_modificacion_utc, importe, liquidacion_comision_id,
                       modificado_por_id, tipo, venta_id
                FROM pg_temp.movimientos_caja_revertidos;

                ALTER TABLE flexpos.movimientos_caja ENABLE ROW LEVEL SECURITY;

                DO $flexpos_rls_fase7_down$
                BEGIN
                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'REVOKE ALL ON TABLE flexpos.movimientos_caja FROM flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.movimientos_caja TO flexpos_api';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.movimientos_caja FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.movimientos_caja FOR INSERT TO flexpos_api WITH CHECK (true)';
                    END IF;
                END
                $flexpos_rls_fase7_down$;
                """);
        }
    }
}
