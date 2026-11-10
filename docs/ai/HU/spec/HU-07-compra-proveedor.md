---
hu: HU-07
titulo: Entrada de inventario por compra a proveedor
epica: Movimientos de inventario y casos especiales
modulo: Inventario
estado: Pendiente
rama: feature/inventario
---

# HU-07: Entrada de inventario por compra a proveedor

## 1. Descripción

Registrar una compra a un proveedor activo mediante un `MOVIMIENTO_INVENTARIO` (`ENTRADA` / `COMPRA`) que pueda combinar líneas de `PRODUCTO` e `INSUMO`, incrementando el stock, guardando el costo pagado en cada línea y actualizando opcionalmente el `costo_unitario` de referencia en `ARTICULO`.

## 2. Cómo se va a implementar (según `AGENTS.md`)

- Módulo dueño: `src/SistemaDDJ.Web/Modules/Inventario`.
- Colaboración con `Acceso`: para validar `registrado_por_usuario_id` sin tocar `AccesoDbContext`, exponer un contrato mínimo en `Modules/Acceso/PublicApi/IAccesoPublicApi.cs` (por ejemplo, verificar usuario activo y obtener el usuario actual temporal mientras HU-01 no esté implementada).
- Caso de uso `Modules/Inventario/Features/RegistrarCompraProveedor/RegistrarCompraProveedor.cs`:
  - Comando con `ProveedorId`, `RegistradoPorUsuarioId`, `Referencia` opcional, `Notas` opcional, `ActualizarCostoReferencia` (`bool`) y lista de líneas (`ArticuloId`, `Cantidad`, `CostoUnitario`).
  - Validaciones: proveedor existe y `activo = true`; usuario activo; al menos una línea; sin artículos repetidos o consolidados; todos los artículos existen y están activos; `cantidad > 0` y `costo_unitario >= 0` en cada línea.
  - En una sola transacción crea `MovimientoInventario` (`TipoMovimiento = ENTRADA`, `Motivo = COMPRA`, `ClienteId = null`, `OrdenServicioId = null`) y sus `DetalleMovimientoInventario` (`PrecioUnitario = null`).
  - Si `ActualizarCostoReferencia` es `true`, actualiza `Articulo.CostoUnitario` con el costo de la línea sin modificar movimientos anteriores.
- UI en `Modules/Inventario/Pages`: formulario de compra con selector de proveedores activos y líneas dinámicas de artículos.

## 3. Archivos a crear o modificar

- `src/SistemaDDJ.Web/Modules/Acceso/PublicApi/IAccesoPublicApi.cs` e implementación registrada en `AccesoModule.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Features/RegistrarCompraProveedor/RegistrarCompraProveedor.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/InventarioModule.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Pages/Inventory.razor` (o página/componente de movimientos).
- `tests/SistemaDDJ.Tests/Infrastructure/BaseDatosPruebaInventario.cs`: habilitar semilla/limpieza del esquema `acceso` e `integracion` para probar las FK reales.
- `tests/SistemaDDJ.Tests/Inventario/CompraProveedorTests.cs`.
- `docs/ai/HU/HU-IMPLEMENT.md` y `docs/ai/MEMORY.md`.

## 4. Casos límite y dudas a resolver antes de implementar

- **Usuario que registra la compra sin HU-01 implementada:** usar un servicio de usuario actual con el usuario semilla de desarrollo en la UI y recibir `RegistradoPorUsuarioId` explícito en el comando.
- **Actualización de `costo_unitario`:** ¿la casilla "actualizar costo de referencia" aplica a toda la compra o por cada línea? ¿Toma el último costo pagado o un promedio ponderado?
  Respuesta: Por cada linea y toma el ultimo costo pagado.
- **Artículos repetidos en la misma compra:** ¿se rechaza el comando si el mismo `articulo_id` aparece en dos líneas?
  Respuesta: No, deberia sumar las cantidades

## 5. Pruebas a crear (`tests/SistemaDDJ.Tests/Inventario/CompraProveedorTests.cs`)

- `RegistrarCompra_MixtaProductosEInsumos_IncrementaStockYGuardaCostos`.
- `RegistrarCompra_ConActualizarCostoReferencia_ActualizaArticuloSinAlterarHistorico`.
- `RegistrarCompra_SinActualizarCostoReferencia_ConservaCostoEnArticulo`.
- `RegistrarCompra_RechazaProveedorInactivoOInexistente`.
- `RegistrarCompra_RechazaCantidadNoPositivaOCostoNegativo`.

## 6. Tasks

- [x] Confirmar las dudas de la sección 4 con el usuario.
- [x] **Red:** escribir `CompraProveedorTests.cs` y comprobar que falla.
- [x] Crear `IAccesoPublicApi` en `Modules/Acceso/PublicApi` y registrarla en `AccesoModule`.
- [x] **Green:** implementar `RegistrarCompraProveedor` y registrarlo en `InventarioModule`.
- [x] Agregar el formulario de compra en la UI de Inventario.
- [x] Ejecutar `dotnet test`, actualizar `HU-IMPLEMENT.md` y `MEMORY.md`, y hacer commit (`feat(inventario): HU-07 ...`) en `feature/inventario`.
