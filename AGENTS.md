# Instrucciones para agentes

## Inicio de cada sesión

- En cada nuevo chat/sesión, lee `docs/ai/ARCHITECTURE.md` antes de analizar o modificar el código.
- Consulta `README.md` para configuración y comandos.
- Consultar `docs/ai/MEMORY.md` para conocer el estado actual del proyecto y decisiones previas.

## Arquitectura y límites

- La solución es un monolito modular Blazor/ASP.NET Core en .NET 10; organiza nuevas funcionalidades en `src/SistemaDDJ.Web/Features/<Feature>` y persistencia compartida en `Infrastructure/Persistence`.
- Usa EF Core Code First con PostgreSQL/Npgsql; no agregues proyectos/capas separados sin una necesidad concreta.
- Las páginas actuales de `Features/` son placeholders; no asumas que autenticación o lógica de negocio ya están implementadas.
- No guardes contraseñas ni cadenas de conexión reales en archivos versionados; usa .NET User Secrets localmente.

## Stack y arquitectura

- Blazor y ASP.NET Core sobre .NET 10
- PostgreSQL con EF Core Code First
- Monolito modular; organizar el trabajo futuro por vertical slices dentro de cada feature

## Comandos

- Compilar: `dotnet build SistemaDDJ.sln`.
- Ejecutar: `dotnet run --project src/SistemaDDJ.Web`.
- Ejecutar pruebas: `dotnet test SistemaDDJ.sln`.
- Restaurar la herramienta EF local: `dotnet tool restore`.
- Crear/aplicar migraciones: `dotnet ef migrations add <Nombre> --project src/SistemaDDJ.Web` y `dotnet ef database update --project src/SistemaDDJ.Web`.

## Flujo de trabajo para implementación

- Leer `docs/ai/HU/Backlog.md` para identificar las HU a implementar. Siempre trabajar enfocado a un solo modulo.
- Pasar las HU seleccionas al archivo `docs/ai/HU/HU-IMPLEMENT.md` para formalizar la implementacion e implementar en la siguiente iteración.
- Antes de implementar cada HU, crear y ejecutar primero las pruebas de sus reglas y recorridos críticos; seguir Red-Green-Refactor. Priorizar comportamiento de negocio, no una prueba por cada función.
- Ubicar las pruebas en `tests/SistemaDDJ.Tests`. Las pruebas de integración con PostgreSQL deben usar una base aislada, nunca la base de desarrollo.
- Crear una branch de Git para cada HU en progreso con el nombre `feature/<nombre-hu>`.
- Para cada Epica completada y validada, documentar los cambios en el PR y subirlo a github.
- Actualizar `docs/ai/MEMORY.md`.
