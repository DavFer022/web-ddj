using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Clientes.Data;

namespace SistemaDDJ.Web.Modules.Clientes;

public static class ClientesModule
{
    public static IServiceCollection AddClientesModule(this IServiceCollection services, string connectionString) =>
        services.AddModuleDbContext<ClientesDbContext>(connectionString, ClientesDbContext.Schema);
}
