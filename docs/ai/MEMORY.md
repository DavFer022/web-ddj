---
titulo: Memoria del proyecto
descripcion: Memoria a corto plazo del estado actual del proyecto, Maximo ~50 lineas, resume o elimina lo que no sea relevante para el desarrollo de nuevas funcionalidades.
---

# Memoria del proyecto

## Estado actual

- Rama `feature/modelo-base-datos`: estructura modular y modelo de datos del MER implementados, sin commit.
- Módulos: `Acceso`, `Inventario`, `Personalizacion`, `Clientes`, `Notificaciones` en `src/SistemaDDJ.Web/Modules/`. Cada uno con su `DbContext` y esquema PostgreSQL.
- Base `web_ddj` limpiada y migrada con la estructura nueva (19 tablas en 5 esquemas, 13 FK entre esquemas en `IntegracionDbContext`).
- Datos de prueba en `DevelopmentDataSeeder` (Development, en una sola transacción, omitido si ya hay usuarios o migraciones pendientes).
- HU-04 completada en `feature/hu-04-catalogo-articulos` (categorías, artículos producto/insumo, pantalla básica en `/inventario`). Pruebas: 12 pasan, incluidas 10 de catálogo con PostgreSQL aislado (`web_ddj_tests`).
- Pendientes de Inventario: HU-05 a HU-10 (ver `HU-IMPLEMENT.md`).

## Decisiones y por que

- Monolito modular con VSA dentro de cada módulo: los módulos definen límites de datos y las features organizan casos de uso.
- Un esquema y un `DbContext` por módulo, sin usuarios de base de datos separados (los esquemas dan propiedad lógica, no seguridad).
- FK entre esquemas mantenidas en la base (`ON DELETE RESTRICT`) para proteger la integridad. Se crean en `IntegracionDbContext` porque el MER tiene dependencias circulares entre módulos.
- IDs: `int` identity en catálogos y relaciones; UUIDv7 en movimientos, clientes, órdenes, cuenta, notificaciones y fidelidad.
- Snake_case para tablas y columnas; enums como texto con CHECK constraints.
- Stock calculado desde `detalle_movimiento_inventario`; el saldo de cliente se calcula desde sus movimientos.
- Sin contraseñas en `acceso.usuario`: autenticación pendiente (se implementará más adelante).
- `producto.sku` agregado según el MER (cambio hecho por el usuario).
- Los datos de prueba se siembran por etapas, usando una conexión compartida, porque cada `DbContext` tiene su propia conexión.
- Unidades de artículo en lista fija (`UnidadMedida`), código y SKU manuales y únicos (normalizados a mayúsculas).
- Pruebas de integración: base `*_tests` indicada por `DDJ_TEST_CONNECTION`; la fixture se niega a usar otras bases.

## Aprendizajes y Errores a evitar

- Sin perfil de lanzamiento, `dotnet run` arranca en Production y no carga User Secrets: usar `ASPNETCORE_ENVIRONMENT=Development`.
- Una transacción de una conexión no sirve en otra: los contextos que participan en una transacción deben compartir la conexión.
- Con IDs `int` generados por la base, los datos dependientes se guardan después de los padres (`SaveChanges` por etapas).
- Cada contexto necesita su propio `MigrationsHistoryTable` dentro de su esquema.
- Un módulo no puede referenciar entidades de otro en su `DbContext`; las FK cruzadas se crean en la migración de integración.

## Proximos pasos

- Definir pendientes de diseño de HU: `tipo` de artículo, generación de `codigo`, unidades, stock mínimo, devoluciones, correcciones de movimientos.
- Crear `PublicApi` de cada módulo cuando un caso de uso necesite llamar a otro módulo.
- Implementar HU de inventario empezando por HU-04 (Red-Green-Refactor).
