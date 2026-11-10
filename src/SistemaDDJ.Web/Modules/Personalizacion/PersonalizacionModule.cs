using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Personalizacion.Data;
using SistemaDDJ.Web.Modules.Personalizacion.Features;
using SistemaDDJ.Web.Modules.Personalizacion.PublicApi;

namespace SistemaDDJ.Web.Modules.Personalizacion;

public static class PersonalizacionModule
{
    public static IServiceCollection AddPersonalizacionModule(this IServiceCollection services, string connectionString) =>
        services
            .AddModuleDbContext<PersonalizacionDbContext>(connectionString, PersonalizacionDbContext.Schema)
            .AddScoped<IPersonalizacionPublicApi, PersonalizacionPublicApi>();
}
