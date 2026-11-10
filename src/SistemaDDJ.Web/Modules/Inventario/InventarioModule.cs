using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarArticulo;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarCategoria;

namespace SistemaDDJ.Web.Modules.Inventario;

public static class InventarioModule
{
    public static IServiceCollection AddInventarioModule(this IServiceCollection services, string connectionString) =>
        services
            .AddModuleDbContext<InventarioDbContext>(connectionString, InventarioDbContext.Schema)
            .AddScoped<RegistrarArticulo.Handler>()
            .AddScoped<RegistrarCategoria.Handler>();
}
