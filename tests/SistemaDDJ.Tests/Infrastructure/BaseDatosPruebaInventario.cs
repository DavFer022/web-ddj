using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaDDJ.Web.Modules.Inventario.Data;

namespace SistemaDDJ.Tests.Infrastructure;

// Base PostgreSQL aislada para pruebas de inventario. Se configura con la variable
// de entorno DDJ_TEST_CONNECTION y su base debe terminar en "_tests", nunca usar la de desarrollo.
public sealed class BaseDatosPruebaInventario : IAsyncLifetime
{
    private const string Variable = "DDJ_TEST_CONNECTION";
    private const string NombrePermitidoSufijo = "_tests";

    private readonly string _cadenaConexion = ObtenerCadenaConexion();

    public async Task InitializeAsync()
    {
        var nombre = new NpgsqlConnectionStringBuilder(_cadenaConexion).Database ?? string.Empty;
        if (!nombre.EndsWith(NombrePermitidoSufijo, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"La base de pruebas debe terminar en '{NombrePermitidoSufijo}' para no usar la base de desarrollo.");
        }

        await CrearBaseSiNoExisteAsync(nombre);

        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public InventarioDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<InventarioDbContext>()
            .UseNpgsql(_cadenaConexion)
            .Options;
        return new InventarioDbContext(opciones);
    }

    private static string ObtenerCadenaConexion() =>
        Environment.GetEnvironmentVariable(Variable)
        ?? throw new InvalidOperationException(
            $"Define la variable de entorno {Variable} con una cadena de conexión a una base '*_tests'.");

    private async Task CrearBaseSiNoExisteAsync(string nombre)
    {
        var administracion = new NpgsqlConnectionStringBuilder(_cadenaConexion) { Database = "postgres" };
        await using var conexion = new NpgsqlConnection(administracion.ConnectionString);
        await conexion.OpenAsync();

        await using (var consulta = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @nombre", conexion))
        {
            consulta.Parameters.AddWithValue("nombre", nombre);
            if (await consulta.ExecuteScalarAsync() is not null) return;
        }

        await using var crear = new NpgsqlCommand($"CREATE DATABASE \"{nombre}\"", conexion);
        await crear.ExecuteNonQueryAsync();
    }
}
