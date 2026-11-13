using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Tests.Infrastructure;
using SistemaDDJ.Web.Modules.Acceso.Data;
using SistemaDDJ.Web.Modules.Acceso.Features;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarConsumoInsumos;
using SistemaDDJ.Web.Modules.Personalizacion.Data;
using SistemaDDJ.Web.Modules.Personalizacion.Entities;
using SistemaDDJ.Web.Modules.Personalizacion.Features;

namespace SistemaDDJ.Tests.Inventario;

[Collection(ColeccionInventario.Nombre)]
public sealed class ConsumoInsumosTests(BaseDatosPruebaInventario bd) : IAsyncLifetime
{
    private InventarioDbContext _db = null!;
    private AccesoDbContext _acceso = null!;
    private PersonalizacionDbContext _personalizacion = null!;

    public async Task InitializeAsync()
    {
        _db = bd.CrearContexto();
        _acceso = bd.CrearContextoAcceso();
        _personalizacion = bd.CrearContextoPersonalizacion();
        await bd.LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _acceso.DisposeAsync();
        await _personalizacion.DisposeAsync();
    }

    [Fact]
    public async Task RegistrarConsumo_DeInsumosConStock_DescuentaExistenciasYAsociaOrden()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var clienteId = await bd.CrearClienteAsync();
        var ordenId = await bd.CrearOrdenServicioAsync(clienteId, usuarioId);
        var insumoId = await CrearArticuloAsync(ArticuloTipo.INSUMO, "Papel DTF");
        await CrearEntradaStockAsync(usuarioId, insumoId, 10);

        var resultado = await CrearHandler().HandleAsync(new RegistrarConsumoInsumos.Command(
            ordenId,
            usuarioId,
            "CONSUMO-01",
            "Consumo real del pedido",
            [new RegistrarConsumoInsumos.Linea(insumoId, 2.5m)]));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        var movimiento = await verificacion.MovimientosInventario
            .Include(m => m.Detalles)
            .SingleAsync(m => m.Id == resultado.MovimientoId);
        Assert.Equal(TipoMovimientoInventario.SALIDA, movimiento.TipoMovimiento);
        Assert.Equal(MotivoMovimientoInventario.CONSUMO, movimiento.Motivo);
        Assert.Equal(ordenId, movimiento.OrdenServicioId);
        Assert.Null(movimiento.ProveedorId);
        Assert.Null(movimiento.ClienteId);
        var detalle = Assert.Single(movimiento.Detalles);
        Assert.Equal(2.5m, detalle.Cantidad);
        Assert.Equal(4.50m, detalle.CostoUnitario);
        Assert.Null(detalle.PrecioUnitario);
        Assert.Equal(7.5m, (await SaldosInventario.ObtenerSaldosAsync(verificacion))[insumoId]);
    }

    [Fact]
    public async Task RegistrarConsumo_SinOrdenDeServicioOConOrdenInexistente_EsRechazado()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var insumoId = await CrearArticuloAsync(ArticuloTipo.INSUMO, "Papel DTF");
        await CrearEntradaStockAsync(usuarioId, insumoId, 10);

        var sinOrden = await CrearHandler().HandleAsync(new RegistrarConsumoInsumos.Command(
            Guid.Empty, usuarioId, null, null, [new RegistrarConsumoInsumos.Linea(insumoId, 1)]));
        var inexistente = await CrearHandler().HandleAsync(new RegistrarConsumoInsumos.Command(
            Guid.CreateVersion7(), usuarioId, null, null, [new RegistrarConsumoInsumos.Linea(insumoId, 1)]));

        Assert.False(sinOrden.Exitoso);
        Assert.Contains(sinOrden.Errores, e => e.Contains("orden", StringComparison.OrdinalIgnoreCase));
        Assert.False(inexistente.Exitoso);
        Assert.Contains(inexistente.Errores, e => e.Contains("orden", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegistrarConsumo_RechazaArticuloDeSubtipoProducto()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var clienteId = await bd.CrearClienteAsync();
        var ordenId = await bd.CrearOrdenServicioAsync(clienteId, usuarioId);
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearEntradaStockAsync(usuarioId, productoId, 10);

        var resultado = await CrearHandler().HandleAsync(new RegistrarConsumoInsumos.Command(
            ordenId, usuarioId, null, null, [new RegistrarConsumoInsumos.Linea(productoId, 1)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("insumo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await _db.MovimientosInventario.CountAsync(m => m.Motivo == MotivoMovimientoInventario.CONSUMO));
    }

    [Fact]
    public async Task RegistrarConsumo_RechazaCantidadMayorAlStockDelInsumo()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var clienteId = await bd.CrearClienteAsync();
        var ordenId = await bd.CrearOrdenServicioAsync(clienteId, usuarioId);
        var insumoId = await CrearArticuloAsync(ArticuloTipo.INSUMO, "Papel DTF");
        await CrearEntradaStockAsync(usuarioId, insumoId, 2);

        var resultado = await CrearHandler().HandleAsync(new RegistrarConsumoInsumos.Command(
            ordenId, usuarioId, null, null, [new RegistrarConsumoInsumos.Linea(insumoId, 3)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("stock", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2m, (await SaldosInventario.ObtenerSaldosAsync(_db))[insumoId]);
    }

    [Fact]
    public async Task RegistrarConsumo_ConOrdenLista_EsRechazado()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var clienteId = await bd.CrearClienteAsync();
        var ordenId = await bd.CrearOrdenServicioAsync(clienteId, usuarioId, EstadoOrdenServicio.LISTO);
        var insumoId = await CrearArticuloAsync(ArticuloTipo.INSUMO, "Papel DTF");
        await CrearEntradaStockAsync(usuarioId, insumoId, 5);

        var resultado = await CrearHandler().HandleAsync(new RegistrarConsumoInsumos.Command(
            ordenId, usuarioId, null, null, [new RegistrarConsumoInsumos.Linea(insumoId, 1)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("orden", StringComparison.OrdinalIgnoreCase));
    }

    private RegistrarConsumoInsumos.Handler CrearHandler() =>
        new(_db, new AccesoPublicApi(_acceso), new PersonalizacionPublicApi(_personalizacion));

    private async Task CrearEntradaStockAsync(int usuarioId, int articuloId, decimal cantidad)
    {
        var movimiento = new MovimientoInventario
        {
            TipoMovimiento = TipoMovimientoInventario.ENTRADA,
            Motivo = MotivoMovimientoInventario.AJUSTE,
            OcurridoEn = DateTimeOffset.UtcNow,
            Notas = "Carga inicial de existencias",
            RegistradoPorUsuarioId = usuarioId,
            Detalles = [new DetalleMovimientoInventario { ArticuloId = articuloId, Cantidad = cantidad }]
        };
        _db.MovimientosInventario.Add(movimiento);
        await _db.SaveChangesAsync();
    }

    private async Task<int> CrearArticuloAsync(ArticuloTipo tipo, string nombre)
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
            CostoUnitario = 4.50m,
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
            _db.Insumos.Add(new Insumo { ArticuloId = articulo.Id, Descripcion = "Insumo de prueba" });
        }
        await _db.SaveChangesAsync();
        return articulo.Id;
    }
}
