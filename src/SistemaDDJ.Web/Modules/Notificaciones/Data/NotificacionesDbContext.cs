using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Notificaciones.Data.Configurations;
using SistemaDDJ.Web.Modules.Notificaciones.Entities;

namespace SistemaDDJ.Web.Modules.Notificaciones.Data;

public sealed class NotificacionesDbContext(DbContextOptions<NotificacionesDbContext> options) : DbContext(options)
{
    public const string Schema = "notificaciones";

    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NotificacionConfiguration());
        modelBuilder.ApplyModuleConventions(Schema);
    }
}
