using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Tests.Infrastructure;
using SistemaDDJ.Web.Modules.Acceso.Data;
using SistemaDDJ.Web.Modules.Acceso.Features;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;
using SistemaDDJ.Web.Modules.Clientes.Data;
using SistemaDDJ.Web.Modules.Clientes.Features;
using SistemaDDJ.Web.Modules.Clientes.PublicApi;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarVentaProductos;

namespace SistemaDDJ.Tests.Inventario;

[Collection(ColeccionInventario.Nombre)]
public sealed class VentaProductosTests(BaseDatosPruebaInventario bd) : IAsyncLifetime
{
    private InventarioDbContext _db = null!;
    private AccesoDbContext _acceso = null!;
    private ClientesDbContext _clientes = null!;

    public async Task InitializeAsync()
    {
        _db = bd.CrearContexto();
        _acceso = bd.CrearContextoAcceso();
        _clientes = bd.CrearContextoClientes();
        await bd.LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _acceso.DisposeAsync();
        await _clientes.DisposeAsync();
    }

    [Fact]
    public async Task RegistrarVenta_DeProductosConStock_DescuentaExistenciasYGuardaPrecioVigente()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca", precioVenta: 12m);
        await CrearEntradaStockAsync(usuarioId, productoId, 10);

        var resultado = await CrearHandler().HandleAsync(new RegistrarVentaProductos.Command(
            usuarioId,
            ClienteId: null,
            EsCredito: false,
            Referencia: "VTA-0001",
            Notas: null,
            [new RegistrarVentaProductos.Linea(productoId, 3)]));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        var movimiento = await verificacion.MovimientosInventario
            .Include(m => m.Detalles)
            .SingleAsync(m => m.Id == resultado.MovimientoId);
        Assert.Equal(TipoMovimientoInventario.SALIDA, movimiento.TipoMovimiento);
        Assert.Equal(MotivoMovimientoInventario.VENTA, movimiento.Motivo);
        var detalle = Assert.Single(movimiento.Detalles);
        Assert.Equal(3m, detalle.Cantidad);
        Assert.Equal(12m, detalle.PrecioUnitario);
        Assert.Equal(7m, (await SaldosInventario.ObtenerSaldosAsync(verificacion))[productoId]);
    }

    [Fact]
    public async Task RegistrarVenta_RechazaArticuloDeSubtipoInsumo()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var insumoId = await CrearArticuloAsync(ArticuloTipo.INSUMO, "Papel DTF");
        await CrearEntradaStockAsync(usuarioId, insumoId, 10);

        var resultado = await CrearHandler().HandleAsync(new RegistrarVentaProductos.Command(
            usuarioId, null, false, null, null,
            [new RegistrarVentaProductos.Linea(insumoId, 1)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("PRODUCTO", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await _db.MovimientosInventario.CountAsync(m => m.Motivo == MotivoMovimientoInventario.VENTA));
    }

    [Fact]
    public async Task RegistrarVenta_RechazaCantidadMayorAlStockDisponible()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearEntradaStockAsync(usuarioId, productoId, 5);

        var resultado = await CrearHandler().HandleAsync(new RegistrarVentaProductos.Command(
            usuarioId, null, false, null, null,
            [new RegistrarVentaProductos.Linea(productoId, 6)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("stock", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(5m, (await SaldosInventario.ObtenerSaldosAsync(_db))[productoId]);
    }

    [Fact]
    public async Task RegistrarVenta_RechazaLineasRepetidasQueSuperanElStockTotal()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearEntradaStockAsync(usuarioId, productoId, 5);

        var resultado = await CrearHandler().HandleAsync(new RegistrarVentaProductos.Command(
            usuarioId, null, false, null, null,
            [
                new RegistrarVentaProductos.Linea(productoId, 3),
                new RegistrarVentaProductos.Linea(productoId, 3)
            ]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("stock", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegistrarVenta_ACreditoSinCliente_EsRechazada()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearEntradaStockAsync(usuarioId, productoId, 5);

        var resultado = await CrearHandler().HandleAsync(new RegistrarVentaProductos.Command(
            usuarioId, ClienteId: null, EsCredito: true, null, null,
            [new RegistrarVentaProductos.Linea(productoId, 1)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("cliente", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegistrarVenta_DejaProveedorYOrdenServicioEnNulo()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var clienteId = await bd.CrearClienteAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearEntradaStockAsync(usuarioId, productoId, 5);

        var resultado = await CrearHandler().HandleAsync(new RegistrarVentaProductos.Command(
            usuarioId, clienteId, EsCredito: false, "VTA-0002", null,
            [new RegistrarVentaProductos.Linea(productoId, 1)]));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        var movimiento = await verificacion.MovimientosInventario.SingleAsync(m => m.Id == resultado.MovimientoId);
        Assert.Null(movimiento.ProveedorId);
        Assert.Null(movimiento.OrdenServicioId);
        Assert.Equal(clienteId, movimiento.ClienteId);
    }

    [Fact]
    public async Task RegistrarVenta_ConClienteInactivo_EsRechazada()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var clienteId = await bd.CrearClienteAsync();
        var cliente = await _clientes.Clientes.SingleAsync(c => c.Id == clienteId);
        cliente.Activo = false;
        await _clientes.SaveChangesAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearEntradaStockAsync(usuarioId, productoId, 5);

        var resultado = await CrearHandler().HandleAsync(new RegistrarVentaProductos.Command(
            usuarioId, clienteId, EsCredito: false, null, null,
            [new RegistrarVentaProductos.Linea(productoId, 1)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("cliente", StringComparison.OrdinalIgnoreCase));
    }

    private RegistrarVentaProductos.Handler CrearHandler() =>
        new(_db, new AccesoPublicApi(_acceso), new ClientesPublicApi(_clientes));

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

    private async Task<int> CrearArticuloAsync(ArticuloTipo tipo, string nombre, decimal precioVenta = 12m)
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
                PrecioVenta = precioVenta
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
