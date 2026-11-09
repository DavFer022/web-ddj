using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Personalizacion.Data.Configurations;
using SistemaDDJ.Web.Modules.Personalizacion.Entities;

namespace SistemaDDJ.Web.Modules.Personalizacion.Data;

public sealed class PersonalizacionDbContext(DbContextOptions<PersonalizacionDbContext> options) : DbContext(options)
{
    public const string Schema = "personalizacion";
    public const string NumeroOrdenSequence = "orden_servicio_numero_seq";

    public DbSet<OrdenServicio> OrdenesServicio => Set<OrdenServicio>();
    public DbSet<DetalleOrdenServicio> DetallesOrdenServicio => Set<DetalleOrdenServicio>();
    public DbSet<TecnicaPersonalizacion> TecnicasPersonalizacion => Set<TecnicaPersonalizacion>();
    public DbSet<LineaOrdenTecnica> LineasOrdenTecnica => Set<LineaOrdenTecnica>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(NumeroOrdenSequence, Schema).StartsAt(1);
        modelBuilder.ApplyConfiguration(new OrdenServicioConfiguration());
        modelBuilder.ApplyConfiguration(new DetalleOrdenServicioConfiguration());
        modelBuilder.ApplyConfiguration(new TecnicaPersonalizacionConfiguration());
        modelBuilder.ApplyConfiguration(new LineaOrdenTecnicaConfiguration());
        modelBuilder.ApplyModuleConventions(Schema);
    }
}
