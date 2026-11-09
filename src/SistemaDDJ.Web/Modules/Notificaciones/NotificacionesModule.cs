using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Notificaciones.Data;

namespace SistemaDDJ.Web.Modules.Notificaciones;

public static class NotificacionesModule
{
    public static IServiceCollection AddNotificacionesModule(this IServiceCollection services, string connectionString) =>
        services.AddModuleDbContext<NotificacionesDbContext>(connectionString, NotificacionesDbContext.Schema);
}
