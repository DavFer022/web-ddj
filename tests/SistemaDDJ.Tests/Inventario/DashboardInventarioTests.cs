using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Tests.Infrastructure;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarDashboardInventario;

namespace SistemaDDJ.Tests.Inventario;

[Collection(ColeccionInventario.Nombre)]
public sealed class DashboardInventarioTests(BaseDatosPruebaInventario bd) : IAsyncLifetime
{
    private InventarioDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _db = bd.CrearContexto();
        await bd.LimpiarAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task ConsultarDashboard_CalculaStockConEntradasSalidasYAjustes()
    {
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Película de prueba", stockMinimo: 2);
        var usuarioId = await bd.CrearUsuarioAsync();
        await CrearMovimientoAsync(usuarioId, TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, productoId, 10,
            notas: "Carga inicial de existencias");
        await CrearMovimientoAsync(usuarioId, TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.VENTA, productoId, 3);
        await CrearMovimientoAsync(usuarioId, TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.AJUSTE, productoId, 1,
            notas: "Conteo físico con faltante");
        await CrearMovimientoAsync(usuarioId, TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, productoId, 2,
            notas: "Sobrante detectado en conteo");

        var resultado = await new ConsultarDashboardInventario.Handler(_db)
            .HandleAsync(new ConsultarDashboardInventario.Consulta());

        var articulo = Assert.Single(resultado.Articulos);
        Assert.Equal(8m, articulo.Stock);
        Assert.Equal(EstadoStock.DISPONIBLE, articulo.Estado);
        Assert.Equal(1, resultado.Productos.Total);
        Assert.Equal(0, resultado.Productos.Agotados);
    }

    [Fact]
    public async Task ConsultarDashboard_ArticuloSinMovimientos_TieneStockCeroYEstadoAgotado()
    {
        await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Producto sin movimientos");

        var resultado = await new ConsultarDashboardInventario.Handler(_db)
            .HandleAsync(new ConsultarDashboardInventario.Consulta());

        var articulo = Assert.Single(resultado.Articulos);
        Assert.Equal(0m, articulo.Stock);
        Assert.Equal(EstadoStock.AGOTADO, articulo.Estado);
        Assert.Equal(1, resultado.Productos.Agotados);
    }

    [Fact]
    public async Task ConsultarDashboard_SeparaProductosEInsumos()
    {
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearArticuloAsync(ArticuloTipo.INSUMO, "Papel DTF");
        var usuarioId = await bd.CrearUsuarioAsync();
        await CrearMovimientoAsync(usuarioId, TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, productoId, 5,
            notas: "Carga inicial de existencias");

        var resultadoProductos = await new ConsultarDashboardInventario.Handler(_db)
            .HandleAsync(new ConsultarDashboardInventario.Consulta(ArticuloTipo.PRODUCTO));
        var resultadoInsumos = await new ConsultarDashboardInventario.Handler(_db)
            .HandleAsync(new ConsultarDashboardInventario.Consulta(ArticuloTipo.INSUMO));

        var producto = Assert.Single(resultadoProductos.Articulos);
        Assert.Equal(ArticuloTipo.PRODUCTO, producto.Tipo);
        Assert.StartsWith("SKU-", producto.Sku);
        Assert.Equal(12m, producto.PrecioVenta);

        var insumo = Assert.Single(resultadoInsumos.Articulos);
        Assert.Equal(ArticuloTipo.INSUMO, insumo.Tipo);
        Assert.Equal("Descripción del insumo", insumo.DescripcionInsumo);
        Assert.Equal(1, resultadoProductos.Insumos.Total);
        Assert.Equal(1, resultadoProductos.Productos.Total);
    }

    [Fact]
    public async Task ConsultarDashboard_MarcaArticuloConBajoStockSegunUmbral()
    {
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Producto con bajo stock", stockMinimo: 5);
        var usuarioId = await bd.CrearUsuarioAsync();
        await CrearMovimientoAsync(usuarioId, TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, productoId, 3,
            notas: "Carga inicial de existencias");

        var resultado = await new ConsultarDashboardInventario.Handler(_db)
            .HandleAsync(new ConsultarDashboardInventario.Consulta(SoloAlertas: true));

        var articulo = Assert.Single(resultado.Articulos);
        Assert.Equal(EstadoStock.BAJO, articulo.Estado);
        Assert.Equal(3m, articulo.Stock);
        Assert.Equal(5m, articulo.StockMinimo);
        Assert.Equal(1, resultado.Productos.Bajos);
    }

    private async Task<int> CrearArticuloAsync(ArticuloTipo tipo, string nombre, decimal stockMinimo = 0)
    {
        var categoria = new CategoriaArticulo { Nombre = $"Categoría {Guid.NewGuid():N}" };
        _db.CategoriasArticulo.Add(categoria);
        await _db.SaveChangesAsync();

        var articulo = new Articulo
        {
            Codigo = $"ART-{Guid.NewGuid():N}"[..12],
            Nombre = nombre,
            Unidad = UnidadMedida.UNIDAD,
            Tipo = tipo,
            CostoUnitario = 4m,
            StockMinimo = stockMinimo,
            CategoriaId = categoria.Id
        };
        _db.Articulos.Add(articulo);
        await _db.SaveChangesAsync();

        if (tipo == ArticuloTipo.PRODUCTO)
        {
            _db.Productos.Add(new Producto
            {
                ArticuloId = articulo.Id,
                Sku = $"SKU-{Guid.NewGuid():N}"[..12],
                PrecioVenta = 12m
            });
        }
        else
        {
            _db.Insumos.Add(new Insumo { ArticuloId = articulo.Id, Descripcion = "Descripción del insumo" });
        }
        await _db.SaveChangesAsync();
        return articulo.Id;
    }

    private async Task CrearMovimientoAsync(
        int usuarioId,
        TipoMovimientoInventario tipo,
        MotivoMovimientoInventario motivo,
        int articuloId,
        decimal cantidad,
        string? notas = null)
    {
        var movimiento = new MovimientoInventario
        {
            TipoMovimiento = tipo,
            Motivo = motivo,
            OcurridoEn = DateTimeOffset.UtcNow,
            Notas = notas,
            RegistradoPorUsuarioId = usuarioId
        };
        _db.MovimientosInventario.Add(movimiento);
        _db.DetallesMovimientoInventario.Add(new DetalleMovimientoInventario
        {
            MovimientoId = movimiento.Id,
            ArticuloId = articuloId,
            Cantidad = cantidad
        });
        await _db.SaveChangesAsync();
    }
}
