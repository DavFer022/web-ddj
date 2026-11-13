# Instrucciones para agentes

## Inicio de cada sesión

- En cada nuevo chat/sesión, lee `docs/ai/ARCHITECTURE.md` antes de analizar o modificar el código.
- Consulta `README.md` para configuración y comandos.
- Consultar `docs/ai/MEMORY.md` para conocer el estado actual del proyecto y decisiones previas.

## Arquitectura y límites

- La solución es un monolito modular Blazor/ASP.NET Core en .NET 10. Cada módulo de negocio vive en `src/SistemaDDJ.Web/Modules/<Modulo>` y es dueño de sus datos.
- Módulos actuales (nombres en español): `Acceso`, `Inventario`, `Personalizacion`, `Clientes` y `Notificaciones`. Agregar o renombrar un módulo requiere actualizar este archivo y `docs/ai/ARCHITECTURE.md`.
- Estructura de cada módulo: `PublicApi/`, `Entities/`, `Data/` (DbContext, `Configurations/` y `Migrations/`), `Features/` y `Pages/`. En la raíz va `<Modulo>Module.cs` con el método `Add<Modulo>Module`, que es su único punto de registro.
- Dentro de `Features/<CasoDeUso>/` se usa Vertical Slice Architecture: request, validación, handler y endpoint juntos en un mismo archivo. Se separa en varios archivos solo cuando el archivo queda difícil de leer.
- Un módulo accede a datos de otro únicamente mediante la `PublicApi/` de ese módulo. Prohibido inyectar otro `DbContext` o usar entidades, configuraciones o migraciones de otro módulo.
- Los contratos de `PublicApi/` son DTOs o records; nunca se exponen entidades de EF.
- No se permiten dependencias circulares entre módulos. Un flujo que abarque varios módulos (por ejemplo, venta + cargo en cuenta) lo orquesta un único caso de uso que llama a las APIs públicas dentro de una transacción.
- Cada módulo tiene su esquema PostgreSQL (`acceso`, `inventario`, `personalizacion`, `clientes`, `notificaciones`), su `DbContext` y su tabla de historial de migraciones dentro de su esquema.
- Las claves foráneas entre esquemas viven solo en `Infrastructure/Persistence/Integracion/Migrations` (`IntegracionDbContext`), que se aplica después de los módulos. No agregues FK entre esquemas en las migraciones de un módulo.
- `Infrastructure/Persistence` contiene solo helpers técnicos compartidos (convenciones de nombres, registro de contextos, integración). No pongas lógica de negocio allí ni crees carpetas `Shared` de negocio.
- Identificadores: `int` autoincremental para catálogos y tablas de relación. `uuid` (UUIDv7 con `Guid.CreateVersion7()`) solo para `MOVIMIENTO_INVENTARIO`, `CLIENTE`, `ORDEN_SERVICIO`, `MOVIMIENTO_CUENTA_CLIENTE`, `NOTIFICACION` y `MOVIMIENTO_FIDELIDAD_CLIENTE`. Cada FK usa el tipo de la clave que referencia.
- Las bajas son lógicas (`activo = false`). No borres filas con historial; las FK usan `ON DELETE RESTRICT`.
- Inventario ya tiene UI y lógica funcional; `Acceso`, `Personalizacion` y `Clientes` siguen como placeholders. No asumas que la autenticación está implementada.
- La UI usa MudBlazor. El tema global vive en `Components/Theme/DdjTheme.cs`; los componentes compartidos van en `Components/Shared`.
- No guardes contraseñas ni cadenas de conexión reales en archivos versionados; usa .NET User Secrets localmente.

## Stack y arquitectura

- Blazor y ASP.NET Core sobre .NET 10
- MudBlazor para la interfaz (tema propio, layout y componentes)
- PostgreSQL con EF Core Code First, un `DbContext` por módulo
- Monolito modular; VSA dentro de cada módulo

## Comandos

- Compilar: `dotnet build SistemaDDJ.sln`.
- Ejecutar: `dotnet run --project src/SistemaDDJ.Web`.
- Ejecutar pruebas: `dotnet test SistemaDDJ.sln`.
- Restaurar la herramienta EF local: `dotnet tool restore`.
- Crear migración de un módulo: `dotnet ef migrations add <Nombre> --context <Modulo>DbContext --project src/SistemaDDJ.Web --output-dir Modules/<Modulo>/Data/Migrations`.
- Crear migración de integración: `dotnet ef migrations add <Nombre> --context IntegracionDbContext --project src/SistemaDDJ.Web --output-dir Infrastructure/Persistence/Integracion/Migrations`.
- Aplicar migraciones: primero los contextos de los módulos y al final `IntegracionDbContext`, con `dotnet ef database update --context <Contexto> --project src/SistemaDDJ.Web`.
- No edites migraciones ya aplicadas; crea una nueva.

## Flujo de trabajo para implementación

- Leer `docs/ai/HU/Backlog.md` para identificar las HU a implementar. Siempre trabajar enfocado a un solo modulo.
- Pasar las HU seleccionas al archivo `docs/ai/HU/HU-IMPLEMENT.md` para formalizar la implementacion e implementar en la siguiente iteración.
- Antes de implementar cada HU, crear y ejecutar primero las pruebas de sus reglas y recorridos críticos; seguir Red-Green-Refactor. Priorizar comportamiento de negocio, no una prueba por cada función.
- Ubicar las pruebas en `tests/SistemaDDJ.Tests`. Las pruebas de integración con PostgreSQL deben usar una base aislada, nunca la base de desarrollo.
- Trabajar todas las HU solicitadas en la sesión dentro de una única rama del módulo (`feature/<modulo>`, por ejemplo `feature/inventario`), salvo que el usuario pida explícitamente una rama aparte para una HU (en cuyo caso se fusiona a la rama del módulo al completarse y se elimina la rama hija con `git branch -d`).
- Hacer un commit al completar y validar cada HU (`feat(<modulo>): HU-XX <resumen>`) antes de iniciar la siguiente.
- Para cada Epica completada y validada, documentar los cambios en el PR y subirlo a github.
- Actualizar `docs/ai/MEMORY.md`.

## Cuando actualizar este archivo

- Cuando se agreguen dependencias externas, librerías o frameworks.
- Cuando este archivo no refleje el funcionamiento de la aplicación.
