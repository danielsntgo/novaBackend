-- Ejecutar con un usuario administrador de la base de datos.
-- Crear primero los roles flexpos_migraciones y flexpos_api como roles LOGIN,
-- sin privilegios administrativos y sin BYPASSRLS. No incluir contrasenas aqui.

ALTER ROLE flexpos_migraciones NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS NOINHERIT;
ALTER ROLE flexpos_api NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS NOINHERIT;

CREATE SCHEMA IF NOT EXISTS flexpos AUTHORIZATION flexpos_migraciones;
REVOKE ALL ON SCHEMA flexpos FROM PUBLIC;
GRANT USAGE, CREATE ON SCHEMA flexpos TO flexpos_migraciones;
GRANT USAGE ON SCHEMA flexpos TO flexpos_api;

DO $flexpos_permisos$
DECLARE
    tabla text;
    operacion text;
BEGIN
    EXECUTE 'REVOKE ALL ON ALL TABLES IN SCHEMA flexpos FROM flexpos_api, PUBLIC';
    EXECUTE 'REVOKE ALL ON ALL SEQUENCES IN SCHEMA flexpos FROM flexpos_api, PUBLIC';

    FOR tabla IN
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = 'flexpos'
    LOOP
        EXECUTE format('ALTER TABLE flexpos.%I ENABLE ROW LEVEL SECURITY', tabla);
    END LOOP;

    FOR tabla, operacion IN
        SELECT * FROM (VALUES
            ('usuarios', 'select'), ('usuarios', 'insert'), ('usuarios', 'update'),
            ('roles', 'select'), ('roles', 'insert'),
            ('usuarios_roles', 'select'), ('usuarios_roles', 'insert'),
            ('tokens_renovacion', 'select'), ('tokens_renovacion', 'insert'), ('tokens_renovacion', 'update'),
            ('configuracion_negocio', 'select'), ('configuracion_negocio', 'insert'), ('configuracion_negocio', 'update'),
            ('impuestos', 'select'), ('impuestos', 'insert'), ('impuestos', 'update'),
            ('metodos_pago', 'select'), ('metodos_pago', 'insert'), ('metodos_pago', 'update'),
            ('numeraciones_documento', 'select'), ('numeraciones_documento', 'insert'), ('numeraciones_documento', 'update'),
            ('clientes', 'select'), ('clientes', 'insert'), ('clientes', 'update'),
            ('empleados', 'select'), ('empleados', 'insert'), ('empleados', 'update'),
            ('horarios_semanales_empleado', 'select'), ('horarios_semanales_empleado', 'insert'),
            ('horarios_semanales_empleado', 'update'), ('horarios_semanales_empleado', 'delete'),
            ('servicios', 'select'), ('servicios', 'insert'), ('servicios', 'update'),
            ('citas', 'select'), ('citas', 'insert'), ('citas', 'update'),
            ('articulos_inventario', 'select'), ('articulos_inventario', 'insert'), ('articulos_inventario', 'update'),
            ('movimientos_inventario', 'select'), ('movimientos_inventario', 'insert'),
            ('proveedores', 'select'), ('proveedores', 'insert'), ('proveedores', 'update'),
            ('compras', 'select'), ('compras', 'insert'), ('compras', 'update'),
            ('detalles_compra', 'select'), ('detalles_compra', 'insert'), ('detalles_compra', 'update'),
            ('cajas', 'select'), ('cajas', 'insert'), ('cajas', 'update'),
            ('movimientos_caja', 'select'), ('movimientos_caja', 'insert'),
            ('ventas', 'select'), ('ventas', 'insert'), ('ventas', 'update'),
            ('detalles_venta', 'select'), ('detalles_venta', 'insert'),
            ('pagos_venta', 'select'), ('pagos_venta', 'insert'),
            ('documentos_venta', 'select'), ('documentos_venta', 'insert'),
            ('devoluciones_venta', 'select'), ('devoluciones_venta', 'insert'),
            ('detalles_devolucion_venta', 'select'), ('detalles_devolucion_venta', 'insert'),
            ('pagos_devolucion_venta', 'select'), ('pagos_devolucion_venta', 'insert'),
            ('reglas_comision', 'select'), ('reglas_comision', 'insert'), ('reglas_comision', 'update'),
            ('comisiones', 'select'), ('comisiones', 'insert'), ('comisiones', 'update'),
            ('liquidaciones_comision', 'select'), ('liquidaciones_comision', 'insert')
        ) AS permisos(tabla, operacion)
    LOOP
        IF to_regclass(format('flexpos.%I', tabla)) IS NULL THEN
            CONTINUE;
        END IF;

        EXECUTE format('GRANT %s ON TABLE flexpos.%I TO flexpos_api', upper(operacion), tabla);
        EXECUTE format('DROP POLICY IF EXISTS pol_flexpos_api_%s ON flexpos.%I', operacion, tabla);

        IF operacion = 'select' THEN
            EXECUTE format('CREATE POLICY pol_flexpos_api_select ON flexpos.%I FOR SELECT TO flexpos_api USING (true)', tabla);
        ELSIF operacion = 'insert' AND tabla = 'compras' THEN
            EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.compras FOR INSERT TO flexpos_api WITH CHECK (estado = ''Borrador'')';
        ELSIF operacion = 'insert' AND tabla = 'detalles_compra' THEN
            EXECUTE 'CREATE POLICY pol_flexpos_api_insert ON flexpos.detalles_compra FOR INSERT TO flexpos_api WITH CHECK (EXISTS (SELECT 1 FROM flexpos.compras c WHERE c.id = compra_id AND c.estado = ''Borrador''))';
        ELSIF operacion = 'insert' THEN
            EXECUTE format('CREATE POLICY pol_flexpos_api_insert ON flexpos.%I FOR INSERT TO flexpos_api WITH CHECK (true)', tabla);
        ELSIF tabla = 'compras' THEN
            EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.compras FOR UPDATE TO flexpos_api USING (estado = ''Borrador'') WITH CHECK (estado IN (''Borrador'', ''Confirmada''))';
        ELSIF tabla = 'detalles_compra' THEN
            EXECUTE 'CREATE POLICY pol_flexpos_api_update ON flexpos.detalles_compra FOR UPDATE TO flexpos_api USING (EXISTS (SELECT 1 FROM flexpos.compras c WHERE c.id = compra_id AND c.estado = ''Borrador'')) WITH CHECK (EXISTS (SELECT 1 FROM flexpos.compras c WHERE c.id = compra_id AND c.estado = ''Borrador''))';
        ELSIF operacion = 'delete' THEN
            EXECUTE format('CREATE POLICY pol_flexpos_api_delete ON flexpos.%I FOR DELETE TO flexpos_api USING (true)', tabla);
        ELSE
            EXECUTE format('CREATE POLICY pol_flexpos_api_update ON flexpos.%I FOR UPDATE TO flexpos_api USING (true) WITH CHECK (true)', tabla);
        END IF;
    END LOOP;

    IF to_regclass('flexpos.historial_migraciones') IS NOT NULL THEN
        EXECUTE 'REVOKE ALL ON TABLE flexpos.historial_migraciones FROM flexpos_api';
    END IF;
END
$flexpos_permisos$;
