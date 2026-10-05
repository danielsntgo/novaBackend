using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fase4Citas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "revision_horario",
                schema: "flexpos",
                table: "empleados",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "citas",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servicio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    empleado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_inicio_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duracion_minutos = table.Column<int>(type: "integer", nullable: false),
                    fecha_fin_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citas", x => x.id);
                    table.CheckConstraint("ck_citas_duracion", "duracion_minutos BETWEEN 1 AND 1440");
                    table.CheckConstraint("ck_citas_estado", "estado IN ('Pendiente', 'Confirmada', 'Atendida', 'Cancelada', 'NoAsistio')");
                    table.ForeignKey(
                        name: "FK_citas_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "flexpos",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_citas_empleados_empleado_id",
                        column: x => x.empleado_id,
                        principalSchema: "flexpos",
                        principalTable: "empleados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_citas_servicios_servicio_id",
                        column: x => x.servicio_id,
                        principalSchema: "flexpos",
                        principalTable: "servicios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "horarios_semanales_empleado",
                schema: "flexpos",
                columns: table => new
                {
                    dia_semana = table.Column<int>(type: "integer", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    empleado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horarios_semanales_empleado", x => new { x.empleado_id, x.dia_semana, x.hora_inicio });
                    table.ForeignKey(
                        name: "FK_horarios_semanales_empleado_empleados_empleado_id",
                        column: x => x.empleado_id,
                        principalSchema: "flexpos",
                        principalTable: "empleados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_citas_cliente_inicio",
                schema: "flexpos",
                table: "citas",
                columns: new[] { "cliente_id", "fecha_inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_citas_empleado_inicio",
                schema: "flexpos",
                table: "citas",
                columns: new[] { "empleado_id", "fecha_inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_citas_estado_inicio",
                schema: "flexpos",
                table: "citas",
                columns: new[] { "estado", "fecha_inicio_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_citas_servicio_id",
                schema: "flexpos",
                table: "citas",
                column: "servicio_id");

            migrationBuilder.CreateIndex(
                name: "ix_horarios_empleado_dia_fin",
                schema: "flexpos",
                table: "horarios_semanales_empleado",
                columns: new[] { "empleado_id", "dia_semana", "hora_fin" });

            migrationBuilder.Sql("""
                ALTER TABLE flexpos.horarios_semanales_empleado ENABLE ROW LEVEL SECURITY;
                ALTER TABLE flexpos.citas ENABLE ROW LEVEL SECURITY;

                DO $flexpos_rls_fase4$
                BEGIN
                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE flexpos.horarios_semanales_empleado TO flexpos_api';
                        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.citas TO flexpos_api';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.horarios_semanales_empleado FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.horarios_semanales_empleado FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.horarios_semanales_empleado FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_delete ON flexpos.horarios_semanales_empleado FOR DELETE TO flexpos_api USING (true)';

                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.citas FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.citas FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.citas FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';
                    END IF;
                END
                $flexpos_rls_fase4$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "citas",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "horarios_semanales_empleado",
                schema: "flexpos");

            migrationBuilder.DropColumn(
                name: "revision_horario",
                schema: "flexpos",
                table: "empleados");
        }
    }
}
