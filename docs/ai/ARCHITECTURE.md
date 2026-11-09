---
descripcion: Arquitectura de carpetas, tablas y relaciones del proyecto. Formato de tabla, columnas: nombre, tipo, descripción, relaciones. Organizado por modulos (features) y entidades.
---

# Arquitectura de carpetas, tablas y relaciones

## Estructura de carpetas

```text
src/SistemaDDJ.Web/
  Modules/
    _Imports.razor
    Acceso/            (esquema: acceso)
    Inventario/        (esquema: inventario)
    Personalizacion/   (esquema: personalizacion)
    Clientes/          (esquema: clientes)
    Notificaciones/    (esquema: notificaciones)
      <Modulo>Module.cs     punto de registro (Add<Modulo>Module)
      PublicApi/            contratos para otros módulos (DTOs/records)
      Entities/             entidades del módulo
      Data/                 <Modulo>DbContext, Configurations/, Migrations/
      Features/             un caso de uso por carpeta (vertical slice)
      Pages/                componentes Razor del módulo
  Infrastructure/
    Persistence/
      PersistenceExtensions.cs   convenciones de nombres y reglas de FK
      PersistenceRegistration.cs registro de DbContext por módulo
      DevelopmentDataSeeder.cs   datos de prueba (solo Development)
      Integracion/               IntegracionDbContext y migraciones de FK entre esquemas
  Components/           shell de la aplicación (layout, rutas, páginas genéricas)
```

## Orden de migración

1. Un contexto por módulo: `AccesoDbContext`, `InventarioDbContext`, `PersonalizacionDbContext`, `ClientesDbContext`, `NotificacionesDbContext`.
2. `IntegracionDbContext` al final: crea las claves foráneas que cruzan esquemas.

## Reglas de identificadores

- `int` autoincremental: `USUARIO`, `ROL`, `CATEGORIA_ARTICULO`, `ARTICULO`, `PRODUCTO`, `INSUMO`, `PROVEEDOR`, `DETALLE_MOVIMIENTO_INVENTARIO`, `DETALLE_ORDEN_SERVICIO`, `TECNICA_PERSONALIZACION`, `BENEFICIO_FIDELIDAD`.
- `uuid` UUIDv7: `CLIENTE`, `MOVIMIENTO_INVENTARIO`, `ORDEN_SERVICIO`, `MOVIMIENTO_CUENTA_CLIENTE`, `MOVIMIENTO_FIDELIDAD_CLIENTE`, `NOTIFICACION`.

## Módulo Acceso (esquema `acceso`)

| Tabla | Columna | Tipo | Descripción | Relaciones |
|---|---|---|---|---|
| usuario | id | int PK | Identificador del usuario | Referenciado por inventario, personalizacion, clientes y notificaciones |
| usuario | correo | varchar(255) UK | Correo de acceso, único | |
| usuario | nombre_visible | varchar(100) | Nombre mostrado en la aplicación | |
| usuario | activo | boolean | Indica si puede operar; `false` es baja lógica | |
| usuario | creado_en | timestamptz | Fecha de creación | |
| rol | id | int PK | Identificador del rol | |
| rol | nombre | varchar(50) UK | `Admin` o `Trabajador` (datos semilla) | |
| usuario_rol | usuario_id | int PK, FK | Usuario asignado | → usuario |
| usuario_rol | rol_id | int PK, FK | Rol asignado | → rol |

## Módulo Clientes (esquema `clientes`)

| Tabla | Columna | Tipo | Descripción | Relaciones |
|---|---|---|---|---|
| cliente | id | uuid PK | Identificador del cliente | ← movimiento_cuenta_cliente, movimiento_fidelidad_cliente, orden_servicio, movimiento_inventario, notificacion |
| cliente | nombre | varchar(100) | Nombre | |
| cliente | apellido | varchar(100) | Apellido (opcional) | |
| cliente | correo | varchar(255) | Correo (opcional) | |
| cliente | telefono | varchar(30) | Teléfono (opcional) | |
| cliente | activo | boolean | Baja lógica | |
| cliente | creado_en | timestamptz | Fecha de alta | |
| beneficio_fidelidad | id | int PK | Identificador del beneficio | ← movimiento_fidelidad_cliente |
| beneficio_fidelidad | nombre | varchar(100) | Nombre del beneficio | |
| beneficio_fidelidad | descripcion | text | Descripción (opcional) | |
| beneficio_fidelidad | costo_puntos | int | Puntos requeridos, mayor que 0 | |
| beneficio_fidelidad | activo | boolean | Disponible para canje | |
| movimiento_cuenta_cliente | id | uuid PK | Identificador del movimiento | |
| movimiento_cuenta_cliente | tipo_movimiento | varchar(20) | `CARGO` o `PAGO` | |
| movimiento_cuenta_cliente | monto | numeric(18,2) | Monto positivo | |
| movimiento_cuenta_cliente | metodo_pago | varchar(50) | Obligatorio en PAGO | |
| movimiento_cuenta_cliente | referencia | varchar(100) | Número de transacción (opcional) | |
| movimiento_cuenta_cliente | ocurrido_en | timestamptz | Fecha del movimiento | |
| movimiento_cuenta_cliente | notas | text | Observaciones | |
| movimiento_cuenta_cliente | cliente_id | uuid FK | Cliente titular | → cliente |
| movimiento_cuenta_cliente | orden_servicio_id | uuid FK | Orden que origina el CARGO | → personalizacion.orden_servicio (integración) |
| movimiento_cuenta_cliente | movimiento_inventario_id | uuid FK | Venta que origina el CARGO | → inventario.movimiento_inventario (integración) |
| movimiento_cuenta_cliente | registrado_por_usuario_id | int FK | Usuario que registró el movimiento | → acceso.usuario (integración) |
| movimiento_fidelidad_cliente | id | uuid PK | Identificador del movimiento | |
| movimiento_fidelidad_cliente | variacion_puntos | int | Puntos ganados (positivo) o canjeados (negativo), distinto de 0 | |
| movimiento_fidelidad_cliente | motivo | varchar(200) | Motivo obligatorio | |
| movimiento_fidelidad_cliente | ocurrido_en | timestamptz | Fecha del movimiento | |
| movimiento_fidelidad_cliente | cliente_id | uuid FK | Cliente | → cliente |
| movimiento_fidelidad_cliente | beneficio_fidelidad_id | int FK | Beneficio canjeado (opcional) | → beneficio_fidelidad |
| movimiento_fidelidad_cliente | registrado_por_usuario_id | int FK | Usuario que registró el movimiento | → acceso.usuario (integración) |

## Módulo Inventario (esquema `inventario`)

| Tabla | Columna | Tipo | Descripción | Relaciones |
|---|---|---|---|---|
| categoria_articulo | id | int PK | Identificador de la categoría | ← articulo |
| categoria_articulo | nombre | varchar(100) UK | Nombre único | |
| categoria_articulo | descripcion | text | Descripción (opcional) | |
| categoria_articulo | activo | boolean | Baja lógica | |
| articulo | id | int PK | Identificador del artículo | ← producto, insumo, detalle_movimiento_inventario |
| articulo | codigo | varchar(30) UK | Código único | |
| articulo | nombre | varchar(150) | Nombre | |
| articulo | unidad | varchar(30) | Unidad de medida | |
| articulo | tipo | varchar(20) | `PRODUCTO` o `INSUMO`; debe coincidir con su tabla hija | |
| articulo | costo_unitario | numeric(18,2) | Costo de referencia, mayor o igual que 0 | |
| articulo | activo | boolean | Baja lógica | |
| articulo | categoria_id | int FK | Categoría del artículo | → categoria_articulo |
| producto | articulo_id | int PK, FK | Artículo de venta | → articulo (1:1) |
| producto | sku | varchar(30) UK | SKU comercial | |
| producto | precio_venta | numeric(18,2) | Precio de venta, mayor que 0 | |
| insumo | articulo_id | int PK, FK | Artículo de personalización | → articulo (1:1) |
| insumo | descripcion | text | Descripción del insumo | ← detalle_orden_servicio (integración) |
| proveedor | id | int PK | Identificador del proveedor | ← movimiento_inventario |
| proveedor | razon_social | varchar(150) | Razón social | |
| proveedor | identificacion_fiscal | varchar(30) UK | Identificación fiscal (opcional, única) | |
| proveedor | correo | varchar(255) | Correo (opcional) | |
| proveedor | telefono | varchar(30) | Teléfono (opcional) | |
| proveedor | activo | boolean | Baja lógica; no se usa en compras nuevas | |
| movimiento_inventario | id | uuid PK | Identificador del movimiento | ← detalle_movimiento_inventario, movimiento_cuenta_cliente |
| movimiento_inventario | tipo_movimiento | varchar(20) | `ENTRADA` o `SALIDA` | |
| movimiento_inventario | motivo | varchar(20) | `COMPRA`, `VENTA`, `CONSUMO`, `MERMA` o `AJUSTE` | |
| movimiento_inventario | ocurrido_en | timestamptz | Fecha del movimiento | |
| movimiento_inventario | referencia | varchar(100) | Número de factura o documento (opcional) | |
| movimiento_inventario | notas | text | Obligatorias en AJUSTE y MERMA | |
| movimiento_inventario | proveedor_id | int FK | Obligatorio solo en COMPRA | → proveedor |
| movimiento_inventario | cliente_id | uuid FK | Cliente de la venta (opcional) | → clientes.cliente (integración) |
| movimiento_inventario | orden_servicio_id | uuid FK | Obligatorio en CONSUMO; opcional en MERMA | → personalizacion.orden_servicio (integración) |
| movimiento_inventario | registrado_por_usuario_id | int FK | Usuario que registró el movimiento | → acceso.usuario (integración) |
| detalle_movimiento_inventario | id | int PK | Identificador de la línea | |
| detalle_movimiento_inventario | movimiento_id | uuid FK | Movimiento al que pertenece | → movimiento_inventario |
| detalle_movimiento_inventario | articulo_id | int FK | Artículo movido | → articulo |
| detalle_movimiento_inventario | cantidad | numeric(18,3) | Cantidad positiva | |
| detalle_movimiento_inventario | costo_unitario | numeric(18,2) | Costo de la operación (opcional, mayor o igual que 0) | |
| detalle_movimiento_inventario | precio_unitario | numeric(18,2) | Precio aplicado en venta (opcional, mayor o igual que 0) | |

## Módulo Personalizacion (esquema `personalizacion`)

| Tabla | Columna | Tipo | Descripción | Relaciones |
|---|---|---|---|---|
| tecnica_personalizacion | id | int PK | Identificador de la técnica | ← linea_orden_tecnica |
| tecnica_personalizacion | nombre | varchar(100) UK | Nombre único (sublimación, DTF, etc.) | |
| tecnica_personalizacion | descripcion | text | Descripción (opcional) | |
| tecnica_personalizacion | activo | boolean | Baja lógica | |
| orden_servicio | id | uuid PK | Identificador de la orden | ← detalle_orden_servicio, movimiento_inventario, movimiento_cuenta_cliente |
| orden_servicio | numero_orden | varchar(20) UK | Número autoincremental `OS-000001` (secuencia `orden_servicio_numero_seq`) | |
| orden_servicio | estado | varchar(20) | `PENDIENTE`, `EN_PROCESO`, `LISTO`, `ENTREGADO` o `CANCELADO` | |
| orden_servicio | solicitado_en | timestamptz | Fecha de solicitud | |
| orden_servicio | vence_en | timestamptz | Fecha de entrega; mayor o igual que `solicitado_en` | |
| orden_servicio | total_cotizado | numeric(18,2) | Valor del servicio presupuestado, mayor o igual que 0 | |
| orden_servicio | total_final | numeric(18,2) | Valor final al cerrar la orden (opcional) | |
| orden_servicio | notas | text | Observaciones | |
| orden_servicio | cliente_id | uuid FK | Cliente de la orden | → clientes.cliente (integración) |
| orden_servicio | creado_por_usuario_id | int FK | Usuario que creó la orden | → acceso.usuario (integración) |
| orden_servicio | asignado_a_usuario_id | int FK | Usuario asignado (opcional) | → acceso.usuario (integración) |
| detalle_orden_servicio | id | int PK | Identificador de la línea | ← linea_orden_tecnica |
| detalle_orden_servicio | orden_servicio_id | uuid FK | Orden a la que pertenece | → orden_servicio |
| detalle_orden_servicio | insumo_id | int FK | Insumo previsto (opcional, por ejemplo prendas del cliente) | → inventario.insumo (integración) |
| detalle_orden_servicio | descripcion | varchar(200) | Descripción de la línea | |
| detalle_orden_servicio | cantidad | numeric(18,3) | Cantidad positiva | |
| detalle_orden_servicio | precio_unitario | numeric(18,2) | Precio unitario, mayor o igual que 0 | |
| detalle_orden_servicio | notas | text | Observaciones de la prenda o del trabajo | |
| linea_orden_tecnica | detalle_orden_servicio_id | int PK, FK | Línea de la orden | → detalle_orden_servicio |
| linea_orden_tecnica | tecnica_id | int PK, FK | Técnica aplicada | → tecnica_personalizacion |

## Módulo Notificaciones (esquema `notificaciones`)

| Tabla | Columna | Tipo | Descripción | Relaciones |
|---|---|---|---|---|
| notificacion | id | uuid PK | Identificador de la notificación | |
| notificacion | titulo | varchar(150) | Título | |
| notificacion | mensaje | text | Contenido | |
| notificacion | canal | varchar(20) | `WHATSAPP`, `SMS` o `CORREO` | |
| notificacion | estado | varchar(20) | `PENDIENTE`, `ENVIADA`, `LEIDA` o `FALLIDA` | |
| notificacion | creado_en | timestamptz | Fecha de creación | |
| notificacion | enviado_en | timestamptz | Fecha de envío (opcional) | |
| notificacion | leido_en | timestamptz | Fecha de lectura (opcional) | |
| notificacion | usuario_id | int FK | Destinatario interno; exclusivo con cliente_id | → acceso.usuario (integración) |
| notificacion | cliente_id | uuid FK | Destinatario cliente; exclusivo con usuario_id | → clientes.cliente (integración) |

## Relaciones entre esquemas (migración de integración)

| Tabla origen | Columna | Tabla destino |
|---|---|---|
| inventario.movimiento_inventario | cliente_id | clientes.cliente |
| inventario.movimiento_inventario | orden_servicio_id | personalizacion.orden_servicio |
| inventario.movimiento_inventario | registrado_por_usuario_id | acceso.usuario |
| personalizacion.orden_servicio | cliente_id | clientes.cliente |
| personalizacion.orden_servicio | creado_por_usuario_id | acceso.usuario |
| personalizacion.orden_servicio | asignado_a_usuario_id | acceso.usuario |
| personalizacion.detalle_orden_servicio | insumo_id | inventario.insumo |
| clientes.movimiento_cuenta_cliente | orden_servicio_id | personalizacion.orden_servicio |
| clientes.movimiento_cuenta_cliente | movimiento_inventario_id | inventario.movimiento_inventario |
| clientes.movimiento_cuenta_cliente | registrado_por_usuario_id | acceso.usuario |
| clientes.movimiento_fidelidad_cliente | registrado_por_usuario_id | acceso.usuario |
| notificaciones.notificacion | usuario_id | acceso.usuario |
| notificaciones.notificacion | cliente_id | clientes.cliente |

Todas usan `ON DELETE RESTRICT`.
