using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Acceso.Data;
using SistemaDDJ.Web.Modules.Acceso.Features;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;

namespace SistemaDDJ.Web.Modules.Acceso;

public static class AccesoModule
{
    public static IServiceCollection AddAccesoModule(this IServiceCollection services, string connectionString) =>
        services
            .AddModuleDbContext<AccesoDbContext>(connectionString, AccesoDbContext.Schema)
            .AddScoped<IAccesoPublicApi, AccesoPublicApi>();
}
