using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Features.ActualizarProveedor;
using SistemaDDJ.Web.Modules.Inventario.Features.CambiarEstadoProveedor;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarDashboardInventario;
using SistemaDDJ.Web.Modules.Inventario.Features.ListarProveedores;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarArticulo;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarCategoria;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarCompraProveedor;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarProveedor;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarVentaProductos;

namespace SistemaDDJ.Web.Modules.Inventario;

public static class InventarioModule
{
    public static IServiceCollection AddInventarioModule(this IServiceCollection services, string connectionString) =>
        services
            .AddModuleDbContext<InventarioDbContext>(connectionString, InventarioDbContext.Schema)
            .AddScoped<RegistrarArticulo.Handler>()
            .AddScoped<RegistrarCategoria.Handler>()
            .AddScoped<RegistrarProveedor.Handler>()
            .AddScoped<ActualizarProveedor.Handler>()
            .AddScoped<CambiarEstadoProveedor.Handler>()
            .AddScoped<ListarProveedores.Handler>()
            .AddScoped<ConsultarDashboardInventario.Handler>()
            .AddScoped<RegistrarCompraProveedor.Handler>()
            .AddScoped<RegistrarVentaProductos.Handler>();
}
