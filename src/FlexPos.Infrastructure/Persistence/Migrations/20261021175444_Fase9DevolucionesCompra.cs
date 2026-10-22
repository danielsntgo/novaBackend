using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase9DevolucionesCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_anulacion_utc",
                schema: "flexpos",
                table: "compras",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_anulacion",
                schema: "flexpos",
                table: "compras",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_devuelto",
                schema: "flexpos",
                table: "compras",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "devoluciones_compra",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    compra_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_devoluciones_compra", x => x.id);
                    table.CheckConstraint("ck_devoluciones_compra_importes", "importe_lineas >= 0 AND importe_reintegrado >= 0");
                    table.ForeignKey(
                        name: "FK_devoluciones_compra_compras_compra_id",
                        column: x => x.compra_id,
                        principalSchema: "flexpos",
                        principalTable: "compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalles_devolucion_compra",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    devolucion_compra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    detalle_compra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalles_devolucion_compra", x => x.id);
                    table.CheckConstraint("ck_detalles_devolucion_compra_cantidad", "cantidad > 0");
                    table.CheckConstraint("ck_detalles_devolucion_compra_importe", "importe >= 0");
                    table.ForeignKey(
                        name: "FK_detalles_devolucion_compra_detalles_compra_detalle_compra_id",
                        column: x => x.detalle_compra_id,
                        principalSchema: "flexpos",
                        principalTable: "detalles_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalles_devolucion_compra_devoluciones_compra_devolucion_c~",
                        column: x => x.devolucion_compra_id,
                        principalSchema: "flexpos",
                        principalTable: "devoluciones_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagos_devolucion_compra",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    devolucion_compra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo_pago_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo_pago_nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    es_efectivo = table.Column<bool>(type: "boolean", nullable: false),
                    cuenta_financiera_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cuenta_financiera_nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
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
                    table.PrimaryKey("PK_pagos_devolucion_compra", x => x.id);
                    table.CheckConstraint("ck_pagos_devolucion_compra_importe", "importe > 0");
                    table.ForeignKey(
                        name: "FK_pagos_devolucion_compra_cuentas_financieras_cuenta_financie~",
                        column: x => x.cuenta_financiera_id,
                        principalSchema: "flexpos",
                        principalTable: "cuentas_financieras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_devolucion_compra_devoluciones_compra_devolucion_comp~",
                        column: x => x.devolucion_compra_id,
                        principalSchema: "flexpos",
                        principalTable: "devoluciones_compra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagos_devolucion_compra_metodos_pago_metodo_pago_id",
                        column: x => x.metodo_pago_id,
                        principalSchema: "flexpos",
                        principalTable: "metodos_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_total_devuelto",
                schema: "flexpos",
                table: "compras",
                sql: "total_devuelto >= 0 AND total_devuelto <= total_importe");

            migrationBuilder.CreateIndex(
                name: "IX_detalles_devolucion_compra_detalle_compra_id",
                schema: "flexpos",
                table: "detalles_devolucion_compra",
                column: "detalle_compra_id");

            migrationBuilder.CreateIndex(
                name: "ux_detalles_devolucion_compra_linea",
                schema: "flexpos",
                table: "detalles_devolucion_compra",
                columns: new[] { "devolucion_compra_id", "detalle_compra_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devoluciones_compra_compra_fecha",
                schema: "flexpos",
                table: "devoluciones_compra",
                columns: new[] { "compra_id", "fecha_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_pagos_devolucion_compra_cuenta_financiera_id",
                schema: "flexpos",
                table: "pagos_devolucion_compra",
                column: "cuenta_financiera_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_devolucion_compra_devolucion",
                schema: "flexpos",
                table: "pagos_devolucion_compra",
                column: "devolucion_compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_devolucion_compra_metodo_pago_id",
                schema: "flexpos",
                table: "pagos_devolucion_compra",
                column: "metodo_pago_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detalles_devolucion_compra",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "pagos_devolucion_compra",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "devoluciones_compra",
                schema: "flexpos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_total_devuelto",
                schema: "flexpos",
                table: "compras");

            migrationBuilder.DropColumn(
                name: "fecha_anulacion_utc",
                schema: "flexpos",
                table: "compras");

            migrationBuilder.DropColumn(
                name: "motivo_anulacion",
                schema: "flexpos",
                table: "compras");

            migrationBuilder.DropColumn(
                name: "total_devuelto",
                schema: "flexpos",
                table: "compras");
        }
    }
}
