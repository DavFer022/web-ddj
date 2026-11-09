---
titulo: Backlog de Historias de Usuario (HU)
descripcion: Archivo de todas las HU del proyecto, para ser usado por los agentes de IA. No es la implementacion final, solo una coleccion para organizar el trabajo futuro. Cuando se vaya a implementar una HU, se debe pasar al archivo HU-IMPLEMENT.md y borrar de este archivo esa HU.
---

# Módulos

Las notas marcadas con ❗ son casos especiales que requieren un estudio o decisiones de diseño antes de implementar.

## Acceso de usuarios

### Épica: Autenticación y control de acceso

#### HU-01: Inicio y cierre de sesión

**Como** Usuario del sistema de la tienda **Quiero** iniciar y cerrar sesión con mi usuario/correo y contraseña **Para** acceder a los módulos del sistema.

- **Criterios de aceptación:**
  - Permite iniciar sesión con credenciales válidas de un `USUARIO` activo (`activo = true`).
  - Rechaza el acceso con credenciales inválidas o si el usuario está inactivo (`activo = false`), mostrando un mensaje claro sin exponer detalles sensibles.
  - Al cerrar sesión, invalida la sesión actual y redirige a `/login`.
  - Las rutas protegidas redirigen a `/login` cuando el usuario no está autenticado.

#### HU-02: Autorización por roles (Admin vs Usuarios)

**Como** admin del sitema de la tienda **Quiero** restringir operaciones críticas según el rol asignado al usuario **Para** evitar que usuarios sin permisos modifiquen configuraciones, usuarios o ajustes sensibles

- **Criterios de aceptación:**
  - Un usuario puede tener uno o más roles mediante `USUARIO_ROL`.
  - El usuario `Trabajador` pueden operar ventas, órdenes de servicio, consumos de insumos, cobros, catálogo de beneficios de fidelidad, ajustes manuales de inventario, gestionar clientes
  - Solo el rol `Admin` puede gestionar usuarios/roles, ademas de ver todo el sistema.
  - La restricción se valida tanto en la interfaz como en la lógica del servidor.

#### HU-03: Gestión de usuarios y baja lógica sin pérdida de trazabilidad

**Como** admin del sitema de la tienda **Quiero** crear usuarios, asignarles roles y desactivarlos a voluntad **Para** revocar su acceso sin perder el historial de ventas, movimientos u órdenes que registraron

- **Criterios de aceptación:**
  - No permite registrar dos usuarios con el mismo `correo`.
  - Desactivar un usuario (`activo = false`) conserva intactos sus registros históricos en `MOVIMIENTO_INVENTARIO`, `ORDEN_SERVICIO`, `MOVIMIENTO_CUENTA_CLIENTE` y `MOVIMIENTO_FIDELIDAD_CLIENTE`.
  - No permite desactivar ni quitar el rol `Admin`.

---

## Inventario

---

## Servicios de personalización

### Épica: Técnicas y presupuestación

#### HU-11: Catálogo de técnicas y calculadora de presupuesto al cliente

**Como** Usuario trabajador **Quiero** gestionar las técnicas de personalización (`TECNICA_PERSONALIZACION`) **Para** almacenar todos los servicios que ofrecemos.

- **Criterios de aceptación:**
  - Permite crear, editar y activar/desactivar técnicas (sublimación, DTF, vinil textil, etc.) con nombre único.

#### ❗ HU-12: calculadora de presupuesto al cliente

**Como** Usuario trabajador **Quiero** usar una calculadora de presupuestos combinando insumos, cantidades y técnicas **Para** darle una cotización inmediata al cliente antes de confirmar o crear la orden de servicio

- **Criterios de aceptación:**
  - La calculadora permite agregar una o varias líneas con descripción, cantidad, insumo estimado, una o varias técnicas y precio unitario para obtener el total cotizado.
  - Calcular un presupuesto no descuenta inventario ni genera cargos en cuenta hasta que el cliente confirme y se convierta en `ORDEN_SERVICIO`.

### Épica: Gestión de órdenes de servicio, planillas y calendario

#### HU-13: Creación, edición y cambio de estado de órdenes de servicio

**Como** Usuario trabajador **Quiero** crear, editar y actualizar el estado de las órdenes de servicio (`ORDEN_SERVICIO`) con sus detalles (`DETALLE_ORDEN_SERVICIO`) y técnicas por línea (`LINEA_ORDEN_TECNICA`) **Para** controlar el trabajo de personalización desde el pedido hasta la entrega

- **Criterios de aceptación:**
  - Toda orden genera un `numero_orden` único y autoincrementar, requiere `cliente_id`, `creado_por_usuario_id`, fecha de solicitud (`solicitado_en`), fecha de entrega (`vence_en`) y al menos una línea en `DETALLE_ORDEN_SERVICIO`.
  - Para formalizar la orden de servicio es obligatorio registrar un pago inicial ("inicial") de **mínimo el 50% del valor del servicio** (`total_cotizado`), registrando el `CARGO` de la orden y el `PAGO` inicial en `MOVIMIENTO_CUENTA_CLIENTE`.
  - Cada línea de detalle puede asociarse a una o más técnicas activas mediante `LINEA_ORDEN_TECNICA`.
  - Permite avanzar por estados (`PENDIENTE`, `EN_PROCESO`, `LISTO`, `ENTREGADO`, `CANCELADO`).

#### HU-14: Calendario de pedidos actuales y alertas de vencimiento

**Como** empleado del taller **Quiero** visualizar los pedidos en un calendario por fecha de entrega (`vence_en`) **Para** priorizar la carga de trabajo diaria y detectar pedidos próximos a vencer o atrasados

- **Criterios de aceptación:**
  - Muestra las órdenes activas ubicadas según su fecha `vence_en`, diferenciando visualmente su `estado`.
  - Resalta las órdenes vencidas aún no entregadas y las que vencen en los proximos 7 dias.

### Épica: Casos especiales de personalización y notificaciones

#### ❗ HU-15: Prenda aportada por el cliente y diferencia entre insumo previsto vs consumido

**Como** Usuario Trabajador **Quiero** registrar órdenes donde el cliente trae su propia prenda o donde el consumo real de insumos difiere de lo previsto y el `total_final` cambia respecto al `total_cotizado` **Para** reflejar cambios de último momento sin perder la cotización original ni la trazabilidad de insumos

- **Criterios de aceptación:**
  - Permite registrar líneas de servicio sobre prendas del cliente especificando en `DETALLE_ORDEN_SERVICIO` el insumo de personalización requerido y notas de la prenda recibida.
  - El sistema separa lo planificado en `DETALLE_ORDEN_SERVICIO` de lo efectivamente descontado en `MOVIMIENTO_INVENTARIO` (`motivo = CONSUMO`), permitiendo registrar más de un consumo para una misma orden.
  - Permite actualizar `total_final` al cerrar la orden conservando `total_cotizado` para comparar variaciones de precio.

#### HU-16: Cancelación de orden de servicio con consumo de insumos ya realizado

**Como** Usuario trabajador **Quiero** gestionar la cancelación de una orden de servicio que ya cuenta con su pago inicial (>= 50%) y que puede haber tenido consumo de insumos **Para** definir qué ocurre con los materiales usados, el saldo pendiente y el anticipo abonado por el cliente

- **Criterios de aceptación:**
  - Como toda orden formalizada exige un pago inicial mínimo del 50%, la orden ya cuenta con su `CARGO` y al menos el 50% registrado como `PAGO` en `MOVIMIENTO_CUENTA_CLIENTE`.
  - Si la orden ya tiene movimientos de `CONSUMO` o `MERMA` asociados, el sistema advierte al usuario y no revierte automáticamente los insumos ya utilizados (salvo que se registre explícitamente una entrada por reingreso de material no usado).
  - Al cancelar una orden con consumo de insumos ya realizado, se retiene el pago inicial (>= 50%) para cubrir los costos ejecutados y se ajusta/compensa el `CARGO` de la orden en `MOVIMIENTO_CUENTA_CLIENTE` por el saldo restante no ejecutado, de modo que el cliente no quede con deuda pendiente.
  - Si la orden se cancela antes de iniciar proceso y sin consumo de insumos, permite definir si se retiene parcial/totalmente el inicial o si se anula el cargo dejando el monto abonado a favor/reembolsado.

#### ❗ HU-17: Notificaciones a usuarios y clientes

**Como** Usuario trabajador **Quiero** que el sistema genere y registre notificaciones (`NOTIFICACION`) para usuarios internos y clientes a traves de WhatsApp, mensajes de texto o correos electrónicos **Para** avisar sobre pedidos próximos a vencer, cambios de asignación o pedidos listos para retirar

- **Criterios de aceptación:**
  - Toda `NOTIFICACION` apunta exactamente a un destinatario: `usuario_id` (exclusivo) o `cliente_id` (exclusivo), nunca ambos ni ninguno.
  - Registra `canal`, `estado` (`PENDIENTE`, `ENVIADA`, `LEIDA`, `FALLIDA`) y las marcas de tiempo `creado_en`, `enviado_en` y `leido_en`.
  - Los usuarios pueden ver sus notificaciones pendientes y marcarlas como leídas.

---

## CRM y clientes

### Épica: Gestión de clientes y cuenta corriente (deudas y pagos)

#### HU-18: Gestión de clientes y consulta de ficha integral

**Como** Usuario trabajador **Quiero** registrar, editar, buscar clientes y consultar su ficha histórica **Para** tener a mano sus datos de contacto, órdenes de servicio, compras, saldo deudor y puntos de fidelidad

- **Criterios de aceptación:**
  - Permite dar de alta y editar registros en `CLIENTE` (`nombre`, `apellido`, `correo`, `telefono`, `activo`).
  - Permite buscar clientes por nombre, apellido, teléfono o correo.
  - La ficha del cliente muestra su saldo pendiente actual (calculado desde `MOVIMIENTO_CUENTA_CLIENTE`) y su saldo de puntos (calculado desde `MOVIMIENTO_FIDELIDAD_CLIENTE`).

#### HU-19: Registro de cargos y pagos de deudas del cliente

**Como** Usuario trabajador **Quiero** registrar cargos por orden de servicio y pagos realizados por el cliente en `MOVIMIENTO_CUENTA_CLIENTE` **Para** llevar el control exacto de cuánto debe cada cliente y cómo va abonando

- **Criterios de aceptación:**
  - En todo `MOVIMIENTO_CUENTA_CLIENTE`, `monto` es siempre positivo (`monto > 0`).
  - Si `tipo_movimiento = CARGO`, debe referenciar exactamente una `orden_servicio_id` o un `movimiento_inventario_id` (venta), nunca ambos.
  - Si `tipo_movimiento = PAGO`, no referencia orden ni movimiento de inventario, y exige `metodo_pago` (efectivo, transferencia, pago móvil, etc.).
  - El saldo pendiente del cliente se calcula dinámicamente como `suma(CARGO) - suma(PAGO)`.

#### HU-20: Pago inicial mínimo del 50%, abonos parciales, pagos con múltiples métodos y control de sobrepago

**Como** Usuario trabajador **Quiero** registrar el pago inicial mínimo del 50% al formalizar una orden, abonos parciales en distintas fechas o un pago dividido en dos métodos de pago, evitando cobros mayores a la deuda **Para** cumplir la regla de inicial obligatorio y adaptarme a la forma real en que pagan los clientes sin descuadrar su cuenta

- **Criterios de aceptación:**
  - Al formalizar una `ORDEN_SERVICIO`, valida que el pago inicial registrado sea como mínimo el 50% del valor del servicio.
  - Permite registrar múltiples movimientos `PAGO` parciales a lo largo del tiempo para ir amortizando el saldo restante del cliente.
  - Si el cliente paga una parte en efectivo y otra por transferencia, permite registrarlo como movimientos de pago separados con su respectivo `metodo_pago` y `referencia`.
  - Rechaza o solicita confirmación expresa si un `PAGO` supera el saldo pendiente actual del cliente, salvo que se registre simultáneamente el cargo correspondiente.

### Épica: Fidelización y beneficios de clientes

#### HU-21: Catálogo de beneficios y acumulación/canje de puntos

**Como** Usuario trabajador **Quiero** configurar beneficios para clientes fieles (`BENEFICIO_FIDELIDAD`) y registrar la acumulación o el canje de puntos (`MOVIMIENTO_FIDELIDAD_CLIENTE`) **Para** premiar las compras frecuentes e incentivar que los clientes vuelvan

- **Criterios de aceptación:**
  - Permite crear, editar y activar/desactivar beneficios en `BENEFICIO_FIDELIDAD` con su `costo_puntos > 0`.
  - Al acumular puntos se registra un `MOVIMIENTO_FIDELIDAD_CLIENTE` con `variacion_puntos > 0` y su `motivo`.
  - Al canjear un beneficio activo se registra un movimiento con `variacion_puntos < 0` igual al `costo_puntos` del beneficio y su `beneficio_fidelidad_id`.
  - Rechaza el canje si el saldo calculado de puntos del cliente es menor al `costo_puntos` requerido o si el beneficio está inactivo.

#### HU-22: Reversión o ajuste de puntos por anulación o corrección

**Como** trabajador de la tienda **Quiero** registrar un ajuste o reversión en los puntos de fidelidad de un cliente cuando se anula una venta/orden o se aplicó un canje por error **Para** mantener el saldo de puntos consistente sin borrar el historial de movimientos

- **Criterios de aceptación:**
  - No se eliminan filas previas de `MOVIMIENTO_FIDELIDAD_CLIENTE`; toda corrección se registra como un nuevo movimiento compensatorio (`variacion_puntos` positivo o negativo) con `motivo` obligatorio y el `registrado_por_usuario_id`.
