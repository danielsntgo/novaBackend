using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConfiguracionYAdministracionUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "cambio_contrasena_obligatorio",
                schema: "flexpos",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "configuracion_negocio",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_comercial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    razon_social = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    identificacion_fiscal = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    permitir_venta_sin_stock = table.Column<bool>(type: "boolean", nullable: false),
                    comision_productos_habilitada = table.Column<bool>(type: "boolean", nullable: false),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_negocio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "impuestos",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuracion_negocio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_impuestos", x => x.id);
                    table.ForeignKey(
                        name: "FK_impuestos_configuracion_negocio_configuracion_negocio_id",
                        column: x => x.configuracion_negocio_id,
                        principalSchema: "flexpos",
                        principalTable: "configuracion_negocio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "metodos_pago",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuracion_negocio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    requiere_referencia = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metodos_pago", x => x.id);
                    table.ForeignKey(
                        name: "FK_metodos_pago_configuracion_negocio_configuracion_negocio_id",
                        column: x => x.configuracion_negocio_id,
                        principalSchema: "flexpos",
                        principalTable: "configuracion_negocio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "numeraciones_documento",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuracion_negocio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    prefijo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    siguiente_numero = table.Column<long>(type: "bigint", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_numeraciones_documento", x => x.id);
                    table.ForeignKey(
                        name: "FK_numeraciones_documento_configuracion_negocio_configuracion_~",
                        column: x => x.configuracion_negocio_id,
                        principalSchema: "flexpos",
                        principalTable: "configuracion_negocio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                DO $flexpos_rls_fase1$
                DECLARE tabla text;
                BEGIN
                    FOR tabla IN SELECT unnest(ARRAY[
                        'configuracion_negocio', 'impuestos', 'metodos_pago', 'numeraciones_documento'
                    ]) LOOP
                        EXECUTE format('ALTER TABLE flexpos.%I ENABLE ROW LEVEL SECURITY', tabla);
                    END LOOP;

                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'GRANT USAGE ON SCHEMA flexpos TO flexpos_api';
                        FOR tabla IN SELECT unnest(ARRAY[
                            'configuracion_negocio', 'impuestos', 'metodos_pago', 'numeraciones_documento'
                        ]) LOOP
                            EXECUTE format('GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.%I TO flexpos_api', tabla);
                            EXECUTE format('CREATE POLICY pol_flexpos_api_select ON flexpos.%I FOR SELECT TO flexpos_api USING (true)', tabla);
                            EXECUTE format('CREATE POLICY pol_flexpos_api_insert ON flexpos.%I FOR INSERT TO flexpos_api WITH CHECK (true)', tabla);
                            EXECUTE format('CREATE POLICY pol_flexpos_api_update ON flexpos.%I FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)', tabla);
                        END LOOP;
                    END IF;
                END
                $flexpos_rls_fase1$;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_configuracion_negocio_principal",
                schema: "flexpos",
                table: "configuracion_negocio",
                column: "es_principal",
                unique: true,
                filter: "es_principal = true");

            migrationBuilder.CreateIndex(
                name: "ux_impuestos_configuracion_nombre",
                schema: "flexpos",
                table: "impuestos",
                columns: new[] { "configuracion_negocio_id", "nombre_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_metodos_pago_configuracion_nombre",
                schema: "flexpos",
                table: "metodos_pago",
                columns: new[] { "configuracion_negocio_id", "nombre_normalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_numeraciones_documento_tipo",
                schema: "flexpos",
                table: "numeraciones_documento",
                columns: new[] { "configuracion_negocio_id", "tipo_documento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "impuestos",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "metodos_pago",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "numeraciones_documento",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "configuracion_negocio",
                schema: "flexpos");

            migrationBuilder.DropColumn(
                name: "cambio_contrasena_obligatorio",
                schema: "flexpos",
                table: "usuarios");
        }
    }
}
