using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Tests.Infrastructure;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarArticulo;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarCategoria;
using SistemaDDJ.Web.Modules.Inventario.Data;

namespace SistemaDDJ.Tests.Inventario;

[Collection(ColeccionInventario.Nombre)]
public sealed class CatalogoArticulosTests(BaseDatosPruebaInventario bd) : IAsyncLifetime
{
    private InventarioDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _db = bd.CrearContexto();
        await LimpiarAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task RegistrarProducto_CreaArticuloYRegistroDeProducto()
    {
        var categoriaId = await CrearCategoriaAsync("Ropa");

        var resultado = await new RegistrarArticulo.Handler(_db).HandleAsync(ComandoProducto(categoriaId));

        Assert.True(resultado.Exitoso);
        await using var verificacion = bd.CrearContexto();
        var articulo = await verificacion.Articulos.SingleAsync();
        Assert.Equal(resultado.ArticuloId, articulo.Id);
        Assert.Equal(ArticuloTipo.PRODUCTO, articulo.Tipo);
        Assert.Equal("PRD-001", articulo.Codigo);
        Assert.Equal(UnidadMedida.UNIDAD, articulo.Unidad);
        var producto = await verificacion.Productos.SingleAsync();
        Assert.Equal(articulo.Id, producto.ArticuloId);
        Assert.Equal(12.00m, producto.PrecioVenta);
        Assert.Equal("FRN-BLA-M", producto.Sku);
        Assert.Empty(await verificacion.Insumos.ToListAsync());
    }

    [Fact]
    public async Task RegistrarInsumo_CreaArticuloYRegistroDeInsumo_SinRegistroDeProducto()
    {
        var categoriaId = await CrearCategoriaAsync("Insumos DTF");

        var resultado = await new RegistrarArticulo.Handler(_db).HandleAsync(ComandoInsumo(categoriaId));

        Assert.True(resultado.Exitoso);
        await using var verificacion = bd.CrearContexto();
        var articulo = await verificacion.Articulos.SingleAsync();
        Assert.Equal(ArticuloTipo.INSUMO, articulo.Tipo);
        var insumo = await verificacion.Insumos.SingleAsync();
        Assert.Equal("Película para transferencia DTF", insumo.Descripcion);
        Assert.Empty(await verificacion.Productos.ToListAsync());
    }

    [Fact]
    public async Task RegistrarProducto_RechazaPrecioDeVentaNoPositivo()
    {
        var categoriaId = await CrearCategoriaAsync("Ropa");
        var comando = ComandoProducto(categoriaId) with { PrecioVenta = 0m };

        var resultado = await new RegistrarArticulo.Handler(_db).HandleAsync(comando);

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("precio de venta"));
        Assert.Equal(0, await _db.Articulos.CountAsync());
    }

    [Fact]
    public async Task RegistrarProducto_RechazaSkuDuplicado()
    {
        var categoriaId = await CrearCategoriaAsync("Ropa");
        var handler = new RegistrarArticulo.Handler(_db);
        await handler.HandleAsync(ComandoProducto(categoriaId));

        var resultado = await handler.HandleAsync(ComandoProducto(categoriaId) with { Codigo = "PRD-002" });

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("SKU"));
        Assert.Equal(1, await _db.Articulos.CountAsync());
    }

    [Fact]
    public async Task RegistrarArticulo_RechazaCodigoDuplicado_SinDistinguirMayusculas()
    {
        var categoriaId = await CrearCategoriaAsync("Insumos DTF");
        var handler = new RegistrarArticulo.Handler(_db);
        await handler.HandleAsync(ComandoInsumo(categoriaId));

        var resultado = await handler.HandleAsync(ComandoInsumo(categoriaId) with { Codigo = "ins-004" });

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("código"));
        Assert.Equal(1, await _db.Articulos.CountAsync());
    }

    [Fact]
    public async Task RegistrarArticulo_RechazaCategoriaInexistente()
    {
        var resultado = await new RegistrarArticulo.Handler(_db).HandleAsync(ComandoInsumo(categoriaId: 9999));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("categoría"));
        Assert.Equal(0, await _db.Articulos.CountAsync());
    }

    [Fact]
    public async Task RegistrarArticulo_RechazaCostoNegativo()
    {
        var categoriaId = await CrearCategoriaAsync("Insumos DTF");

        var resultado = await new RegistrarArticulo.Handler(_db).HandleAsync(
            ComandoInsumo(categoriaId) with { CostoUnitario = -1m });

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("costo"));
    }

    [Fact]
    public async Task RegistrarInsumo_RechazaDescripcionVacia()
    {
        var categoriaId = await CrearCategoriaAsync("Insumos DTF");

        var resultado = await new RegistrarArticulo.Handler(_db).HandleAsync(
            ComandoInsumo(categoriaId) with { Descripcion = "  " });

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("descripción"));
        Assert.Equal(0, await _db.Articulos.CountAsync());
    }

    [Fact]
    public async Task RegistrarCategoria_CreaCategoriaActiva()
    {
        var resultado = await new RegistrarCategoria.Handler(_db).HandleAsync(
            new RegistrarCategoria.Command("Tazas", "Artículos sublimables"));

        Assert.True(resultado.Exitoso);
        var categoria = await _db.CategoriasArticulo.SingleAsync();
        Assert.Equal("Tazas", categoria.Nombre);
        Assert.True(categoria.Activo);
    }

    [Fact]
    public async Task RegistrarCategoria_RechazaNombreDuplicado()
    {
        var handler = new RegistrarCategoria.Handler(_db);
        await handler.HandleAsync(new RegistrarCategoria.Command("Ropa", null));

        var resultado = await handler.HandleAsync(new RegistrarCategoria.Command(" ropa ", null));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("nombre"));
        Assert.Equal(1, await _db.CategoriasArticulo.CountAsync());
    }

    private async Task<int> CrearCategoriaAsync(string nombre)
    {
        var categoria = new CategoriaArticulo { Nombre = nombre };
        _db.CategoriasArticulo.Add(categoria);
        await _db.SaveChangesAsync();
        return categoria.Id;
    }

    private static RegistrarArticulo.Command ComandoProducto(int categoriaId) => new(
        Codigo: "PRD-001",
        Nombre: "Franela blanca M",
        Unidad: UnidadMedida.UNIDAD,
        CostoUnitario: 4.50m,
        CategoriaId: categoriaId,
        Tipo: ArticuloTipo.PRODUCTO,
        PrecioVenta: 12.00m,
        Sku: "FRN-BLA-M");

    private static RegistrarArticulo.Command ComandoInsumo(int categoriaId) => new(
        Codigo: "INS-004",
        Nombre: "Película DTF A3",
        Unidad: UnidadMedida.HOJA,
        CostoUnitario: 1.20m,
        CategoriaId: categoriaId,
        Tipo: ArticuloTipo.INSUMO,
        Descripcion: "Película para transferencia DTF");

    private async Task LimpiarAsync() =>
        await _db.Database.ExecuteSqlRawAsync(
            "TRUNCATE inventario.detalle_movimiento_inventario, inventario.movimiento_inventario, inventario.insumo, " +
            "inventario.producto, inventario.articulo, inventario.categoria_articulo, inventario.proveedor RESTART IDENTITY CASCADE");
}

[CollectionDefinition(Nombre, DisableParallelization = true)]
public sealed class ColeccionInventario : ICollectionFixture<BaseDatosPruebaInventario>
{
    public const string Nombre = "Base de datos de inventario";
}
