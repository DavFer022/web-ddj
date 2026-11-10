using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaDDJ.Web.Modules.Acceso.Data;
using SistemaDDJ.Web.Modules.Acceso.Entities;
using SistemaDDJ.Web.Modules.Clientes.Data;
using SistemaDDJ.Web.Modules.Clientes.Entities;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Notificaciones.Data;
using SistemaDDJ.Web.Modules.Personalizacion.Data;
using SistemaDDJ.Web.Modules.Personalizacion.Entities;
using SistemaDDJ.Web.Infrastructure.Persistence.Integracion;

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
        await MigrarContextosAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public InventarioDbContext CrearContexto() => CrearContexto<InventarioDbContext>();

    public AccesoDbContext CrearContextoAcceso() => CrearContexto<AccesoDbContext>();

    public ClientesDbContext CrearContextoClientes() => CrearContexto<ClientesDbContext>();

    public PersonalizacionDbContext CrearContextoPersonalizacion() => CrearContexto<PersonalizacionDbContext>();

    public NotificacionesDbContext CrearContextoNotificaciones() => CrearContexto<NotificacionesDbContext>();

    public IntegracionDbContext CrearContextoIntegracion() => CrearContexto<IntegracionDbContext>();

    // Limpia todas las tablas de negocio, conservando los roles semilla de acceso.
    public async Task LimpiarAsync()
    {
        await using var db = CrearContexto();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE " +
            "notificaciones.notificacion, " +
            "clientes.movimiento_fidelidad_cliente, clientes.movimiento_cuenta_cliente, " +
            "clientes.beneficio_fidelidad, clientes.cliente, " +
            "personalizacion.linea_orden_tecnica, personalizacion.detalle_orden_servicio, " +
            "personalizacion.orden_servicio, personalizacion.tecnica_personalizacion, " +
            "inventario.detalle_movimiento_inventario, inventario.movimiento_inventario, " +
            "inventario.telefono_proveedor, inventario.proveedor, inventario.insumo, inventario.producto, " +
            "inventario.articulo, inventario.categoria_articulo, " +
            "acceso.usuario_rol, acceso.usuario " +
            "RESTART IDENTITY CASCADE");
    }

    public async Task<int> CrearUsuarioAsync(string? rol = "Admin", bool activo = true, string? correo = null)
    {
        await using var db = CrearContextoAcceso();
        var usuario = new Usuario
        {
            Correo = correo ?? $"usuario-{Guid.NewGuid():N}@pruebas.local",
            NombreVisible = "Usuario de pruebas",
            Activo = activo,
            CreadoEn = DateTimeOffset.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(rol))
        {
            var rolId = await db.Roles.Where(r => r.Nombre == rol).Select(r => r.Id).SingleAsync();
            db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuario.Id, RolId = rolId });
            await db.SaveChangesAsync();
        }

        return usuario.Id;
    }

    public async Task<Guid> CrearClienteAsync(string nombre = "Cliente de pruebas")
    {
        await using var db = CrearContextoClientes();
        var cliente = new Cliente { Nombre = nombre, CreadoEn = DateTimeOffset.UtcNow };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        return cliente.Id;
    }

    public async Task<Guid> CrearOrdenServicioAsync(
        Guid clienteId,
        int creadoPorUsuarioId,
        EstadoOrdenServicio estado = EstadoOrdenServicio.PENDIENTE,
        decimal totalCotizado = 100m)
    {
        await using var db = CrearContextoPersonalizacion();
        var ahora = DateTimeOffset.UtcNow;
        var orden = new OrdenServicio
        {
            Estado = estado,
            SolicitadoEn = ahora,
            VenceEn = ahora.AddDays(7),
            TotalCotizado = totalCotizado,
            ClienteId = clienteId,
            CreadoPorUsuarioId = creadoPorUsuarioId
        };
        db.OrdenesServicio.Add(orden);
        await db.SaveChangesAsync();
        return orden.Id;
    }

    private TContext CrearContexto<TContext>() where TContext : DbContext
    {
        var opciones = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(_cadenaConexion)
            .Options;
        return (TContext)Activator.CreateInstance(typeof(TContext), opciones)!;
    }

    private async Task MigrarContextosAsync()
    {
        await using (var db = CrearContextoAcceso()) await db.Database.MigrateAsync();
        await using (var db = CrearContexto()) await db.Database.MigrateAsync();
        await using (var db = CrearContextoPersonalizacion()) await db.Database.MigrateAsync();
        await using (var db = CrearContextoClientes()) await db.Database.MigrateAsync();
        await using (var db = CrearContextoNotificaciones()) await db.Database.MigrateAsync();
        await using (var db = CrearContextoIntegracion()) await db.Database.MigrateAsync();
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
