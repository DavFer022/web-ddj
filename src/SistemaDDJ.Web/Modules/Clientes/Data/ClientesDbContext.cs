using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Clientes.Data.Configurations;
using SistemaDDJ.Web.Modules.Clientes.Entities;

namespace SistemaDDJ.Web.Modules.Clientes.Data;

public sealed class ClientesDbContext(DbContextOptions<ClientesDbContext> options) : DbContext(options)
{
    public const string Schema = "clientes";

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<MovimientoCuentaCliente> MovimientosCuentaCliente => Set<MovimientoCuentaCliente>();
    public DbSet<BeneficioFidelidad> BeneficiosFidelidad => Set<BeneficioFidelidad>();
    public DbSet<MovimientoFidelidadCliente> MovimientosFidelidadCliente => Set<MovimientoFidelidadCliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoCuentaClienteConfiguration());
        modelBuilder.ApplyConfiguration(new BeneficioFidelidadConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoFidelidadClienteConfiguration());
        modelBuilder.ApplyModuleConventions(Schema);
    }
}
