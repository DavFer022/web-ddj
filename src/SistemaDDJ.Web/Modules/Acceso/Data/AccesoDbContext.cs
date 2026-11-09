using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Acceso.Data.Configurations;
using SistemaDDJ.Web.Modules.Acceso.Entities;

namespace SistemaDDJ.Web.Modules.Acceso.Data;

public sealed class AccesoDbContext(DbContextOptions<AccesoDbContext> options) : DbContext(options)
{
    public const string Schema = "acceso";

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new RolConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioRolConfiguration());
        modelBuilder.ApplyModuleConventions(Schema);
    }
}
