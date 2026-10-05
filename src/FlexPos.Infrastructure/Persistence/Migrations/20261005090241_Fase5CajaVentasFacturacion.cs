using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase5CajaVentasFacturacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_articulos_inventario_existencia",
                schema: "flexpos",
                table: "articulos_inventario");

            migrationBuilder.AddColumn<Guid>(
                name: "detalle_venta_id",
                schema: "flexpos",
                table: "movimientos_inventario",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "venta_id",
                schema: "flexpos",
                table: "movimientos_inventario",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_efectivo",
                schema: "flexpos",
                table: "metodos_pago",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "exigir_empleado_venta_servicio",
                schema: "flexpos",
                table: "configuracion_negocio",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "facturacion_habilitada",
                schema: "flexpos",
                table: "configuracion_negocio",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "permitir_saldos_pendientes",
                schema: "flexpos",
                table: "configuracion_negocio",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "cajas",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    efectivo_apertura = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fecha_apertura_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    usuario_apertura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    efectivo_esperado_cierre = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    efectivo_contado_cierre = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    diferencia_cierre = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    fecha_cierre_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    usuario_cierre_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cajas", x => x.id);
                    table.CheckConstraint("ck_cajas_estado", "estado IN ('Abierta', 'Cerrada')");
                });

            migrationBuilder.CreateTable(
                name: "ventas",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    caja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cliente_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cliente_documento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fecha_venta_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tipo_descuento_general = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    valor_descuento_general = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_lineas = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_general = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_pagado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_devuelto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_reintegrado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_pendiente = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    motivo_anulacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    anulada_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_anulacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas", x => x.id);
                    table.CheckConstraint("ck_ventas_estado", "estado IN ('Finalizada', 'ParcialmenteDevuelta', 'Devuelta', 'Anulada')");
                    table.ForeignKey(
                        name: "FK_ventas_cajas_caja_id",
                        column: x => x.caja_id,
                        principalSchema: "flexpos",
                        principalTable: "cajas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "flexpos",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalles_venta",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    articulo_inventario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    servicio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    empleado_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    unidad = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_inventario_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    tipo_descuento_linea = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    valor_descuento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    importe_bruto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuento_general_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto_nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    impuesto_porcentaje = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    impuesto_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalles_venta", x => x.id);
                    table.CheckConstraint("ck_detalles_venta_cantidad", "cantidad > 0");
                    table.ForeignKey(
                        name: "FK_detalles_venta_articulos_inventario_articulo_inventario_id",
                        column: x => x.articulo_inventario_id,
                        principalSchema: "flexpos",
                        principalTable: "articulos_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalles_venta_empleados_empleado_id",
                        column: x => x.empleado_id,
                        principalSchema: "flexpos",
                        principalTable: "empleados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalles_venta_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalSchema: "flexpos",
                        principalTable: "servicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalles_venta_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "flexpos",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devoluciones_venta",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    importe_lineas = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    importe_reintegrado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devoluciones_venta", x => x.id);
                    table.ForeignKey(
                        name: "FK_devoluciones_venta_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "flexpos",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documentos_venta",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    prefijo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    numero = table.Column<long>(type: "bigint", nullable: false),
                    numero_completo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fecha_emision_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    nombre_comercial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    razon_social = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    identificacion_fiscal = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    cliente_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cliente_documento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descuentos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    impuestos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_venta", x => x.id);
                    table.ForeignKey(
                        name: "FK_documentos_venta_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "flexpos",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_venta",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    caja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo_pago_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo_pago_nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    es_efectivo = table.Column<bool>(type: "boolean", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_venta", x => x.id);
                    table.CheckConstraint("ck_pagos_venta_importe", "importe > 0");
                    table.ForeignKey(
                        name: "FK_pagos_venta_cajas_caja_id",
                        column: x => x.caja_id,
                        principalSchema: "flexpos",
                        principalTable: "cajas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_venta_metodos_pago_metodo_pago_id",
                        column: x => x.metodo_pago_id,
                        principalSchema: "flexpos",
                        principalTable: "metodos_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_venta_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "flexpos",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalles_devolucion_venta",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    devolucion_venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    detalle_venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalles_devolucion_venta", x => x.id);
                    table.CheckConstraint("ck_detalles_devolucion_cantidad", "cantidad > 0");
                    table.ForeignKey(
                        name: "FK_detalles_devolucion_venta_detalles_venta_detalle_venta_id",
                        column: x => x.detalle_venta_id,
                        principalSchema: "flexpos",
                        principalTable: "detalles_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalles_devolucion_venta_devoluciones_venta_devolucion_ven~",
                        column: x => x.devolucion_venta_id,
                        principalSchema: "flexpos",
                        principalTable: "devoluciones_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_caja",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    caja_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    concepto = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    devolucion_venta_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                        name: "FK_movimientos_caja_ventas_venta_id",
                        column: x => x.venta_id,
                        principalSchema: "flexpos",
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_devolucion_venta",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    devolucion_venta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo_pago_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo_pago_nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    es_efectivo = table.Column<bool>(type: "boolean", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    referencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_devolucion_venta", x => x.id);
                    table.CheckConstraint("ck_pagos_devolucion_importe", "importe > 0");
                    table.ForeignKey(
                        name: "FK_pagos_devolucion_venta_devoluciones_venta_devolucion_venta_~",
                        column: x => x.devolucion_venta_id,
                        principalSchema: "flexpos",
                        principalTable: "devoluciones_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_devolucion_venta_metodos_pago_metodo_pago_id",
                        column: x => x.metodo_pago_id,
                        principalSchema: "flexpos",
                        principalTable: "metodos_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_detalle_venta_id",
                schema: "flexpos",
                table: "movimientos_inventario",
                column: "detalle_venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_venta",
                schema: "flexpos",
                table: "movimientos_inventario",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ux_metodos_pago_unico_efectivo",
                schema: "flexpos",
                table: "metodos_pago",
                column: "configuracion_negocio_id",
                unique: true,
                filter: "es_efectivo = true");

            migrationBuilder.CreateIndex(
                name: "ix_cajas_fecha_apertura",
                schema: "flexpos",
                table: "cajas",
                column: "fecha_apertura_utc");

            migrationBuilder.CreateIndex(
                name: "ux_cajas_abierta_compartida",
                schema: "flexpos",
                table: "cajas",
                column: "estado",
                unique: true,
                filter: "estado = 'Abierta'");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_devolucion_venta_devolucion_venta_id",
                schema: "flexpos",
                table: "detalles_devolucion_venta",
                column: "devolucion_venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_devolucion_detalle_venta",
                schema: "flexpos",
                table: "detalles_devolucion_venta",
                column: "detalle_venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_detalles_venta_articulo",
                schema: "flexpos",
                table: "detalles_venta",
                column: "articulo_inventario_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_venta_empleado_id",
                schema: "flexpos",
                table: "detalles_venta",
                column: "empleado_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_venta_servicio_id",
                schema: "flexpos",
                table: "detalles_venta",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_venta_venta_id",
                schema: "flexpos",
                table: "detalles_venta",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_devoluciones_venta_fecha",
                schema: "flexpos",
                table: "devoluciones_venta",
                columns: new[] { "venta_id", "fecha_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_documentos_venta_numero",
                schema: "flexpos",
                table: "documentos_venta",
                columns: new[] { "tipo_documento", "numero_completo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_documentos_venta_tipo",
                schema: "flexpos",
                table: "documentos_venta",
                columns: new[] { "venta_id", "tipo_documento" },
                unique: true);

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
                name: "IX_movimientos_caja_venta_id",
                schema: "flexpos",
                table: "movimientos_caja",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_devolucion_venta_devolucion_venta_id",
                schema: "flexpos",
                table: "pagos_devolucion_venta",
                column: "devolucion_venta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_devolucion_venta_metodo_pago_id",
                schema: "flexpos",
                table: "pagos_devolucion_venta",
                column: "metodo_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_venta_caja_id",
                schema: "flexpos",
                table: "pagos_venta",
                column: "caja_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_venta_fecha",
                schema: "flexpos",
                table: "pagos_venta",
                columns: new[] { "venta_id", "fecha_creacion_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_venta_metodo_pago_id",
                schema: "flexpos",
                table: "pagos_venta",
                column: "metodo_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_caja_id",
                schema: "flexpos",
                table: "ventas",
                column: "caja_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_cliente_fecha",
                schema: "flexpos",
                table: "ventas",
                columns: new[] { "cliente_id", "fecha_venta_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_ventas_fecha_id",
                schema: "flexpos",
                table: "ventas",
                columns: new[] { "fecha_venta_utc", "id" });

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_inventario_detalles_venta_detalle_venta_id",
                schema: "flexpos",
                table: "movimientos_inventario",
                column: "detalle_venta_id",
                principalSchema: "flexpos",
                principalTable: "detalles_venta",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_inventario_ventas_venta_id",
                schema: "flexpos",
                table: "movimientos_inventario",
                column: "venta_id",
                principalSchema: "flexpos",
                principalTable: "ventas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                ALTER TABLE flexpos.cajas ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.movimientos_caja ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.ventas ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.detalles_venta ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.pagos_venta ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.documentos_venta ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.devoluciones_venta ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.detalles_devolucion_venta ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.pagos_devolucion_venta ENABLE ROW LEVEL SECURITY;

                DO $flexpos_rls_fase5$
                BEGIN
                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.cajas TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.movimientos_caja TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.ventas TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.detalles_venta TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.pagos_venta TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.documentos_venta TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.devoluciones_venta TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.detalles_devolucion_venta TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.pagos_devolucion_venta TO flexpos_api';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.cajas FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.cajas FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.cajas FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.movimientos_caja FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.movimientos_caja FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.ventas FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.ventas FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.ventas FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.detalles_venta FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.detalles_venta FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.pagos_venta FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.pagos_venta FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.documentos_venta FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.documentos_venta FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.devoluciones_venta FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.devoluciones_venta FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.detalles_devolucion_venta FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.detalles_devolucion_venta FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.pagos_devolucion_venta FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.pagos_devolucion_venta FOR INSERT TO flexpos_api WITH CHECK (true)';
                    END IF;
                END
                $flexpos_rls_fase5$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_inventario_detalles_venta_detalle_venta_id",
                schema: "flexpos",
                table: "movimientos_inventario");

            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_inventario_ventas_venta_id",
                schema: "flexpos",
                table: "movimientos_inventario");

            migrationBuilder.DropTable(
                name: "detalles_devolucion_venta",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "documentos_venta",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "movimientos_caja",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "pagos_devolucion_venta",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "pagos_venta",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "detalles_venta",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "devoluciones_venta",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "ventas",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "cajas",
                schema: "flexpos");

            migrationBuilder.DropIndex(
                name: "IX_movimientos_inventario_detalle_venta_id",
                schema: "flexpos",
                table: "movimientos_inventario");

            migrationBuilder.DropIndex(
                name: "ix_movimientos_venta",
                schema: "flexpos",
                table: "movimientos_inventario");

            migrationBuilder.DropIndex(
                name: "ux_metodos_pago_unico_efectivo",
                schema: "flexpos",
                table: "metodos_pago");

            migrationBuilder.DropColumn(
                name: "detalle_venta_id",
                schema: "flexpos",
                table: "movimientos_inventario");

            migrationBuilder.DropColumn(
                name: "venta_id",
                schema: "flexpos",
                table: "movimientos_inventario");

            migrationBuilder.DropColumn(
                name: "es_efectivo",
                schema: "flexpos",
                table: "metodos_pago");

            migrationBuilder.DropColumn(
                name: "exigir_empleado_venta_servicio",
                schema: "flexpos",
                table: "configuracion_negocio");

            migrationBuilder.DropColumn(
                name: "facturacion_habilitada",
                schema: "flexpos",
                table: "configuracion_negocio");

            migrationBuilder.DropColumn(
                name: "permitir_saldos_pendientes",
                schema: "flexpos",
                table: "configuracion_negocio");

            migrationBuilder.AddCheckConstraint(
                name: "ck_articulos_inventario_existencia",
                schema: "flexpos",
                table: "articulos_inventario",
                sql: "existencia_actual >= 0");
        }
    }
}
