---
hu: HU-08
titulo: Salida de inventario por venta de productos
epica: Movimientos de inventario y casos especiales
modulo: Inventario
estado: Pendiente
rama: feature/inventario
---

# HU-08: Salida de inventario por venta de productos

## 1. Descripción

Registrar la salida por venta de productos al público mediante un `MOVIMIENTO_INVENTARIO` (`SALIDA` / `VENTA`), validando que solo se vendan artículos de tipo `PRODUCTO`, que haya stock suficiente y que quede guardado el `precio_unitario` aplicado. Permite asociar un cliente opcional y, si es venta a crédito, exige `cliente_id` y coordina el cargo correspondiente en la cuenta del cliente.

## 2. Cómo se va a implementar (según `AGENTS.md`)

- Módulo dueño del flujo: `src/SistemaDDJ.Web/Modules/Inventario`.
- Colaboración entre módulos por `PublicApi/` (sin inyectar `ClientesDbContext` en Inventario):
  - `Modules/Clientes/PublicApi/IClientesPublicApi.cs`: validar que el cliente exista y esté activo, y registrar el `CARGO` de venta cuando la venta sea a crédito.
  - `Modules/Acceso/PublicApi/IAccesoPublicApi.cs`: validar el usuario que registra la operación.
  - Cuando la venta es a crédito, el caso de uso comparte la misma conexión y transacción PostgreSQL entre `InventarioDbContext` y la operación de `IClientesPublicApi`.
- Caso de uso `Modules/Inventario/Features/RegistrarVentaProductos/RegistrarVentaProductos.cs`:
  - Comando con `RegistradoPorUsuarioId`, `ClienteId` opcional, `EsCredito` (`bool`), `Referencia` opcional, `Notas` opcional y líneas (`ArticuloId`, `Cantidad`, `PrecioUnitario` opcional).
  - Validaciones: al menos una línea; `cantidad > 0`; todos los artículos existen, están activos y son de subtipo `PRODUCTO` (rechaza cualquier `INSUMO`); la cantidad solicitada por artículo no supera su stock disponible actual; si `EsCredito` es `true`, `ClienteId` es obligatorio.
  - Toma como `PrecioUnitario` el `Producto.PrecioVenta` vigente (o el enviado si se permite editarlo, siempre `> 0`) y deja `ProveedorId = null` y `OrdenServicioId = null`.
- UI en `Modules/Inventario/Pages`: formulario de venta de productos con stock disponible visible.

## 3. Archivos a crear o modificar

- `src/SistemaDDJ.Web/Modules/Clientes/PublicApi/IClientesPublicApi.cs` e implementación registrada en `ClientesModule.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Features/RegistrarVentaProductos/RegistrarVentaProductos.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/InventarioModule.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Pages/Inventory.razor`.
- `tests/SistemaDDJ.Tests/Infrastructure/BaseDatosPruebaInventario.cs`: incluir migraciones de `ClientesDbContext` para probar ventas con cliente/crédito.
- `tests/SistemaDDJ.Tests/Inventario/VentaProductosTests.cs`.
- `docs/ai/HU/HU-IMPLEMENT.md` y `docs/ai/MEMORY.md`.

## 4. Casos límite y dudas a resolver antes de implementar

- **Precio unitario en la venta:** ¿se toma siempre fijo desde `PRODUCTO.precio_venta` o el trabajador puede modificarlo por línea (descuento) guardando el precio aplicado?
  Respuesta: Es fijo
- **Alcance de venta a crédito en esta HU:** ¿registramos ya el `CARGO` en `MOVIMIENTO_CUENTA_CLIENTE` mediante `IClientesPublicApi` dentro de la misma transacción, o en HU-08 solo se exige `cliente_id` y el cargo se completa en HU-19?
  Respuesta: solo se exige cliente_id
- **Líneas repetidas del mismo producto:** la validación de stock debe evaluar la suma total solicitada del producto en todo el movimiento, no línea por línea de forma aislada.

## 5. Pruebas a crear (`tests/SistemaDDJ.Tests/Inventario/VentaProductosTests.cs`)

- `RegistrarVenta_DeProductosConStock_DescuentaExistenciasYGuardaPrecioVigente`.
- `RegistrarVenta_RechazaArticuloDeSubtipoInsumo`.
- `RegistrarVenta_RechazaCantidadMayorAlStockDisponible`.
- `RegistrarVenta_ACreditoSinCliente_EsRechazada`.
- `RegistrarVenta_DejaProveedorYOrdenServicioEnNulo`.

## 6. Tasks

- [x] Confirmar con el usuario las dudas de la sección 4 (precio editable o fijo, y alcance del cargo a crédito).
- [x] **Red:** escribir `VentaProductosTests.cs` y comprobar que falla.
- [x] Crear `IClientesPublicApi` en `Modules/Clientes/PublicApi` y registrarla en `ClientesModule`.
- [x] **Green:** implementar `RegistrarVentaProductos` y registrarlo en `InventarioModule`.
- [x] Agregar el formulario de venta en la UI de Inventario.
- [x] Ejecutar `dotnet test`, actualizar `HU-IMPLEMENT.md` y `MEMORY.md`, y hacer commit (`feat(inventario): HU-08 ...`) en `feature/inventario`.
