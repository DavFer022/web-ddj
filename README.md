# Sistema DD&J

Aplicación web para la gestión de una tienda de ropa y personalización, construida con Blazor, ASP.NET Core, EF Core y PostgreSQL.

## Requisitos

- .NET SDK 10
- PostgreSQL disponible localmente

## Configuración local

La aplicación lee la cadena de conexión `ConnectionStrings:DefaultConnection` desde .NET User Secrets. No guardes credenciales en `appsettings.json` ni en Git.

Para configurar una conexión en tu equipo:

```sh
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=5433;Database=web_ddj;Username=postgres;Password=<tu-contraseña>" --project src/SistemaDDJ.Web
```

Ejecuta la aplicación:

```sh
dotnet run --project src/SistemaDDJ.Web
```

## Pruebas

Ejecuta la suite con:

```sh
dotnet test SistemaDDJ.sln
```

Las pruebas están en `tests/SistemaDDJ.Tests`. Las pruebas de integración que requieren PostgreSQL usan la variable de entorno `DDJ_TEST_CONNECTION` con una base que termine en `_tests` (por ejemplo `web_ddj_tests`). La base se crea si no existe; nunca se usa la base de desarrollo. Ejemplo en PowerShell:

```sh
export DDJ_TEST_CONNECTION="Host=127.0.0.1;Port=5433;Database=web_ddj_tests;Username=postgres;Password=<tu-contraseña>"
dotnet test SistemaDDJ.sln
```

## Base de datos

El proyecto usa EF Core con Npgsql. Cada módulo tiene su propio `DbContext` y su esquema PostgreSQL (`acceso`, `inventario`, `personalizacion`, `clientes`, `notificaciones`). Las claves foráneas entre esquemas se crean en un contexto de integración que se aplica al final.

Aplicar migraciones desde cero, en este orden:

```sh
dotnet tool restore
dotnet ef database update --context AccesoDbContext --project src/SistemaDDJ.Web
dotnet ef database update --context InventarioDbContext --project src/SistemaDDJ.Web
dotnet ef database update --context PersonalizacionDbContext --project src/SistemaDDJ.Web
dotnet ef database update --context ClientesDbContext --project src/SistemaDDJ.Web
dotnet ef database update --context NotificacionesDbContext --project src/SistemaDDJ.Web
dotnet ef database update --context IntegracionDbContext --project src/SistemaDDJ.Web
```

Al ejecutar la aplicación en Development se cargan datos de prueba (solo si la base no tiene usuarios y no hay migraciones pendientes en ningún contexto).

Nueva migración de un módulo:

```sh
dotnet ef migrations add <Nombre> --context InventarioDbContext --project src/SistemaDDJ.Web --output-dir Modules/Inventario/Data/Migrations
```

Nueva migración de integración (claves entre esquemas):

```sh
dotnet ef migrations add <Nombre> --context IntegracionDbContext --project src/SistemaDDJ.Web --output-dir Infrastructure/Persistence/Integracion/Migrations
```

## Estructura

- `src/SistemaDDJ.Web/Modules/<Modulo>/`: módulos de negocio (`PublicApi`, `Entities`, `Data`, `Features`, `Pages`).
- `src/SistemaDDJ.Web/Infrastructure/Persistence/`: helpers de persistencia, datos de prueba e integración entre esquemas.
- `docs/ai/`: contexto y convenciones del proyecto para asistentes de IA. Ver `docs/ai/ARCHITECTURE.md`.
