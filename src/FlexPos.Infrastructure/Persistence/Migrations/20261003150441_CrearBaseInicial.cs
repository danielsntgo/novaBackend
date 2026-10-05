using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlexPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CrearBaseInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "flexpos");

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    nombre = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    nombre_normalizado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    sello_concurrencia = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    nombre_usuario = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    nombre_usuario_normalizado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correo_normalizado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    correo_confirmado = table.Column<bool>(type: "boolean", nullable: false),
                    hash_contrasena = table.Column<string>(type: "text", nullable: true),
                    sello_seguridad = table.Column<string>(type: "text", nullable: true),
                    sello_concurrencia = table.Column<string>(type: "text", nullable: true),
                    telefono = table.Column<string>(type: "text", nullable: true),
                    telefono_confirmado = table.Column<bool>(type: "boolean", nullable: false),
                    doble_factor_habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    fin_bloqueo_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    bloqueo_habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reclamaciones_rol",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: true),
                    valor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reclamaciones_rol", x => x.id);
                    table.ForeignKey(
                        name: "FK_reclamaciones_rol_roles_rol_id",
                        column: x => x.rol_id,
                        principalSchema: "flexpos",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inicios_sesion_externos",
                schema: "flexpos",
                columns: table => new
                {
                    proveedor = table.Column<string>(type: "text", nullable: false),
                    clave_proveedor = table.Column<string>(type: "text", nullable: false),
                    nombre_proveedor = table.Column<string>(type: "text", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inicios_sesion_externos", x => new { x.proveedor, x.clave_proveedor });
                    table.ForeignKey(
                        name: "FK_inicios_sesion_externos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "flexpos",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reclamaciones_usuario",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: true),
                    valor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reclamaciones_usuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_reclamaciones_usuario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "flexpos",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tokens_renovacion",
                schema: "flexpos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sello_seguridad = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    creado_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    vence_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revocado_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reemplazado_por_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    fecha_creacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_modificacion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tokens_renovacion", x => x.id);
                    table.ForeignKey(
                        name: "FK_tokens_renovacion_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "flexpos",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tokens_usuario",
                schema: "flexpos",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proveedor = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    valor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tokens_usuario", x => new { x.usuario_id, x.proveedor, x.nombre });
                    table.ForeignKey(
                        name: "FK_tokens_usuario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "flexpos",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_roles",
                schema: "flexpos",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_roles", x => new { x.usuario_id, x.rol_id });
                    table.ForeignKey(
                        name: "FK_usuarios_roles_roles_rol_id",
                        column: x => x.rol_id,
                        principalSchema: "flexpos",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuarios_roles_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "flexpos",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                DO $flexpos_rls$
                DECLARE tabla record;
                BEGIN
                    FOR tabla IN
                        SELECT tablename FROM pg_tables WHERE schemaname = 'flexpos'
                    LOOP
                        EXECUTE format('ALTER TABLE flexpos.%I ENABLE ROW LEVEL SECURITY', tabla.tablename);
                    END LOOP;

                    IF to_regrole('flexpos_api') IS NOT NULL THEN
                        EXECUTE 'GRANT USAGE ON SCHEMA flexpos TO flexpos_api';

                        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.usuarios TO flexpos_api';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.usuarios FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.usuarios FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.usuarios FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';

                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.roles TO flexpos_api';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.roles FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.roles FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'GRANT SELECT, INSERT ON TABLE flexpos.usuarios_roles TO flexpos_api';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.usuarios_roles FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.usuarios_roles FOR INSERT TO flexpos_api WITH CHECK (true)';

                        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE flexpos.tokens_renovacion TO flexpos_api';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_select ON flexpos.tokens_renovacion FOR SELECT TO flexpos_api USING (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.tokens_renovacion FOR INSERT TO flexpos_api WITH CHECK (true)';
                        EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.tokens_renovacion FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)';
                    END IF;
                END
                $flexpos_rls$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_inicios_sesion_externos_usuario_id",
                schema: "flexpos",
                table: "inicios_sesion_externos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_reclamaciones_rol_rol_id",
                schema: "flexpos",
                table: "reclamaciones_rol",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "IX_reclamaciones_usuario_usuario_id",
                schema: "flexpos",
                table: "reclamaciones_usuario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "flexpos",
                table: "roles",
                column: "nombre_normalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tokens_renovacion_hash",
                schema: "flexpos",
                table: "tokens_renovacion",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tokens_renovacion_usuario_activos",
                schema: "flexpos",
                table: "tokens_renovacion",
                columns: new[] { "usuario_id", "revocado_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_correo_normalizado",
                schema: "flexpos",
                table: "usuarios",
                column: "correo_normalizado");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "flexpos",
                table: "usuarios",
                column: "nombre_usuario_normalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_roles_rol_id",
                schema: "flexpos",
                table: "usuarios_roles",
                column: "rol_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inicios_sesion_externos",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "reclamaciones_rol",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "reclamaciones_usuario",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "tokens_renovacion",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "tokens_usuario",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "usuarios_roles",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "flexpos");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "flexpos");
        }
    }
}
