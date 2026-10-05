# FlexPos Backend

API ASP.NET Core sobre .NET 10, con capas Domain, Application, Infrastructure y Api. Infrastructure usa Entity Framework Core con Npgsql; PostgreSQL es el proveedor y no hay dependencia de APIs de Supabase.

## Desarrollo sin PostgreSQL

La API puede iniciarse sin una base configurada. En ese modo no ejecuta el bootstrap de datos y los endpoints que requieren persistencia no estarán disponibles.

```powershell
dotnet build FlexPos.slnx
dotnet run --project src/FlexPos.Api
```

Swagger/OpenAPI se expone en desarrollo en `/openapi/v1.json`. La API local escucha en `http://localhost:5063` y `https://localhost:7258` según el perfil seleccionado.

## Configuración local segura

No guardar connection strings, claves JWT ni contraseñas en archivos versionados. Para desarrollo use User Secrets o variables de entorno. Ejemplo de User Secrets para la API:

```powershell
dotnet user-secrets set "Autenticacion:Jwt:ClaveFirma" "<cadena-aleatoria-de-al-menos-32-bytes>" --project src/FlexPos.Api
dotnet user-secrets set "AdministradorInicial:Correo" "admin@ejemplo.com" --project src/FlexPos.Api
dotnet user-secrets set "AdministradorInicial:Contrasena" "<contrasena-segura>" --project src/FlexPos.Api
dotnet user-secrets set "ConnectionStrings:FlexPos" "<connection-string-runtime>" --project src/FlexPos.Api
```

La contraseña del administrador solo se usa al crearlo por primera vez. Si el correo ya existe, el inicializador no cambia usuario, contraseña ni roles. Si no se configura correo y contraseña, no crea al administrador.

`ConnectionStrings:FlexPos` es la conexión de runtime. `ConnectionStrings:Migraciones` es exclusiva de EF CLI y debe apuntar al usuario de migraciones. Para guardarla localmente, use el almacén de User Secrets de Infrastructure:

```powershell
dotnet user-secrets set "ConnectionStrings:Migraciones" "<connection-string-migraciones>" --project src/FlexPos.Infrastructure
```

La clave JWT se interpreta como texto UTF-8 y debe tener como mínimo 32 bytes aleatorios. Use un gestor de secretos en producción.

## PostgreSQL y migraciones

Cuando la instancia PostgreSQL esté disponible:

1. Cree dos roles LOGIN en el proveedor: `flexpos_migraciones` y `flexpos_api`. Ninguno debe ser superusuario ni tener `BYPASSRLS`, `CREATEDB` o `CREATEROLE`; use `NOINHERIT` y no los haga miembros de roles privilegiados. Guarde sus contraseñas fuera del repositorio y configure conexiones distintas.
2. Como propietario/administrador de la base, ejecute [`01-runtime-permissions.sql`](database/postgresql/01-runtime-permissions.sql). El esquema `flexpos` queda propiedad del rol de migraciones.
3. Aplique la migración usando la conexión de migraciones:

```powershell
dotnet tool restore
dotnet ef database update --project src/FlexPos.Infrastructure --startup-project src/FlexPos.Api
```

4. Ejecute otra vez el SQL de permisos después de la migración. La migración también habilita RLS y, si existe `flexpos_api`, instala políticas y grants iniciales.
5. Configure la conexión de runtime (`ConnectionStrings:FlexPos`) y los secretos del administrador en el entorno de la API.

La API no aplica migraciones automáticamente: deben ejecutarse con el usuario separado de migraciones antes de iniciar el runtime. Cada migración nueva debe incluir habilitación de RLS, sus políticas y grants mínimos para runtime; la cuenta de migraciones conserva la propiedad necesaria para cambios de esquema. El historial `flexpos.historial_migraciones` no se concede a runtime.

El script concede solamente los permisos operativos necesarios sobre usuarios, roles, vínculos usuario-rol, tokens de renovación, configuración del negocio, clientes, empleados, horarios semanales, servicios, citas, artículos, movimientos, proveedores, compras y sus detalles, cajas, ventas, pagos y devoluciones. No concede permisos sobre el historial de migraciones ni `DELETE` sobre ventas, movimientos financieros o inventario. El runtime no puede borrar citas ni movimientos; sí puede reemplazar los intervalos horarios de un empleado. Las políticas se limitan al rol y a esas operaciones. Sus predicados permiten las filas de las tablas autorizadas porque el backend actual no establece un contexto de usuario/tenant en PostgreSQL; por tanto RLS no se presenta como aislamiento por fila entre usuarios. Al incorporar multi-tenancy o acceso directo desde clientes, habrá que añadir contexto transaccional y predicados por tenant/usuario antes de exponer esas tablas.

La API valida cada JWT contra el estado, el sello de seguridad, el rol y el requisito de cambio de contraseña actuales. Cambiar la contraseña o desactivar una recepcionista invalida sus JWT y refresh tokens previos. Las rutas quedan restringidas a Administrador por defecto; los endpoints operativos que se implementen para Recepcionista deberán declarar explícitamente la política `OperacionDiaria`.

En el Dashboard de Supabase, abra **Project Settings > Data API** (API settings) y compruebe que `flexpos` no aparece en **Exposed schemas**. No agregue este esquema a la Data API; la aplicación se conecta exclusivamente por PostgreSQL/Npgsql. La opción para exponer esquemas personalizados se describe en la [documentación oficial de Supabase](https://supabase.com/docs/guides/api/using-custom-schemas).

## Configuración y cuentas

`/api/configuracion` administra los datos del negocio, impuestos, métodos de pago y numeraciones. Los valores comerciales, moneda, impuestos y formas de pago no se inicializan con supuestos: se configuran desde la API. La numeración de factura y comprobante se guarda por separado y se consume dentro de la transacción que registra la venta y sus documentos.

`POST /api/usuarios/recepcionistas` crea exclusivamente cuentas con rol Recepcionista. Genera una contraseña temporal criptográficamente aleatoria, devuelta una sola vez en esa respuesta; no se persiste en texto claro. El primer acceso queda restringido al endpoint de cambio de contraseña. El DTO de consulta nunca devuelve contraseñas ni hashes. Los accesos para crear la cuenta inicial de Administrador siguen en User Secrets o variables seguras y son idempotentes: si el correo ya existe, no se cambia su cuenta ni credenciales.

## Clientes, empleados y servicios

Clientes se administran desde `/api/clientes` con paginación, búsqueda y desactivación lógica; Administrador y Recepcionista pueden operarlos. Empleados se administran desde `/api/empleados` y son exclusivos de Administrador. Los servicios activos se consultan en `/api/servicios` por ambos roles; administrar el catálogo, incluidos registros inactivos, requiere Administrador. Los empleados son registros de negocio separados de las cuentas de acceso Identity.

Los campos de documento, teléfono y correo son opcionales; no se presume un tipo de identificación nacional ni se imponen unicidades no definidas por el negocio. Categorías de servicio son texto configurable, sin una lista inicial hardcodeada. Los listados buscan por prefijo con texto normalizado (sin distinguir mayúsculas ni tildes) y usan índices B-tree `text_pattern_ops`. Los cambios de estado conservan el historial.

Cada precio de servicio guarda su `Dinero` con importe `numeric(18,2)` y código ISO de moneda tomado de Configuración; no se acepta una moneda por servicio que difiera de la del establecimiento. Una vez creados servicios, artículos, compras, cajas o ventas, no se permite cambiar la moneda general, para evitar reinterpretar precios y registros financieros existentes.

## Citas

`/api/empleados/{id}/horario-semanal` permite consultar y reemplazar el horario semanal del empleado (solo Administrador). Cada día puede tener varios intervalos para representar pausas; se rechazan intervalos invertidos o superpuestos. `diaSemana` usa `0` para domingo hasta `6` para sábado, y una lista vacía significa que el empleado no tiene horas laborales configuradas. Los cambios de horario aplican a nuevas citas y reprogramaciones; no cancelan reservas existentes.

`/api/citas` permite registrar y listar citas. Administrador y Recepcionista pueden consultar la agenda, crear citas, reprogramarlas y cambiar sus estados. Una cita toma la duración del servicio al reservar y la conserva aunque el catálogo cambie después. Solo se permite reservar dentro de un intervalo continuo del horario del empleado. `Pendiente` puede pasar a `Confirmada`, `Atendida`, `Cancelada` o `NoAsistio`; `Confirmada` puede pasar a `Atendida`, `Cancelada` o `NoAsistio`. Los estados finales no se reabren. Una cita cancelada deja libre su horario; las demás conservan el tramo reservado.

Las fechas enviadas a creación y reprogramación deben incluir el desfase UTC local del establecimiento, por ejemplo `2026-10-05T10:00:00-05:00`. El backend interpreta la fecha y hora con ese desfase para validar el día y horario local, y almacena el instante en UTC; no infiere una zona horaria del servidor. Los filtros de agenda se expresan en UTC y devuelven citas que se cruzan con el rango solicitado. `GET /api/citas/disponibilidad?servicioId={id}&fechaLocal=2026-10-05T00:00:00-05:00` devuelve los periodos libres por empleado que admiten la duración del servicio. Devuelve rangos, no una cuadrícula fija de horas de inicio; la creación vuelve a validar horario y cruces.

La reserva y la modificación del horario toman el mismo bloqueo transaccional de PostgreSQL por empleado antes de comprobar y guardar. Así, dos solicitudes simultáneas no pueden reservar el mismo tramo aunque ambas consulten disponibilidad a la vez.

## Inventario y compras

`/api/inventario/articulos` administra productos e insumos; cada artículo elige independientemente si maneja cantidades enteras o fraccionarias (hasta 3 decimales) y define una unidad base. Las cantidades de compras y movimientos se ingresan en esa unidad; no hay conversiones automáticas entre presentaciones. Productos requieren precio de venta; insumos no. El costo promedio se calcula con promedio ponderado móvil en la moneda del negocio.

El saldo disponible se actualiza solo al registrar movimientos, nunca mediante edición directa. Las entradas, salidas y compras guardan historial; los ajustes requieren motivo y no permiten saldo negativo. Las salidas por venta pueden dejar existencia negativa solo si `PermitirVentaSinStock` está habilitado. Se puede filtrar el catálogo por debajo del mínimo configurado mediante `soloBajoMinimo=true`.

`/api/proveedores` administra proveedores. `/api/compras` guarda borradores editables; `POST /api/compras/{id}/confirmar` actualiza existencias, costo promedio y movimientos en una única escritura. Una compra confirmada no se puede editar ni confirmar de nuevo; las correcciones se hacen con ajustes. Inventario, proveedores y compras son exclusivamente administrativos.

## Conexiones Supabase

La aplicación usa exclusivamente PostgreSQL/Npgsql; no usa SDK ni API de Supabase. Para el backend ASP.NET persistente, prefiera conexión directa (`5432`) si el entorno tiene IPv6 (o complemento IPv4); use el pooler compartido en modo sesión (`5432` del host del pooler) si el entorno es solo IPv4. Las migraciones se ejecutan por conexión directa; use modo sesión como alternativa cuando la red requiera pooler. El pooler transaccional (`6543`) está pensado para funciones serverless/de corta duración y no conserva estado de sesión ni admite sentencias preparadas. Si se usa excepcionalmente con Npgsql, configure `Max Auto Prepare=0`; `Pooling=false` es una opción conservadora para evitar el pool cliente en entornos de vida corta. No active `No Reset On Close`, porque puede filtrar estado entre sesiones. La connection string debe usar `SSL Mode=Require`; obtenga el host, puerto y usuario concretos del Dashboard y guarde el secreto fuera del repositorio. Consulte la [guía oficial de conexiones Supabase](https://supabase.com/docs/guides/database/connecting-to-postgres) y [parámetros de Npgsql](https://www.npgsql.org/doc/connection-string-parameters).

## Caja, ventas y facturación

La caja es compartida por el negocio: solo puede existir una caja abierta a la vez. Un usuario con rol Administrador o Recepcionista abre con un efectivo inicial, registra ingresos/egresos manuales y cierra ingresando el efectivo contado. El cierre conserva efectivo esperado, contado y diferencia; los pagos en efectivo, reembolsos y liquidaciones de comisiones en efectivo se reflejan como movimientos. Los movimientos y cierres no borran registros previos.

Una venta requiere caja abierta y numeración de comprobante configurada. Admite hasta 200 líneas, producto o servicio, cliente opcional, descuento porcentual o de importe fijo por línea y general, impuesto opcional por línea, y hasta 10 formas de pago por registro. Los descuentos se aplican antes del impuesto. Puede dejar saldo pendiente según `PermitirSaldosPendientes`; los abonos posteriores también requieren caja abierta. La forma de pago se marca como efectivo para determinar si afecta el arqueo. El empleado en líneas de servicio es opcional salvo que `ExigirEmpleadoVentaServicio` esté habilitado.

Cada venta registra y numera siempre un comprobante; su PDF interno se genera al solicitarlo. Si `FacturacionHabilitada` está activo, la venta puede solicitar además una factura numerada, cuyo PDF también se genera al solicitarlo. Estos documentos no constituyen integración ni validación de factura electrónica DIAN. Las devoluciones parciales y anulaciones solo están autorizadas para Administrador; exigen motivo, quedan registradas con sus pagos de reintegro y las devoluciones de productos vuelven a inventario. Las ventas, pagos, documentos, devoluciones y movimientos se conservan; la API no ofrece eliminación de ventas.

Los endpoints operativos están documentados en [`docs/contrato-api.md`](docs/contrato-api.md) y [`docs/openapi.json`](docs/openapi.json).

## Comisiones

Las comisiones se configuran por empleado y servicio, con tarifa porcentual o valor fijo por unidad. Se devengan al registrar la línea de servicio en una venta, aunque quede saldo pendiente; los descuentos se restan antes de calcular porcentajes y los impuestos no forman parte de la base. Las devoluciones agregan ajustes auditables. Solo Administrador puede consultar y administrar reglas, movimientos y liquidaciones; los pagos se agrupan por empleado, sin abonos parciales sobre las comisiones seleccionadas. Las comisiones de productos permanecen deshabilitadas en esta fase.

## Reportes

El Administrador dispone de informes de ventas, ingresos y reembolsos, productos, servicios, inventario, compras, caja, clientes, empleados y comisiones. Se pueden consultar como JSON y descargar en PDF, XLSX o CSV con filtros por fechas y criterios propios de cada informe. La fotografía de inventario representa existencias actuales, no un saldo histórico. Los detalles de filas, límites, fechas y permisos están en [`docs/contrato-api.md`](docs/contrato-api.md).

## Pruebas

```powershell
dotnet test FlexPos.slnx
```

Las pruebas unitarias no necesitan servicios externos. Las pruebas de integración PostgreSQL se omiten si no se definen `ConnectionStrings__FlexPosTest` (runtime) y `ConnectionStrings__FlexPosTestMigraciones` (migraciones). Al habilitarlas, use una base de datos desechable, separada de producción, con los dos roles configurados; así las llamadas de API se prueban realmente con el rol de runtime. Cubren autenticación, catálogos, compras, horarios y citas concurrentes, además de caja, ventas, pagos múltiples, saldos, documentos, devoluciones, anulaciones, inventario y el flujo de comisiones con ajustes y liquidación en efectivo.

```powershell
$env:ConnectionStrings__FlexPosTestMigraciones = "<connection-string-migraciones-de-pruebas>"
$env:ConnectionStrings__FlexPosTest = "<connection-string-runtime-de-pruebas>"
dotnet test FlexPos.slnx
```

## Decisiones del dominio

`Dinero` mantiene importe decimal y código de moneda ISO 4217, y rechaza operaciones entre monedas distintas. Las entidades principales usan UUID versión 7; las tablas auxiliares administradas por ASP.NET Identity conservan la forma requerida por el framework cuando corresponda.
