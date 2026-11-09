using Microsoft.EntityFrameworkCore;

namespace SistemaDDJ.Web.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string connectionString, string schema)
        where TContext : DbContext =>
        services.AddDbContext<TContext>(options => options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
}
