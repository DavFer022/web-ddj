---
hu: HU-06
titulo: Dashboard resumen de ambos inventarios
epica: Catálogo de artículos, productos, insumos y proveedores
modulo: Inventario
estado: Pendiente
rama: feature/inventario
---

# HU-06: Dashboard resumen de ambos inventarios

## 1. Descripción

Mostrar un resumen separado del inventario de productos de venta y del inventario de insumos de taller, calculando el stock actual desde `DETALLE_MOVIMIENTO_INVENTARIO` y destacando los artículos agotados (`stock = 0`) o con bajo nivel de existencias.

## 2. Cómo se va a implementar (según `AGENTS.md`)

- Módulo dueño: `src/SistemaDDJ.Web/Modules/Inventario`.
- Cálculo de stock en una consulta proyectada: para cada `ARTICULO` activo, `SUM(ENTRADA) - SUM(SALIDA)` sobre `detalle_movimiento_inventario` + `movimiento_inventario`. Los artículos sin movimientos tienen `stock = 0`.
- Estado de stock por fila: `AGOTADO` (`stock == 0`), `BAJO` (`stock > 0 && stock <= umbral`), `DISPONIBLE` (`stock > umbral`).
- Caso de uso en `Modules/Inventario/Features/ConsultarDashboardInventario/ConsultarDashboardInventario.cs`:
  - Recibe filtro opcional por `ArticuloTipo` (`PRODUCTO` o `INSUMO`), búsqueda por código/nombre y filtro de solo alertas (agotados o bajo stock).
  - Devuelve totales de resumen (cantidad de artículos, agotados, bajo stock) y las listas separadas o filtradas de productos (con `sku` y `precio_venta`) e insumos (con `descripcion`).
- UI en `Modules/Inventario/Pages/Inventory.razor`: pestañas o vistas separadas para Productos e Insumos, tarjetas de conteo y resaltado visual de agotados y bajo stock.

## 3. Archivos a crear o modificar

- Si se elige `stock_minimo` por artículo: `Entities/Articulo.cs`, `Data/Configurations/InventarioConfigurations.cs`, nueva migración en `Data/Migrations/`, `DevelopmentDataSeeder.cs`, `RegistrarArticulo.cs`, MER y `ARCHITECTURE.md`.
- `src/SistemaDDJ.Web/Modules/Inventario/Features/ConsultarDashboardInventario/ConsultarDashboardInventario.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/InventarioModule.cs`: registro del handler.
- `src/SistemaDDJ.Web/Modules/Inventario/Pages/Inventory.razor`: vista del dashboard y filtros.
- `tests/SistemaDDJ.Tests/Inventario/DashboardInventarioTests.cs`.
- `docs/ai/HU/HU-IMPLEMENT.md` y `docs/ai/MEMORY.md`.

## 4. Casos límite y dudas a resolver antes de implementar

- **Criterio de "bajo nivel de existencias":** el MER no tiene columna de umbral. Opciones:
  1. Agregar `stock_minimo` (`numeric(18,3) >= 0`, por defecto 0) en `ARTICULO` (recomendado, porque comparar hojas de papel con rollos de vinil o franelas requiere umbrales distintos).
  2. Usar un umbral fijo configurable en la aplicación.
     Respuesta: Usar opcion 1.
- **Artículos inactivos:** ¿se ocultan siempre del dashboard o solo se muestran si aún tienen stock positivo?
  Respuesta: Se siguen mostrando

## 5. Pruebas a crear (`tests/SistemaDDJ.Tests/Inventario/DashboardInventarioTests.cs`)

- `ConsultarDashboard_CalculaStockConEntradasSalidasYAjustes`.
- `ConsultarDashboard_ArticuloSinMovimientos_TieneStockCeroYEstadoAgotado`.
- `ConsultarDashboard_SeparaProductosEInsumos`.
- `ConsultarDashboard_MarcaArticuloConBajoStockSegunUmbral`.

## 6. Tasks

- [x] Confirmar con el usuario cómo se define el umbral de bajo stock y el trato a artículos inactivos.
- [x] **Red:** escribir `DashboardInventarioTests.cs` y comprobar que falla.
- [x] Si se aprueba `stock_minimo`, agregar la propiedad, restricción CHECK y migración.
- [x] **Green:** implementar `ConsultarDashboardInventario` y registrarlo en `InventarioModule`.
- [x] Integrar el dashboard en `/inventario` con vistas separadas para `PRODUCTO` e `INSUMO` y resaltado de alertas.
- [x] Ejecutar `dotnet test`, actualizar `HU-IMPLEMENT.md` y `MEMORY.md`, hacer commit (`feat(inventario): HU-06 ...`) en `feature/inventario` y preparar el cierre/PR de la Épica 1 de Inventario cuando HU-04, HU-05 y HU-06 estén validadas. La épica 1 queda completada; el PR se prepara al final del lote.
