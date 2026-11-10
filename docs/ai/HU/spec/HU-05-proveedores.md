---
hu: HU-05
titulo: Gestión de proveedores y teléfonos de contacto
epica: Catálogo de artículos, productos, insumos y proveedores
modulo: Inventario
estado: Pendiente
rama: feature/inventario
---

# HU-05: Gestión de proveedores y teléfonos de contacto

## 1. Descripción

Permitir a un trabajador registrar, actualizar y desactivar proveedores con uno o varios teléfonos de contacto, sin perder el historial de compras (`MOVIMIENTO_INVENTARIO`) y evitando que los proveedores inactivos se usen en compras nuevas.

## 2. Cómo se va a implementar (según `AGENTS.md`)

- Módulo dueño: `src/SistemaDDJ.Web/Modules/Inventario`. No requiere comunicación con otros módulos.
- Tabla hija `inventario.telefono_proveedor` (ID `int` identity, `proveedor_id` FK a `inventario.proveedor` con `ON DELETE RESTRICT`, `numero`, `etiqueta` opcional, `es_principal`). Se conserva `proveedor.telefono` o se migra a la tabla hija según se decida antes de implementar.
- Casos de uso en `Modules/Inventario/Features/` (un archivo por caso de uso):
  - `RegistrarProveedor`: crea el proveedor activo y sus teléfonos en una transacción; valida razón social obligatoria, al menos un teléfono válido y unicidad de `identificacion_fiscal` cuando se informe.
  - `ActualizarProveedor`: edita datos del proveedor y reemplaza/actualiza su lista de teléfonos.
  - `CambiarEstadoProveedor`: activa o desactiva (`activo = false`) sin borrar filas ni afectar movimientos históricos.
  - `ListarProveedores`: devuelve proveedores para gestión y un filtro de solo activos para el selector de compras (HU-07).
- UI básica en `Modules/Inventario/Pages/Inventory.razor` (o subcomponente del módulo) para listar, crear, editar y activar/desactivar proveedores.

## 3. Archivos a crear o modificar

- `src/SistemaDDJ.Web/Modules/Inventario/Entities/MovimientoInventario.cs` o nuevo `Proveedor.cs`: entidad `TelefonoProveedor` y ajustes en `Proveedor`.
- `src/SistemaDDJ.Web/Modules/Inventario/Data/InventarioDbContext.cs` y `Data/Configurations/InventarioConfigurations.cs`: `DbSet<TelefonoProveedor>` y configuración de tabla/índices.
- `src/SistemaDDJ.Web/Modules/Inventario/Data/Migrations/`: nueva migración para `telefono_proveedor`.
- `src/SistemaDDJ.Web/Infrastructure/Persistence/DevelopmentDataSeeder.cs`: sembrar teléfonos de los proveedores de prueba.
- `src/SistemaDDJ.Web/Modules/Inventario/Features/RegistrarProveedor/RegistrarProveedor.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Features/ActualizarProveedor/ActualizarProveedor.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/Features/CambiarEstadoProveedor/CambiarEstadoProveedor.cs`.
- `src/SistemaDDJ.Web/Modules/Inventario/InventarioModule.cs`: registro de handlers.
- `src/SistemaDDJ.Web/Modules/Inventario/Pages/Inventory.razor`: sección de proveedores.
- `docs/ai/Diagrams/SistemaDDJ-MER.mmd`, `docs/ai/ARCHITECTURE.md` y `docs/ai/MEMORY.md`: reflejar `TELEFONO_PROVEEDOR`.
- `tests/SistemaDDJ.Tests/Inventario/ProveedoresTests.cs`: pruebas de negocio.

## 4. Casos límite y dudas a resolver antes de implementar

- **Tabla de teléfonos vs columna `proveedor.telefono`:** la HU pide uno o varios registros en `TELEFONO`, mientras el MER actual tiene `proveedor.telefono`. Propuesta recomendada: crear `TELEFONO_PROVEEDOR` en el esquema `inventario` y eliminar `proveedor.telefono` en una migración que copie el valor existente.
  Respuesta: Crear la tabla hija `telefono_proveedor`
- **`identificacion_fiscal`:** ¿sigue siendo opcional y única (para permitir proveedores informales) o pasa a obligatoria?
  Respuesta: Se mantiene opcional y única, para permitir proveedores informales.
- **Proveedores sin teléfonos:** ¿es obligatorio registrar al menos un teléfono o puede quedar vacío?
  Respuesta: No es obligatorio
- **Duplicados de teléfono dentro del mismo proveedor:** se deben rechazar o consolidar.
  Respuesta: Se debe rechazar

## 5. Pruebas a crear (`tests/SistemaDDJ.Tests/Inventario/ProveedoresTests.cs`)

- `RegistrarProveedor_ConVariosTelefonos_GuardaProveedorYTelefonos`.
- `RegistrarProveedor_RechazaIdentificacionFiscalDuplicada`.
- `ActualizarProveedor_ModificaDatosYTelefonos`.
- `DesactivarProveedor_ConservaMovimientosDeCompraHistoricos`.
- `ListarProveedoresActivos_ExcluyeInactivos` (la validación al registrar una compra se prueba también en HU-07).

## 6. Tasks

- [x] Confirmar las dudas de la sección 4 con el usuario.
- [x] **Red:** escribir `ProveedoresTests.cs` y verificar que falla.
- [x] Agregar entidad `TelefonoProveedor`, configuración EF y migración en `InventarioDbContext` (actualizando el seeder, MER y `ARCHITECTURE.md`).
- [x] **Green:** implementar `RegistrarProveedor`, `ActualizarProveedor` y `CambiarEstadoProveedor`, y registrarlos en `InventarioModule`.
- [x] Ejecutar `dotnet test` sobre `web_ddj_tests` y aplicar la migración en `web_ddj`.
- [x] Agregar la interfaz de proveedores en `Modules/Inventario/Pages` y su prueba de humo si aplica.
- [x] **Refactor**, actualizar `HU-IMPLEMENT.md` y `MEMORY.md`, y hacer commit (`feat(inventario): HU-05 ...`) en `feature/inventario`.
