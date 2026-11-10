---
hu: HU-10
titulo: Ajustes de inventario, mermas de taller y devoluciones
epica: Movimientos de inventario y casos especiales
modulo: Inventario
estado: Pendiente
rama: feature/inventario
---

# HU-10: Ajustes de inventario, mermas de taller y devoluciones

## 1. Descripción

Registrar ajustes de inventario (entradas o salidas por conteo físico) y mermas de taller (salidas por pruebas o errores de estampado), exigiendo siempre `notas` con la causa y un usuario con rol autorizado. Cuando una merma ocurre durante un pedido, permite vincularla opcionalmente a la `ORDEN_SERVICIO` sin alterar el `total_cotizado`, el `total_final` ni los cargos en la cuenta del cliente. Ningún movimiento de salida puede dejar el stock resultante en negativo.

## 2. Cómo se va a implementar (según `AGENTS.md`)

- Módulo dueño: `src/SistemaDDJ.Web/Modules/Inventario`.
- Colaboración por `PublicApi/`:
  - `IAccesoPublicApi`: verificar que `RegistradoPorUsuarioId` esté activo y tenga rol autorizado (`Admin` o `Trabajador`, según HU-02).
  - `IPersonalizacionPublicApi`: si el movimiento es `MERMA` y trae `OrdenServicioId`, verificar que la orden exista sin modificar sus importes ni generar cargos en `Clientes`.
- Caso de uso `Modules/Inventario/Features/RegistrarAjusteOMerma/RegistrarAjusteOMerma.cs`:
  - Comando con `TipoMovimiento` (`ENTRADA` o `SALIDA`), `Motivo` (`AJUSTE` o `MERMA`), `Notas` obligatorias, `RegistradoPorUsuarioId`, `OrdenServicioId` opcional, `Referencia` opcional y líneas (`ArticuloId`, `Cantidad`).
  - Validaciones:
    - `Notas` no vacías.
    - Usuario activo y con rol autorizado.
    - Si `Motivo == MERMA`: `TipoMovimiento` debe ser `SALIDA`; si se indica `OrdenServicioId`, la orden debe existir.
    - Si `Motivo == AJUSTE`: `OrdenServicioId` debe ser nulo; permite `ENTRADA` (sobrante) o `SALIDA` (faltante).
    - Si `TipoMovimiento == SALIDA`: para cada artículo, el stock disponible actual debe ser mayor o igual a la cantidad total a descontar (nunca deja stock negativo).
- UI en `Modules/Inventario/Pages`: formulario de ajuste/merma con campo de notas obligatorio y selector opcional de orden de servicio cuando el motivo es `MERMA`.

## 3. Archivos a crear o modificar

- `src/SistemaDDJ.Web/Modules/Acceso/PublicApi/IAccesoPublicApi.cs`: consulta de autorización por rol para operaciones de inventario.
- `src/SistemaDDJ.Web/Modules/Inventario/Features/RegistrarAjusteOMerma/RegistrarAjusteOMerma.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/InventarioModule.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Pages/Inventory.razor`.
- `tests/SistemaDDJ.Tests/Inventario/AjustesYMermasTests.cs`.
- `docs/ai/HU/HU-IMPLEMENT.md` y `docs/ai/MEMORY.md`.

## 4. Casos límite y dudas a resolver antes de implementar

- **Devoluciones:** el título de la HU menciona "devoluciones", pero sus criterios de aceptación solo definen `AJUSTE` y `MERMA`. Definir si una devolución se registra como `AJUSTE` de entrada/salida con nota explicativa o si se agrega el motivo `DEVOLUCION` al enum y al CHECK de base de datos.
  Respuesta: Dejemos las devoluciones para iteraciones futuras, no lo contemples
- **Subtipo en `MERMA`:** ¿una merma solo puede aplicarse a `INSUMO` o también a un `PRODUCTO` dañado en tienda/taller (por ejemplo, una taza o franela dañada)?
  Respuesta: Solo a insumos
- **Roles autorizados:** según HU-02, tanto `Trabajador` como `Admin` pueden hacer ajustes manuales de inventario; confirmar que ambos roles están autorizados y que un usuario inactivo o sin esos roles se rechaza.
  Respuesta: Exacto, ambos pueden hacerlo. Los inactivos no

## 5. Pruebas a crear (`tests/SistemaDDJ.Tests/Inventario/AjustesYMermasTests.cs`)

- `RegistrarAjuste_SinNotas_EsRechazado`.
- `RegistrarAjuste_ConUsuarioSinRolAutorizadoOInactivo_EsRechazado`.
- `RegistrarAjusteSalida_QueDejariaStockNegativo_EsRechazado`.
- `RegistrarAjusteEntradaYSalida_CuadraElStockCalculado`.
- `RegistrarMerma_VinculadaAOrdenDeServicio_DescuentaStockSinAlterarTotalesDeLaOrden`.

## 6. Tasks

- [x] Confirmar con el usuario las dudas de la sección 4 (devoluciones, subtipo en merma y roles).
- [ ] **Red:** escribir `AjustesYMermasTests.cs` y comprobar que falla.
- [ ] Ampliar `IAccesoPublicApi` para validar rol autorizado del usuario.
- [ ] **Green:** implementar `RegistrarAjusteOMerma` y registrarlo en `InventarioModule`.
- [ ] Agregar el formulario de ajustes y mermas en la UI de Inventario.
- [ ] Ejecutar `dotnet test`, actualizar `HU-IMPLEMENT.md` y `MEMORY.md`, hacer commit (`feat(inventario): HU-10 ...`) en `feature/inventario` y preparar el cierre/PR de la Épica 2 de Inventario.
