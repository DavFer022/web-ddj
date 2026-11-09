using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Inventario.Data.Configurations;
using SistemaDDJ.Web.Modules.Inventario.Entities;

namespace SistemaDDJ.Web.Modules.Inventario.Data;

public sealed class InventarioDbContext(DbContextOptions<InventarioDbContext> options) : DbContext(options)
{
    public const string Schema = "inventario";

    public DbSet<CategoriaArticulo> CategoriasArticulo => Set<CategoriaArticulo>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<DetalleMovimientoInventario> DetallesMovimientoInventario => Set<DetalleMovimientoInventario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CategoriaArticuloConfiguration());
        modelBuilder.ApplyConfiguration(new ArticuloConfiguration());
        modelBuilder.ApplyConfiguration(new ProductoConfiguration());
        modelBuilder.ApplyConfiguration(new InsumoConfiguration());
        modelBuilder.ApplyConfiguration(new ProveedorConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoInventarioConfiguration());
        modelBuilder.ApplyConfiguration(new DetalleMovimientoInventarioConfiguration());
        modelBuilder.ApplyModuleConventions(Schema);
    }
}
