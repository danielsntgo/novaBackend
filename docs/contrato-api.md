# Contrato actual de la API

Este documento describe lo que está implementado en el backend a fecha de generación. El contrato estructurado, con esquemas de solicitudes y respuestas, está en [`openapi.json`](openapi.json). Los nombres JSON usan camelCase.

## Base y disponibilidad

- OpenAPI es `3.1.1`; se exportó desde `GET /openapi/v1.json` en entorno `Development`.
- El archivo exportado contiene como `servers` la URL local del proceso de generación (`http://127.0.0.1:5088/`). El frontend debe configurar su URL base según el entorno; esa dirección local no es un endpoint de producción.
- OpenAPI no declara un esquema de seguridad Bearer ni las políticas de roles. La sección de autenticación y permisos de esta guía complementa esa omisión.
- La API puede arrancar sin PostgreSQL, pero las operaciones de negocio que leen o escriben datos requieren una conexión configurada. La exportación se realizó sin conexión a base de datos.
- No hay endpoint público de registro de negocios/administradores ni un identificador de tenant en las rutas actuales. El contrato corresponde a una instancia configurada para un negocio.

## Autenticación

### Tokens y cookie

- `POST /api/autenticacion/login` recibe `{ "correo": "...", "contrasena": "..." }`. Responde `200` con `{ "tokenAcceso": "...", "expiraUtc": "...", "segundosVigencia": 900 }` y emite el refresh token en una cookie. El refresh token no está en el cuerpo JSON.
- En el resto de llamadas autenticadas, enviar `Authorization: Bearer <tokenAcceso>`.
- La clave `Autenticacion:Jwt:ClaveFirma` no está en los archivos de configuración versionados ni tiene un valor predeterminado utilizable. Debe suministrarse por configuración segura y tener al menos 32 bytes UTF-8; si falta o es inválida, la emisión de token devuelve `503`.
- Cookie de refresh actual: `flexpos_refresh`; ruta `/api/autenticacion`; `HttpOnly=true`; `Secure=true`; expira con el refresh token. No especifica `Domain`, por lo que el navegador aplica el host que la emitió.
- Duración configurada por defecto: access token 15 minutos; refresh token 7 días. Los valores se pueden cambiar en `Autenticacion:Jwt:MinutosTokenAcceso` y `Autenticacion:Jwt:DiasTokenRenovacion`.
- `SameSite` por configuración: `Lax` en `appsettings.json`; `None` en `appsettings.Development.json`. Ambos configuran `Secure=true`. En el navegador el frontend debe enviar credenciales para que se acepte/envíe la cookie (`withCredentials: true` en Angular).
- `POST /api/autenticacion/refrescar` no recibe cuerpo. Requiere la cookie y la cabecera anti-CSRF indicada abajo. Si renueva correctamente, responde con el nuevo access token en JSON y rota la cookie refresh. Un refresh ausente, inválido, revocado o vencido produce `401` y el backend borra la cookie.
- `POST /api/autenticacion/logout` requiere la cabecera anti-CSRF; revoca el refresh token si existe y borra la cookie. Responde `204` incluso si no había un token utilizable.
- `POST /api/autenticacion/cambiar-contrasena` requiere Bearer. Recibe `{ "contrasenaActual": "...", "contrasenaNueva": "..." }`, responde con el nuevo access token y emite una cookie refresh nueva; no requiere la cabecera anti-CSRF.

### Anti-CSRF

En `refrescar` y `logout` es obligatoria la cabecera exacta `X-Requested-With: XMLHttpRequest`. Si falta o su valor no coincide, responde `403`. No está implementada una cabecera `X-CSRF-Token`.

### Alta de cuentas

- No existe registro público de cuentas. El administrador inicial se crea al iniciar con base de datos configurada, si no existe ya, usando `AdministradorInicial:Correo` y `AdministradorInicial:Contrasena` de configuración segura/secreta. Si el correo ya existe, no se modifica. Sin ambas claves no se crea ese administrador. No poner credenciales en archivos versionados.
- Un administrador puede crear recepcionistas mediante `POST /api/usuarios/recepcionistas`. La respuesta incluye la contraseña temporal generada; la cuenta requiere cambiarla en el primer inicio. No hay endpoint para que un usuario nuevo se autoconvierta en administrador.

## Roles y permisos por endpoint

`Administrador` y `Recepcionista` son los nombres exactos de los roles. Los endpoints protegidos requieren además una sesión vigente. Salvo `cambiar-contrasena`, se rechazan tokens que indiquen cambio de contraseña obligatorio. Un endpoint sin una política más permisiva queda sujeto a la política global: solo `Administrador`.

| Permiso | Endpoints |
|---|---|
| Público | `POST /api/autenticacion/login`, `POST /api/autenticacion/refrescar`, `POST /api/autenticacion/logout` (refresh/logout aún requieren cookie/cabecera anti-CSRF). |
| Cualquier usuario autenticado | `POST /api/autenticacion/cambiar-contrasena`. |
| `Administrador` o `Recepcionista` (`OperacionDiaria`) | `GET, POST /api/clientes`; `GET, PUT /api/clientes/{id}`; `PATCH /api/clientes/{id}/estado`; `GET /api/servicios`, `GET /api/servicios/{id}`; `GET, POST /api/citas`; `GET /api/citas/disponibilidad`; `GET /api/citas/{id}`; `PUT /api/citas/{id}/reprogramar`; `PATCH /api/citas/{id}/estado`. |
| `Administrador` o `Recepcionista` (`OperacionDiaria`) | `GET /api/caja/actual`; `GET /api/caja`; `GET /api/caja/{id}`; `POST /api/caja/aperturas`; `POST /api/caja/{id}/movimientos`; `POST /api/caja/{id}/cierre`; `GET, POST /api/ventas`; `GET /api/ventas/{id}`; `POST /api/ventas/{id}/pagos`; `GET /api/ventas/{id}/comprobante.pdf`; `GET /api/ventas/{id}/factura.pdf`. |
| Solo `Administrador` | Todos los endpoints de `/api/usuarios`, `/api/empleados`, `/api/inventario`, `/api/proveedores`, `/api/compras`, `/api/configuracion` y `/api/comisiones`; además `POST /api/servicios`, `PUT /api/servicios/{id}`, `PATCH /api/servicios/{id}/estado`, `GET /api/servicios/administracion`, `GET /api/servicios/administracion/{id}`. Esto incluye `GET, PUT /api/empleados/{id}/horario-semanal`. |
| Solo `Administrador` | `POST /api/ventas/{id}/devoluciones`; `POST /api/ventas/{id}/anular`. |
| Solo `Administrador` | `GET /api/reportes/{tipo}` y `GET /api/reportes/{tipo}/exportar`. Recepcionista no tiene acceso a informes. |

Los métodos y cuerpos concretos de cada ruta están en `openapi.json`. El documento OpenAPI generado no refleja estas autorizaciones.

## Enums y estados

- Citas (`estado`): `Pendiente` es el estado inicial; `Confirmada` está reservada; `Atendida` completada; `Cancelada` anulada; `NoAsistio` marca ausencia. Desde `Pendiente` se permite `Confirmada`, `Atendida`, `Cancelada` o `NoAsistio`; desde `Confirmada`, solo `Atendida`, `Cancelada` o `NoAsistio`. Los tres estados finales no se pueden reabrir ni reprogramar. `Cancelada` libera el horario; las demás citas reservan el horario.
- Compras (`estado`): `Borrador` se puede editar; `Confirmada` no se puede editar y su confirmación registra las entradas al inventario.
- Inventario (`tipo`): `Producto` requiere precio de venta no negativo; `Insumo` no admite precio de venta.
- Ventas (`estado`): `Finalizada` es el estado inicial, incluso si tiene saldo pendiente; `ParcialmenteDevuelta` conserva una venta con devolución de una parte de sus unidades; `Devuelta` indica que se devolvieron todas las unidades; `Anulada` es la anulación administrativa. No se elimina la venta.
- Línea de venta (`tipo`): `Producto` descuenta existencia y `Servicio` no mueve inventario. La línea de servicio puede incluir `empleadoId` según configuración.
- Descuento (`tipo`): `Porcentaje` usa un valor entre 0 y 100; `ImporteFijo` usa valor monetario positivo o cero. Se puede especificar por línea y para el total de la venta.
- Caja (`estado`): `Abierta` acepta ventas y movimientos; `Cerrada` conserva el arqueo final.
- Movimientos de caja (`tipo`): `PagoVenta` suma efectivo recibido, `Reembolso` resta efectivo devuelto, `IngresoManual` suma efectivo y `EgresoManual` lo resta.
- Tarifa (`tipo` en reglas y `tipoTarifa` en movimientos): `Porcentaje` calcula sobre la base neta antes de impuestos; `ValorFijo` es un importe por unidad de servicio. `tipoMovimiento`: `DevengoServicio` o `AjusteDevolucion`. `estado`: `Pendiente` aún no liquidada, `Pagada` incluida en una liquidación, `Revertida` neutralizada antes del pago.
- Movimiento de caja (`tipo`): también existe `PagoComision`, que resta la liquidación pagada en efectivo del efectivo esperado.
- Movimientos de inventario (`tipo`): `EntradaCompra`, `EntradaAjuste`, `SalidaAjuste`, `SalidaVenta`, `EntradaDevolucion`.
- Numeración (`tipoDocumento`): `Factura` y `Comprobante`. La numeración configurada se consume transaccionalmente; el comprobante se genera en toda venta y la factura solo si se solicita con facturación habilitada.
- Estado activo de registros y usuarios se expresa como booleano `activo`, no como enum.

## Reglas de negocio que afectan al frontend

### Citas y horarios

- Para crear una cita, cliente, servicio y empleado deben existir y estar activos. Se copia la duración del servicio a la cita; modificar el servicio después no altera la duración ya guardada.
- `InicioLocal` y `fechaLocal` deben ser fecha-hora ISO 8601 con desfase UTC explícito, por ejemplo `2026-10-05T09:00:00-05:00`. Las citas se guardan y devuelven como `inicioUtc`/`finUtc` en UTC. No hay zona horaria IANA ni regla de horario de verano configurada por negocio; el cliente debe enviar el desfase correcto.
- `GET /api/citas/disponibilidad?servicioId=...&fechaLocal=...` devuelve empleados con intervalos continuos disponibles (`periodos`), no una lista de slots. Un intervalo se devuelve solo si cabe completa la duración del servicio. Solo considera empleados activos con horario configurado.
- Horario semanal: `diaSemana` usa `DayOfWeek` de .NET: `0` domingo, `1` lunes, ..., `6` sábado; `horaInicio` y `horaFin` son horas locales. Se aceptan varios intervalos al día, pero no se pueden solapar y `horaFin` debe ser posterior a `horaInicio`. Una lista vacía elimina el horario y deja al empleado sin disponibilidad.
- Crear/reprogramar valida que la cita quede completamente dentro de un intervalo laboral y que no se cruce con otra cita del empleado. Las canceladas no bloquean. Los cruces de citas producen conflicto `409`; también hay bloqueo transaccional PostgreSQL para coordinar operaciones de agenda.
- Guardar el horario valida cruces entre sus intervalos, pero el código actual no comprueba que las citas existentes sigan quedando dentro del nuevo horario. Tenerlo en cuenta al editar turnos.

### Inventario, compras y dinero

- Cada artículo elige su propia precisión: `manejaFraccion=false` acepta solo cantidades enteras; `true` acepta hasta tres decimales. Tras registrar movimientos no se puede cambiar la unidad base ni esa opción.
- Cantidades de entradas y salidas deben ser positivas. Una salida mayor que la existencia actual falla con `409`. El costo promedio se actualiza como promedio ponderado en las entradas.
- Una compra se crea como borrador y debe tener líneas válidas para confirmarse. Confirmar una compra registra existencias y costos; una compra confirmada no se puede editar.
- Los importes monetarios se acompañan de `codigoMoneda` (ISO 4217, por ejemplo `COP`) en los DTO que lo exponen. La moneda de negocio se configura en `/api/configuracion`.
- La configuración de negocio expone `permitirSaldosPendientes`, `facturacionHabilitada` y `exigirEmpleadoVentaServicio`. En una nueva configuración, los tres valores predeterminados son `false`; un administrador puede actualizarlos mediante `PUT /api/configuracion`.
- `comisionProductosHabilitada` debe permanecer `false` en `PUT /api/configuracion`; intentar activarlo responde `400` porque esta fase solo comisiona servicios. La migración de esta fase también normaliza a `false` configuraciones existentes.
- Después de crear servicios, artículos, compras, cajas o ventas, no se permite cambiar `codigoMoneda`; los precios y movimientos previos no se convierten.

### Caja y ventas

- `GET /api/caja/actual` devuelve `204` cuando no hay caja abierta y `200` con la caja activa y su `efectivoEsperado` cuando sí existe. Solo puede estar abierta una caja a la vez para el negocio; es compartida por Administrador y Recepcionista, no está asociada a una terminal o empleado.
- `POST /api/caja/aperturas` recibe `efectivoInicial`. Requiere configuración del negocio y no permite abrir una segunda caja si ya hay una activa.
- `POST /api/caja/{id}/movimientos` acepta solo `IngresoManual` o `EgresoManual`, importe positivo y concepto obligatorio. No se pueden agregar movimientos manuales a una caja cerrada.
- `POST /api/caja/{id}/cierre` recibe `efectivoContado`. El backend registra `efectivoEsperadoCierre`, `efectivoContadoCierre` y `diferenciaCierre = contado - esperado`. El efectivo esperado se compone del efectivo inicial más pagos en efectivo e ingresos manuales, menos reembolsos y egresos manuales. Pagos no en efectivo no afectan el arqueo. `GET /api/caja/{id}` incluye sus movimientos.
- Para registrar una venta debe existir caja abierta, configuración del negocio y numeración de `Comprobante`. El cliente es opcional. Se aceptan de 1 a 200 líneas y hasta 10 pagos en una solicitud. Las líneas de producto y servicio usan precios/unidades de los catálogos vigentes y guardan una copia de nombres, códigos, moneda, precio, impuesto y descuentos para conservar el historial.
- `tipo` de línea acepta `Producto` o `Servicio`. `Producto` requiere `articuloInventarioId`, permite cantidad fraccionaria solo si así está configurado el artículo, y descuenta inventario. `Servicio` requiere `servicioId`, su cantidad debe ser entera y no mueve inventario. `empleadoId` solo corresponde a servicios; es opcional si `ExigirEmpleadoVentaServicio=false` y requerido si es `true`. El empleado, servicio y producto deben estar activos.
- Cada línea puede incluir `descuento: { tipo, valor }`; la solicitud de venta también admite `descuentoGeneral` con igual estructura. `tipo` es `Porcentaje` (0 a 100) o `ImporteFijo` (importe en la moneda del negocio). Un descuento fijo no puede superar la base disponible. Los descuentos se calculan antes de impuestos; el descuento general se distribuye proporcionalmente entre las líneas y los importes se redondean a dos decimales. Cada línea puede seleccionar un `impuestoId` activo; no seleccionar impuesto significa tasa cero/sin impuesto. La tasa y el nombre se copian al snapshot de la venta.
- `pagos` puede contener varios métodos configurados activos; cada importe debe ser mayor que cero y la suma no puede exceder el total pendiente. Si los pagos dejan saldo, se acepta solo cuando `PermitirSaldosPendientes=true`; si está deshabilitado, la suma debe cubrir el total. El arreglo puede omitirse cuando se permiten saldos. `POST /api/ventas/{id}/pagos` agrega uno o varios abonos y requiere caja abierta; el cambio posterior de `PermitirSaldosPendientes` no impide cobrar saldos que ya existen. No se admite sobrepago ni se calcula cambio.
- El método de pago se configura con `esEfectivo` y `requiereReferencia`. Solo puede existir un método marcado como efectivo. Los pagos en efectivo crean movimientos de caja; los demás métodos quedan en la venta, pero no alteran el conteo de efectivo.
- Todo `POST /api/ventas` consume numeración y registra un documento `Comprobante` en la respuesta; la numeración debe haberse configurado en `/api/configuracion/numeraciones-documento`. `GET /api/ventas/{id}/comprobante.pdf` genera y entrega el PDF a partir del snapshot. No es un documento electrónico validado por DIAN.
- Para generar además `Factura`, se requiere que `FacturacionHabilitada=true`, solicitar `solicitarFactura=true` en la venta y configurar numeración `Factura`. `GET /api/ventas/{id}/factura.pdf` devuelve el PDF si existe; si no se pidió o no existe, devuelve ProblemDetails `404`. No hay integración con DIAN, envío, numeración/autorización fiscal ni validación electrónica: queda **pendiente**.
- La devolución parcial usa `POST /api/ventas/{id}/devoluciones`, motivo obligatorio de hasta 250 caracteres, una o más líneas con `detalleVentaId` y cantidad no devuelta previamente, y pagos de reintegro cuando exista un importe que reintegrar. El importe de reintegro debe coincidir con el saldo que ya se había cobrado y corresponde reintegrar; no es editable libremente. Al devolver líneas de producto, el inventario se repone y se registra `EntradaDevolucion`. Si se devuelven todas las unidades, el estado pasa a `Devuelta`; de lo contrario, `ParcialmenteDevuelta`.
- `POST /api/ventas/{id}/anular` anula las unidades aún no devueltas y requiere motivo y eventual reintegro. Ambas operaciones exigen rol Administrador y caja abierta. No borran ventas ni pagos originales: agregan devolución, detalles, reintegros, usuario/fecha de anulación y movimientos auditables. Un reembolso en efectivo genera movimiento negativo de caja.
- Las ventas admiten filtros `desdeUtc`, `hastaUtc`, `clienteId` y `estado`; el estado debe ser uno de los enums anteriores. Cajas admiten `desdeUtc` y `hastaUtc`. Ambas listas usan `pagina` y `tamanoPagina`.

### Comisiones

- Todas las rutas `/api/comisiones` requieren rol `Administrador`; Recepcionista no tiene acceso a reglas, movimientos ni liquidaciones.
- `GET /api/comisiones/reglas` lista tarifas, incluidas las inactivas. Acepta filtros opcionales `empleadoId` y `servicioId`; es paginado. `POST /api/comisiones/reglas` crea una tarifa para un empleado y servicio activos. Solo puede existir una regla por pareja empleado/servicio; si ya existe, debe actualizarse con `PUT /api/comisiones/reglas/{id}` o reactivarse con `PATCH /api/comisiones/reglas/{id}/estado`. No hay borrado de reglas.
- `tipo` admite `Porcentaje` o `ValorFijo`. `valor` debe ser positivo y tener hasta dos decimales; porcentaje máximo `100`. La tarifa fija es por unidad de servicio y se multiplica por la cantidad de la línea.
- El devengo ocurre al guardar la línea de servicio en una venta, no al marcar una cita como atendida. Requiere `empleadoId` y una regla activa exacta para empleado/servicio. Si la línea no tiene empleado o no tiene regla activa, la venta continúa sin crear comisión.
- Para porcentaje, la base es `ImporteBruto - DescuentoImporte - DescuentoGeneralImporte`: después de ambos descuentos y antes de impuestos. El importe se redondea a dos decimales con redondeo de mitades alejándose de cero. La comisión se devenga aunque la venta conserve saldo pendiente. Guarda la tarifa, base, cantidad y moneda de la venta como snapshot; cambios posteriores de precio o regla no recalculan movimientos anteriores.
- El campo `importe` de cada movimiento es siempre positivo; para obtener el efecto neto, `DevengoServicio` suma y `AjusteDevolucion` resta. `baseCalculo` y `cantidad` del ajuste describen la parte revertida.
- En esta fase no se generan comisiones de productos. La configuración rechaza activar `comisionProductosHabilitada`; el cálculo solo considera líneas `Servicio`.
- Las devoluciones y anulaciones administrativas de servicios crean `AjusteDevolucion` en el mismo proceso transaccional, proporcional a unidades devueltas y limitado al devengo original. Si se devuelve todo antes de pagar, los movimientos pendientes quedan `Revertida`; no se eliminan. Si la comisión original ya fue pagada, el ajuste queda pendiente y compensa liquidaciones futuras.
- `GET /api/comisiones` es paginado y filtra opcionalmente por `empleadoId`, `estado`, `tipoMovimiento`, `desdeUtc` y `hastaUtc`. Las fechas se comparan contra `fechaCreacionUtc` e incluyen ambos extremos. El listado contiene devengos y ajustes.
- `POST /api/comisiones/liquidaciones` recibe `comisionIds` (entre 1 y 200 devengos `Pendiente` del mismo empleado), `metodoPagoId` activo y opcionalmente `referencia`. No admite liquidación parcial de los devengos seleccionados: los paga completos en un lote para ese empleado. Resta ajustes pendientes asociados a la selección y ajustes de devengos ya pagados. Si el neto es cero o negativo, no crea liquidación y deja movimientos pendientes para compensar con comisiones futuras.
- Los pagos de liquidación usan un método configurado. Si está marcado `esEfectivo=true`, se requiere caja abierta y se registra `PagoComision`, que reduce el efectivo esperado; los demás métodos no alteran la caja. El DTO de liquidación devuelve también los IDs de todos los movimientos (devengos y ajustes) incluidos.
- Las reglas son editables y activables/desactivables; los movimientos de comisión y liquidaciones son auditables y no tienen endpoint de borrado. PostgreSQL aplica RLS y el runtime recibe solo `SELECT/INSERT/UPDATE` para reglas y movimientos, `SELECT/INSERT` para liquidaciones y ningún permiso `DELETE` para estos registros.

### Reportes

- Solo Administrador puede consultar o exportar informes. `{tipo}` admite `ventas`, `ingresos`, `productos`, `servicios`, `inventario`, `compras`, `caja`, `clientes`, `empleados` y `comisiones`.
- `GET /api/reportes/{tipo}` responde `tipo`, `nombre`, `generadoUtc`, `desdeUtc`, `hastaUtc`, `columnas`, `filas`, `totales`, `totalRegistros`, `limite` y `truncado`. Cada fila contiene `valores`, un objeto cuyas claves coinciden con `columnas`; sus valores conservan tipos JSON (número, booleano, UUID, fecha/hora, texto o null).
- `GET /api/reportes/{tipo}/exportar?formato=pdf|xlsx|csv` descarga el archivo en `Content-Disposition`. El formato no distingue mayúsculas. PDF usa `application/pdf`, Excel usa `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` y CSV usa `text/csv; charset=utf-8`. PDF y Excel incluyen encabezado/resumen; CSV contiene las columnas y filas tabulares. Las fechas se exportan como UTC.
- Todos salvo `inventario` aceptan `desdeUtc` y `hastaUtc`, inclusivos. Si se envían ambos, `hastaUtc` debe ser igual o posterior a `desdeUtc`. Los filtros adicionales aplicables dependen del informe y se enumeran en la tabla de filtros. Un filtro incompatible devuelve `400 ProblemDetails` (`reporte.filtro_no_aplicable`).
- No hay paginación en informes. `limite` predeterminado `5000`, máximo `10000`, limita las filas devueltas/exportadas. `totalRegistros` es el total coincidente y `truncado=true` señala que hay más filas; `totales` se calculan sobre las filas incluidas, no sobre las omitidas por el límite.
Las claves y el orden exactos de `columnas` (y de cada objeto `fila.valores`) son:
- `ventas`: `VentaId`, `FechaVentaUtc`, `Cliente`, `DocumentoCliente`, `Estado`, `Subtotal`, `Descuentos`, `Impuestos`, `Total`, `Pagado`, `Devuelto`, `Reintegrado`, `Pendiente`, `Moneda`.
- `ingresos`: `FechaUtc`, `Tipo`, `VentaId`, `Cliente`, `MetodoPago`, `EsEfectivo`, `Importe`, `Moneda`.
- `productos` y `servicios`: `FechaUtc`, `Movimiento`, `ArticuloId`, `ServicioId`, `EmpleadoId`, `Codigo`, `Nombre`, `Unidad`, `Cantidad`, `Importe`, `Moneda`.
- `inventario`: `ArticuloId`, `Codigo`, `Nombre`, `Tipo`, `Categoria`, `Unidad`, `ManejaFraccion`, `Existencia`, `Minimo`, `CostoPromedio`, `ValorInventario`, `PrecioVenta`, `Activo`, `BajoMinimo`, `Moneda`.
- `compras`: `CompraId`, `FechaCompraUtc`, `Proveedor`, `Referencia`, `Estado`, `Lineas`, `Total`, `Moneda`.
- `caja`: `CajaId`, `Estado`, `AperturaUtc`, `CierreUtc`, `EfectivoInicial`, `PagosVenta`, `IngresosManuales`, `Reembolsos`, `EgresosManuales`, `PagoComisiones`, `EfectivoEsperado`, `EfectivoContado`, `Diferencia`, `Moneda`.
- `clientes`: `ClienteId`, `Nombre`, `Documento`, `Telefono`, `Correo`, `Activo`, `Ventas`, `TotalVentas`, `TotalDevuelto`, `TotalNeto`, `TotalPagado`, `TotalReintegrado`, `SaldoPendiente`, `Moneda`.
- `empleados`: `EmpleadoId`, `Nombre`, `Cargo`, `Activo`, `LineasServicio`, `CantidadServicios`, `TotalServicios`, `ComisionesDevengadas`, `AjustesDevolucion`, `ComisionesNetas`, `Moneda`.
- `comisiones`: `ComisionId`, `FechaUtc`, `Empleado`, `Servicio`, `Movimiento`, `Estado`, `Cantidad`, `BaseCalculo`, `Importe`, `Moneda`, `VentaId`, `DevolucionVentaId`, `LiquidacionId`.
- Las etiquetas exactas de `totales` son: `ventas` (`Ventas`, `Total`, `Pagado`, `Pendiente`); `ingresos` (`Movimientos`, `Neto recaudado`); `productos`/`servicios` (`Movimientos`, `Cantidad neta`, `Importe neto`); `inventario` (`Articulos`, `Existencia total`, `Valor inventario`); `compras` (`Compras`, `Importe`); `caja` (`Cajas`, `Efectivo inicial`, `Diferencias de cierre`); `clientes` (`Clientes con ventas`, `Ventas`, `Total vendido`, `Total devuelto`, `Total neto`, `Total pagado`); `empleados` (`Empleados`, `Servicios vendidos`, `Total servicios`, `Comisiones netas`); `comisiones` (`Movimientos`, `Importe movimientos`). Los importes incluyen `codigoMoneda` cuando hay filas.
- Algunas claves son nulas cuando el dato no existe: por ejemplo `Cliente`/`DocumentoCliente`, identificadores de artículo/servicio/empleado que no correspondan al tipo de línea, `Codigo`, `Categoria`, `PrecioVenta`, `Referencia`, y campos de cierre de una caja aún abierta. En JSON se envían como `null`; en CSV/Excel quedan celdas vacías.
- `ventas`: una fila por venta, seleccionada por `fechaVentaUtc`. `Total`, `Pagado`, `Devuelto`, `Reintegrado` y `Pendiente` reflejan el estado actual de cada venta del período; no son exclusivamente movimientos ocurridos en ese rango. Acepta `clienteId` y `estado` (`Finalizada`, `ParcialmenteDevuelta`, `Devuelta`, `Anulada`).
- `ingresos`: una fila por pago o reembolso, según la fecha de creación del pago. `PagoVenta` tiene importe positivo y `Reembolso` negativo; se informa también el método y `esEfectivo` para distinguir pagos en efectivo de otros métodos. Acepta `clienteId` y `metodoPagoId`. No equivale al total vendido.
- `productos` y `servicios`: una fila por evento de venta o devolución. Las devoluciones usan la fecha de devolución y cantidad/importe negativos. `productos` acepta `articuloId`; `servicios` acepta `servicioId` y `empleadoId`.
- `inventario`: fotografía de existencias actuales al generar el informe, con costo promedio, valoración, mínimo y marca `bajoMinimo`. No es histórica ni admite fechas; acepta `articuloId`.
- `compras`: una fila por compra, por `fechaCompraUtc`, con proveedor, estado y total. Incluye borradores y confirmadas sin filtro de estado. Acepta `proveedorId` y `estado` (`Borrador`, `Confirmada`).
- `caja`: una fila por sesión, filtrada por fecha de apertura. Agrega todos los movimientos de la sesión, no solo movimientos dentro del rango de aperturas. Para cajas abiertas, `efectivoEsperado` se calcula con el efectivo inicial y movimientos de efectivo registrados; las cerradas usan el valor de arqueo guardado. Acepta `estado` (`Abierta`, `Cerrada`).
- `clientes`: actividad de clientes con al menos una venta en el período; agrupa por fecha de venta e incluye total vendido, devuelto, neto, pagado, reintegrado y saldo actual. Acepta `clienteId`.
- `empleados`: agrupa líneas de servicios según fecha de venta y comisiones/ajustes según fecha de creación del movimiento. Incluye empleados con ventas de servicios asociadas o movimientos de comisión en el período. Acepta `empleadoId`.
- `comisiones`: movimientos individuales por fecha de creación, incluidos devengos y ajustes. El importe del ajuste se muestra positivo con `tipoMovimiento=AjusteDevolucion`; para el neto debe restarse de `DevengoServicio`. Acepta `empleadoId`, `servicioId` y `estado` (`Pendiente`, `Pagada`, `Revertida`).
- Tipos, filtros, estados, formatos o límites inválidos responden `400 ProblemDetails`.

### Registros

Clientes, empleados, servicios, proveedores, artículos, impuestos, métodos de pago y usuarios exponen activación/desactivación mediante `PATCH .../estado`; no hay endpoint DELETE en esos recursos. Un registro desactivado se conserva.

## Errores y validación

- Los errores de aplicación se responden como `ProblemDetails`. En errores de dominio, `type` incluye el identificador, por ejemplo `urn:flexpos:error:cita.solapamiento`; `title` contiene el mensaje en español y `status` el código HTTP. No se entrega una propiedad JSON independiente llamada `codigo`.
- Errores no controlados se ocultan como `500` con título genérico y `traceId`; conflictos de concurrencia de EF Core se devuelven como `409`. La estructura estable que debe consumir el frontend es `type`, `title` y `status`; `instance`/`traceId` pueden aparecer en respuestas de middleware.
- La API usa `[ApiController]`: errores de lectura/model binding pueden devolver `400 ValidationProblemDetails` con `errors` indexado por nombre de campo. Sin embargo, las validaciones de negocio se convierten en `ProblemDetails` generales sin asociación consistente a campos. Un contrato uniforme de errores por campo está **pendiente**.
- Los códigos HTTP y respuestas por operación se detallan en OpenAPI; autenticación fallida devuelve `401` y falta de cabecera anti-CSRF devuelve `403`. En caja, un identificador inexistente devuelve `404`; caja cerrada, caja ya abierta o conflictos de concurrencia devuelven `409`. En ventas, venta/documento inexistente devuelve `404`; caja cerrada/ausente, referencias inactivas, facturación deshabilitada, saldo pendiente deshabilitado y conflictos devuelven `409` cuando el controlador los mapea así. Errores de validación de solicitud o dominio restantes pueden responder `400`; el `type` identifica el error concreto.
- Comisiones: regla o liquidación inexistente devuelve `404`; regla duplicada, referencias inactivas, método de pago inactivo, selección no liquidable, neto no positivo, falta de caja para pago en efectivo y conflictos devuelven `409`. Tarifas/datos inválidos y activación de comisión de productos devuelven `400`.
- Reportes: tipos, filtros, estados, formatos o límites inválidos devuelven `400 ProblemDetails`; las rutas requieren Administrador.

## Paginación y filtros

Las listas paginadas usan `pagina` (predeterminado `1`) y `tamanoPagina` (predeterminado `20`, rango aceptado `1..100`). Las respuestas contienen `elementos`, `pagina`, `tamanoPagina` y total. Según el DTO, la propiedad del total es `totalElementos` o `total` (inventario y compras); verificar el esquema de cada operación en OpenAPI.

| Lista | Filtros implementados |
|---|---|
| `GET /api/usuarios` | `buscar` |
| `GET /api/clientes` | `buscar`, `activo` (predeterminado `true`) |
| `GET /api/empleados` | `buscar`, `activo` |
| `GET /api/servicios` | `buscar` (solo activos); `GET /api/servicios/administracion`: `buscar`, `activo` |
| `GET /api/inventario/articulos` | `buscar`, `tipo` (`Producto`/`Insumo`), `activo` (predeterminado `true`), `soloBajoMinimo` (predeterminado `false`) |
| `GET /api/inventario/movimientos` | `articuloId`, `desdeUtc`, `hastaUtc` |
| `GET /api/proveedores` | `buscar`, `activo` (predeterminado `true`) |
| `GET /api/compras` | `buscar`, `estado` (`Borrador`/`Confirmada`), `desdeUtc`, `hastaUtc` |
| `GET /api/citas` | `desdeUtc`, `hastaUtc`, `empleadoId`, `clienteId`, `estado` |
| `GET /api/caja` | `desdeUtc`, `hastaUtc` |
| `GET /api/ventas` | `desdeUtc`, `hastaUtc`, `clienteId`, `estado` (`Finalizada`/`ParcialmenteDevuelta`/`Devuelta`/`Anulada`) |
| `GET /api/comisiones/reglas` | `empleadoId`, `servicioId` |
| `GET /api/comisiones` | `empleadoId`, `estado`, `tipoMovimiento`, `desdeUtc`, `hastaUtc` |
| `GET /api/comisiones/liquidaciones` | `empleadoId`, `desdeUtc`, `hastaUtc` |
| `GET /api/reportes/{tipo}` y `/exportar` | `desdeUtc`, `hastaUtc`, filtros específicos por informe, `estado`, `limite`; sin paginación. Exportación con `formato=pdf|xlsx|csv` |

En filtros que validan `buscar`, la longitud máxima implementada es 120 caracteres. En citas, el rango temporal devuelve citas que se solapan con el intervalo indicado (`finUtc > desdeUtc` y `inicioUtc < hastaUtc`); si se envían ambos límites, `hastaUtc` debe ser posterior a `desdeUtc`. Los detalles de cada filtro están en los parámetros OpenAPI.

## CORS para Angular

- Política `Frontend`: orígenes exactos configurados en `Cors:OrigenesPermitidos`, comparación sin distinguir mayúsculas. No admite comodín. `appsettings.Development.json` permite `http://localhost:4200`; en `appsettings.json` la lista está vacía, así que hay que añadir explícitamente el origen desplegado en la configuración de ese entorno.
- Métodos permitidos: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`, `OPTIONS`.
- Cabeceras permitidas: `Content-Type`, `Authorization`, `X-Requested-With`.
- `AllowCredentials` está habilitado. En Angular usar `withCredentials: true` para llamadas que manejan la cookie; enviar también el Bearer en `Authorization` para rutas protegidas.
- Para refrescar y cerrar sesión, incluir `X-Requested-With: XMLHttpRequest`. El servidor rechazará cualquier origen no configurado; la API no contiene un origen de producción predeterminado.

## Exportar OpenAPI

Ejecutar la API en Development en una terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__FlexPos = ''
dotnet run --project src\FlexPos.Api\FlexPos.Api.csproj --no-launch-profile --urls http://127.0.0.1:5088
```

En otra terminal, desde la raíz del backend:

```powershell
Invoke-WebRequest -Uri http://127.0.0.1:5088/openapi/v1.json -OutFile docs\openapi.json
```

El endpoint OpenAPI solo se registra en `Development`. Al exportar desde otro host o puerto, el campo `servers` del JSON reflejará esa URL de ejecución; el frontend debe usar su propia URL base por entorno. No se necesita PostgreSQL para arrancar y exportar OpenAPI, pero sí para probar las operaciones persistentes.
