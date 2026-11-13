---
titulo: Memoria del proyecto
descripcion: Memoria a corto plazo del estado actual del proyecto, Maximo ~50 lineas, resume o elimina lo que no sea relevante para el desarrollo de nuevas funcionalidades.
---

# Memoria del proyecto

## Estado actual

- Rama `feature/inventario`: HU-04 a HU-10 completadas. Inventario tiene catálogo, proveedores con teléfonos, dashboard de stock, compras, ventas, consumo de insumos y ajustes/mermas.
- Pruebas: 54 pasan con PostgreSQL aislado (`web_ddj_tests`); las migraciones nuevas también están aplicadas en `web_ddj`.
- UI migrada a **MudBlazor 9.12.0** con layout, tema propio y selectores con autocompletado (`MudAutocomplete`). Rutas: `/inventario` (solo dashboard y catálogo con botón FAB sticky), `/inventario/catalogo` (alta de categorías y artículos), `/inventario/proveedores`, `/inventario/compras` (con historial de compras), `/inventario/ventas`, `/inventario/consumos` y `/inventario/ajustes`.
- Pendiente: preparar el PR/cierre de las dos épicas de Inventario. Los demás módulos siguen con páginas placeholder y sin lógica de negocio.

## Decisiones y por que

- Monolito modular con VSA: cada módulo es dueño de su esquema y `DbContext`; las FK cruzadas viven en `IntegracionDbContext`.
- MudBlazor se registra con `AddMudServices()`; el tema y los providers viven en el layout, y el CSS/JS se cargan desde `_content/MudBlazor`.
- La colaboración entre módulos usa `PublicApi` (`IAccesoPublicApi`, `IClientesPublicApi`, `IPersonalizacionPublicApi`); nunca se inyecta otro `DbContext`.
- El usuario actual es temporal: devuelve el primer usuario activo hasta que HU-01 implemente autenticación.
- Stock calculado desde `detalle_movimiento_inventario`: entradas suman, salidas restan; `stock_minimo` en `articulo` define bajo stock y 0 es agotado.
- Proveedores usan `telefono_proveedor` (varios teléfonos, único por proveedor, máximo un principal); `proveedor.telefono` fue eliminado copiando los datos existentes.
- Compras: líneas repetidas del mismo artículo se consolidan sumando cantidades; el costo de referencia se actualiza por línea con el último costo pagado.
- Ventas: precio fijo desde `producto.precio_venta`; la venta a crédito solo exige `cliente_id` (el cargo en cuenta se hará en HU-19).
- Consumo de insumos: solo órdenes `PENDIENTE` o `EN_PROCESO`; descuenta stock y guarda el costo vigente del insumo.
- Ajustes/mermas: notas obligatorias, roles `Admin`/`Trabajador`, merma solo para insumos y nunca deja stock negativo. Las devoluciones quedan fuera de alcance.
- Los movimientos de compra/venta/consumo/merma guardan snapshots de costo o precio para no alterar el histórico.

## Aprendizajes y Errores a evitar

- Una transacción de una conexión no sirve en otra: los contextos que participan en una transacción deben compartir la conexión.
- La fixture de pruebas migra todos los contextos y al final Integración; por las FK cruzadas, las pruebas deben crear usuarios, clientes y órdenes reales.
- `TRUNCATE ... RESTART IDENTITY CASCADE` centralizado conserva los roles semilla y evita conflictos entre pruebas.
- En Razor, `@bind` ya agrega `onchange`; para efectos secundarios usa `@bind:after`.
- Cada contexto necesita su propio `MigrationsHistoryTable` dentro de su esquema.
- Sin perfil de lanzamiento, `dotnet run` arranca en Production y no carga User Secrets: usar `ASPNETCORE_ENVIRONMENT=Development`.

## Próximos pasos

- Revisar y subir el PR de Inventario con el resumen de HU-04 a HU-10.
- Continuar con las HU de Personalización (HU-11 en adelante) en su propia rama de módulo.
- Implementar autenticación (HU-01) para retirar el usuario actual temporal de `IAccesoPublicApi`.
