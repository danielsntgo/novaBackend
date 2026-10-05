using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase2ClientesEmpleadosServicios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    documento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    documento_normalizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    telefono_normalizado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correo_normalizado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empleados",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cargo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cargo_normalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    documento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    documento_normalizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    telefono_normalizado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correo_normalizado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empleados", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "servicios",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    categoria_normalizada = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    precio_importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    precio_codigo_moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    duracion_minutos = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicios", x => x.id);
                });

            migrationBuilder.Sql("""
                DO $flexpos_rls_fase2$
                DECLARE tabla text;
                BEGIN
                    FOR tabla IN SELECT unnest(ARRAY['clientes', 'empleados', 'servicios']) LOOP
                        EXECUTE format('ALTER TABLE flexpos.%I ENABLE ROW LEVEL SECURITY', tabla);
                    END LOOP;

                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'GRANT USAGE ON SCHEMA flexpos TO flexpos_api';
                        FOR tabla IN SELECT unnest(ARRAY['clientes', 'empleados', 'servicios']) LOOP
                            EXECUTE format('GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.%I TO flexpos_api', tabla);
                            EXECUTE format('CREATE POLICY pol_flexpos_api_select ON flexpos.%I FOR SELECT TO flexpos_api USING (true)', tabla);
                            EXECUTE format('CREATE POLICY pol_flexpos_api_insert ON flexpos.%I FOR INSERT TO flexpos_api WITH CHECK (true)', tabla);
                            EXECUTE format('CREATE POLICY pol_flexpos_api_update ON flexpos.%I FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)', tabla);
                        END LOOP;
                    END IF;
                END
                $flexpos_rls_fase2$;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_clientes_activo_nombre",
                schema: "flexpos",
                table: "clientes",
                columns: new[] { "activo", "nombre_normalizado" });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_correo_prefijo",
                schema: "flexpos",
                table: "clientes",
                column: "correo_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_documento_prefijo",
                schema: "flexpos",
                table: "clientes",
                column: "documento_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_nombre_prefijo",
                schema: "flexpos",
                table: "clientes",
                column: "nombre_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_telefono_prefijo",
                schema: "flexpos",
                table: "clientes",
                column: "telefono_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_empleados_activo_nombre",
                schema: "flexpos",
                table: "empleados",
                columns: new[] { "activo", "nombre_normalizado" });

            migrationBuilder.CreateIndex(
                name: "ix_empleados_cargo_prefijo",
                schema: "flexpos",
                table: "empleados",
                column: "cargo_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_empleados_correo_prefijo",
                schema: "flexpos",
                table: "empleados",
                column: "correo_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_empleados_documento_prefijo",
                schema: "flexpos",
                table: "empleados",
                column: "documento_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_empleados_nombre_prefijo",
                schema: "flexpos",
                table: "empleados",
                column: "nombre_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_empleados_telefono_prefijo",
                schema: "flexpos",
                table: "empleados",
                column: "telefono_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_servicios_activo_nombre",
                schema: "flexpos",
                table: "servicios",
                columns: new[] { "activo", "nombre_normalizado" });

            migrationBuilder.CreateIndex(
                name: "ix_servicios_categoria_prefijo",
                schema: "flexpos",
                table: "servicios",
                column: "categoria_normalizada")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_servicios_nombre_prefijo",
                schema: "flexpos",
                table: "servicios",
                column: "nombre_normalizado")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "empleados",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "servicios",
                schema: "flexpos");
        }
    }
}
