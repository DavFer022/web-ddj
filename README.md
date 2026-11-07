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

Las pruebas están en `tests/SistemaDDJ.Tests`. Las pruebas de integración que requieran PostgreSQL deben usar una base de datos de prueba aislada.

## Base de datos

El proyecto usa EF Core con el proveedor Npgsql y `AppDbContext`. Cuando se agreguen las primeras entidades, crear una migración con:

```sh
dotnet tool restore
dotnet ef migrations add InitialCreate --project src/SistemaDDJ.Web
dotnet ef database update --project src/SistemaDDJ.Web
```

## Estructura

- `src/SistemaDDJ.Web/Features/`: módulos y funcionalidades organizados por feature.
- `src/SistemaDDJ.Web/Infrastructure/Persistence/`: configuración de persistencia.
- `docs/ai/`: contexto y convenciones del proyecto para asistentes de IA.
