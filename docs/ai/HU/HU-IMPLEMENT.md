---
titulo: Implentacion de HU
descripcion: Aqui se guardan las HU que se van a implementar en la siguiente iteracion. Se deben organizar por modulos, marcarlas como "En progreso" y luego "Completadas" cuando se finalicen.
reglas:
  - Solo se deben agregar HU que se van a implementar en la siguiente iteracion.
  - Ordenarlas por modulos y Epica (grupo de HU relacionadas)
  - Marcar el estado de cada HU y Epica como "En progreso" o "Completada" segun corresponda.
---

# Módulos

## Acceso de usuarios

## Inventario

Planes y tasks detallados por HU pendiente en `docs/ai/HU/spec/`:

- `docs/ai/HU/spec/HU-05-proveedores.md`
- `docs/ai/HU/spec/HU-06-dashboard-inventario.md`
- `docs/ai/HU/spec/HU-07-compra-proveedor.md`
- `docs/ai/HU/spec/HU-08-venta-productos.md`
- `docs/ai/HU/spec/HU-09-consumo-insumos.md`
- `docs/ai/HU/spec/HU-10-ajustes-y-mermas.md`

### Épica: Catálogo de artículos, productos, insumos y proveedores

**Estado:** Pendiente (en análisis)

#### HU-04: Gestión de categorías y catálogo separado de productos e insumos

**Estado:** Completada (rama `feature/inventario`)

**Decisiones acordadas:** código manual y único (se normaliza a mayúsculas); unidad de lista fija (`UNIDAD`, `METRO`, `HOJA`, `ROLLO`, `ML`, `KG`, `LITRO`); SKU manual y único, obligatorio en PRODUCTO; una variante de talla/color es un artículo distinto; incluye pantalla básica en `Modules/Inventario/Pages`.

**Como** Ususario trabajador de la tienda **Quiero** registrar categorías (`CATEGORIA_ARTICULO`) y dar de alta artículos diferenciando si son `PRODUCTO` de venta o `INSUMO` de personalización **Para** mantener separados el catálogo comercial y el de taller compartiendo los datos base de `ARTICULO`

- **Criterios de aceptación:**
  - Todo registro crea un `ARTICULO` con `codigo` único, `nombre`, `unidad`, `tipo`, `costo_unitario`, `activo` y `categoria_id`.
  - Si es producto de venta, crea su registro hijo en `PRODUCTO` con `precio_venta > 0`.
  - Si es insumo de personalización, crea su registro hijo en `INSUMO` con su `descripcion`.
  - Un `ARTICULO` pertenece exactamente a `PRODUCTO` o a `INSUMO`, nunca a ambos.

#### HU-05: Gestión de proveedores y teléfonos de contacto

**Estado:** Pendiente (en análisis)

**Como** Usuario trabajador **Quiero** registrar y actualizar proveedores con sus teléfonos de contacto **Para** asociarlos a las compras de mercancía e insumos

- **Criterios de aceptación:**
  - Permite registrar `PROVEEDOR` segun modelo del MER y uno o varios registros en `TELEFONO`.
  - Permite desactivar un proveedor sin eliminar las compras históricas registradas en `MOVIMIENTO_INVENTARIO`.
  - No permite seleccionar proveedores inactivos en nuevas compras.

#### HU-06: Dashboard resumen de ambos inventarios

**Estado:** Pendiente (en análisis)

**Como** Usuario trabajador de la tienda **Quiero** visualizar un resumen separado del stock de productos de venta y del stock de insumos **Para** identificar existencias disponibles, artículos sin stock e insumos críticos para el taller

- **Criterios de aceptación:**
  - El stock actual de cada artículo se calcula a partir de `DETALLE_MOVIMIENTO_INVENTARIO` (entradas menos salidas más/menos ajustes).
  - Muestra vistas o filtros separados para `PRODUCTO` e `INSUMO`.
  - Destaca artículos agotados (`stock = 0`) o con bajo nivel de existencias.

### Épica: Movimientos de inventario y casos especiales

**Estado:** Pendiente (en análisis)

#### HU-07: Entrada de inventario por compra a proveedor

**Estado:** Pendiente (en análisis)

**Como** Usuario trabajador **Quiero** registrar una compra de proveedor que pueda incluir productos de venta, insumos o ambos en un mismo movimiento **Para** incrementar el stock y guardar el costo real pagado en esa compra

- **Criterios de aceptación:**
  - Crea un `MOVIMIENTO_INVENTARIO` con `tipo_movimiento = ENTRADA`, `motivo = COMPRA`, `proveedor_id` obligatorio y `registrado_por_usuario_id`.
  - Cada línea en `DETALLE_MOVIMIENTO_INVENTARIO` exige `cantidad > 0` y `costo_unitario >= 0`.
  - Permite incluir en el mismo movimiento tanto artículos de tipo `PRODUCTO` como `INSUMO` cuando provienen de la misma factura/compra.
  - Opcionalmente actualiza el `costo_unitario` de referencia en `ARTICULO` sin alterar el costo histórico de movimientos anteriores.

#### HU-08: Salida de inventario por venta de productos

**Estado:** Pendiente (en análisis)

**Como** Usuario trabajador **Quiero** registrar la salida por venta de productos al público **Para** descontar existencias del inventario comercial y conservar el precio de venta aplicado

- **Criterios de aceptación:**
  - Crea un `MOVIMIENTO_INVENTARIO` con `tipo_movimiento = SALIDA` y `motivo = VENTA` (con `cliente_id` opcional o requerido si es venta a crédito).
  - Rechaza la operación si alguna línea intenta vender un artículo cuyo subtipo sea `INSUMO`.
  - Rechaza la operación si la `cantidad` solicitada supera el stock disponible del `PRODUCTO`.
  - Guarda en `DETALLE_MOVIMIENTO_INVENTARIO` el `precio_unitario` vigente al momento de la venta (`proveedor_id` y `orden_servicio_id` deben ser nulos).

#### HU-09: Salida de inventario por consumo de insumos en una orden de servicio

**Estado:** Pendiente (en análisis)

**Como** Usuario trabajador **Quiero** registrar los insumos consumidos asociándolos a una orden de servicio **Para** descontar del inventario de taller exactamente lo utilizado en cada pedido

- **Criterios de aceptación:**
  - Crea un `MOVIMIENTO_INVENTARIO` con `tipo_movimiento = SALIDA`, `motivo = CONSUMO` y `orden_servicio_id` obligatorio (`proveedor_id` nulo).
  - Rechaza el movimiento si se intenta consumir un artículo de subtipo `PRODUCTO`.
  - Rechaza el movimiento si la cantidad a consumir supera el stock actual del `INSUMO`.

#### HU-10: Ajustes de inventario, mermas de taller

**Estado:** Pendiente (en análisis)

**Como** Usuario trabajador de la tienda **Quiero** registrar ajustes de inventario, mermas por pruebas/errores de estampado **Para** cuadrar el stock físico con el sistema dejando constancia del motivo y las notas

- **Criterios de aceptación:**
  - Todo movimiento con `motivo = AJUSTE` o `MERMA` exige `notas` obligatorias explicando la causa y que el usuario tenga rol autorizado.
  - Si una merma ocurre durante la ejecución de un pedido (ej. papel DTF o sublimación dañado), permite vincular el consumo adicional a la `ORDEN_SERVICIO` sin alterar el precio cobrado al cliente.
  - Ningún ajuste de salida puede dejar el stock resultante de un artículo en negativo.

## Servicios de personalización

## CRM y clientes
