---
hu: HU-09
titulo: Salida de inventario por consumo de insumos en una orden de servicio
epica: Movimientos de inventario y casos especiales
modulo: Inventario
estado: Pendiente
rama: feature/inventario
---

# HU-09: Salida de inventario por consumo de insumos en una orden de servicio

## 1. Descripción

Registrar los insumos efectivamente utilizados en un pedido mediante un `MOVIMIENTO_INVENTARIO` (`SALIDA` / `CONSUMO`) asociado obligatoriamente a una `ORDEN_SERVICIO`, rechazando artículos de subtipo `PRODUCTO` y evitando consumos que superen el stock disponible del insumo.

## 2. Cómo se va a implementar (según `AGENTS.md`)

- Módulo dueño: `src/SistemaDDJ.Web/Modules/Inventario`.
- Colaboración con `Personalizacion` por `PublicApi/`:
  - `Modules/Personalizacion/PublicApi/IPersonalizacionPublicApi.cs`: consultar si una `orden_servicio_id` existe y si su estado permite registrar consumos, más un listado básico de órdenes activas para el selector de la UI. Inventario nunca inyecta `PersonalizacionDbContext`.
- Caso de uso `Modules/Inventario/Features/RegistrarConsumoInsumos/RegistrarConsumoInsumos.cs`:
  - Comando con `OrdenServicioId` obligatorio, `RegistradoPorUsuarioId`, `Referencia` opcional, `Notas` opcional y líneas (`ArticuloId`, `Cantidad`).
  - Validaciones: orden existe y admite consumo; usuario activo; al menos una línea; `cantidad > 0`; todos los artículos son de subtipo `INSUMO` (rechaza `PRODUCTO`); el stock actual de cada insumo cubre la cantidad total solicitada en el movimiento.
  - Crea `MovimientoInventario` (`TipoMovimiento = SALIDA`, `Motivo = CONSUMO`, `OrdenServicioId`, `ProveedorId = null`, `ClienteId = null`) y sus `DetalleMovimientoInventario` guardando el `CostoUnitario` vigente del insumo (`PrecioUnitario = null`).
- UI en `Modules/Inventario/Pages`: formulario de salida por consumo vinculado a una orden de servicio.

## 3. Archivos a crear o modificar

- `src/SistemaDDJ.Web/Modules/Personalizacion/PublicApi/IPersonalizacionPublicApi.cs` e implementación registrada en `PersonalizacionModule.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Features/RegistrarConsumoInsumos/RegistrarConsumoInsumos.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/InventarioModule.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Pages/Inventory.razor`.
- `tests/SistemaDDJ.Tests/Infrastructure/BaseDatosPruebaInventario.cs`: incluir migraciones de `PersonalizacionDbContext` para cumplir la FK `fk_movimiento_inventario_orden_servicio_id`.
- `tests/SistemaDDJ.Tests/Inventario/ConsumoInsumosTests.cs`.
- `docs/ai/HU/HU-IMPLEMENT.md` y `docs/ai/MEMORY.md`.

## 4. Casos límite y dudas a resolver antes de implementar

- **Estados de `ORDEN_SERVICIO` que permiten consumo:** ¿solo `PENDIENTE` y `EN_PROCESO`, o también `LISTO`? (Las órdenes `CANCELADO` o `ENTREGADO` deberían rechazarse).
  Respuesta: Pendiente y en_proceso
- **Dependencia con HU-13 (creación de órdenes):** como HU-13 aún no está implementada, en las pruebas se insertará una orden de prueba por la API/fixture, y en desarrollo se pueden usar las órdenes creadas por `DevelopmentDataSeeder`.

## 5. Pruebas a crear (`tests/SistemaDDJ.Tests/Inventario/ConsumoInsumosTests.cs`)

- `RegistrarConsumo_DeInsumosConStock_DescuentaExistenciasYAsociaOrden`.
- `RegistrarConsumo_SinOrdenDeServicioOConOrdenInexistente_EsRechazado`.
- `RegistrarConsumo_RechazaArticuloDeSubtipoProducto`.
- `RegistrarConsumo_RechazaCantidadMayorAlStockDelInsumo`.

## 6. Tasks

- [x] Confirmar con el usuario qué estados de orden permiten registrar consumos.
- [x] **Red:** escribir `ConsumoInsumosTests.cs` y comprobar que falla.
- [x] Crear `IPersonalizacionPublicApi` en `Modules/Personalizacion/PublicApi` y registrarla en `PersonalizacionModule`.
- [x] **Green:** implementar `RegistrarConsumoInsumos` y registrarlo en `InventarioModule`.
- [x] Agregar el formulario de consumo de insumos en la UI de Inventario.
- [x] Ejecutar `dotnet test`, actualizar `HU-IMPLEMENT.md` y `MEMORY.md`, y hacer commit (`feat(inventario): HU-09 ...`) en `feature/inventario`.
