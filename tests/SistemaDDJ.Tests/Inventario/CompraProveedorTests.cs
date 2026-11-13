using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Tests.Infrastructure;
using SistemaDDJ.Web.Modules.Acceso.Data;
using SistemaDDJ.Web.Modules.Acceso.Features;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarCompraProveedor;

namespace SistemaDDJ.Tests.Inventario;

[Collection(ColeccionInventario.Nombre)]
public sealed class CompraProveedorTests(BaseDatosPruebaInventario bd) : IAsyncLifetime
{
    private InventarioDbContext _db = null!;
    private AccesoDbContext _acceso = null!;

    public async Task InitializeAsync()
    {
        _db = bd.CrearContexto();
        _acceso = bd.CrearContextoAcceso();
        await bd.LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _acceso.DisposeAsync();
    }

    [Fact]
    public async Task RegistrarCompra_MixtaProductosEInsumos_IncrementaStockYGuardaCostos()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var proveedorId = await CrearProveedorAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        var insumoId = await CrearArticuloAsync(ArticuloTipo.INSUMO, "Papel DTF");

        var resultado = await CrearHandler().HandleAsync(new RegistrarCompraProveedor.Command(
            proveedorId,
            usuarioId,
            "FAC-0001",
            "Compra mixta",
            [
                new RegistrarCompraProveedor.Linea(productoId, 10, 4.50m, ActualizarCostoReferencia: true),
                new RegistrarCompraProveedor.Linea(insumoId, 100, 0.30m, ActualizarCostoReferencia: true)
            ]));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        var movimiento = await verificacion.MovimientosInventario
            .Include(m => m.Detalles)
            .SingleAsync(m => m.Id == resultado.MovimientoId);
        Assert.Equal(TipoMovimientoInventario.ENTRADA, movimiento.TipoMovimiento);
        Assert.Equal(MotivoMovimientoInventario.COMPRA, movimiento.Motivo);
        Assert.Equal(proveedorId, movimiento.ProveedorId);
        Assert.Equal(usuarioId, movimiento.RegistradoPorUsuarioId);
        Assert.Equal(2, movimiento.Detalles.Count);
        Assert.Contains(movimiento.Detalles, d => d.ArticuloId == productoId && d.Cantidad == 10 && d.CostoUnitario == 4.50m);
        Assert.Contains(movimiento.Detalles, d => d.ArticuloId == insumoId && d.Cantidad == 100 && d.CostoUnitario == 0.30m);

        var saldos = await SaldosInventario.ObtenerSaldosAsync(verificacion);
        Assert.Equal(10m, saldos[productoId]);
        Assert.Equal(100m, saldos[insumoId]);
    }

    [Fact]
    public async Task RegistrarCompra_ConActualizarCostoReferencia_ActualizaArticuloSinAlterarHistorico()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var proveedorId = await CrearProveedorAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca", costoUnitario: 4.50m);

        var resultado = await CrearHandler().HandleAsync(new RegistrarCompraProveedor.Command(
            proveedorId,
            usuarioId,
            null,
            null,
            [new RegistrarCompraProveedor.Linea(productoId, 5, 6.00m, ActualizarCostoReferencia: true)]));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        Assert.Equal(6.00m, (await verificacion.Articulos.SingleAsync(a => a.Id == productoId)).CostoUnitario);
        var detalle = await verificacion.DetallesMovimientoInventario.SingleAsync();
        Assert.Equal(6.00m, detalle.CostoUnitario);
    }

    [Fact]
    public async Task RegistrarCompra_SinActualizarCostoReferencia_ConservaCostoEnArticulo()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var proveedorId = await CrearProveedorAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca", costoUnitario: 4.50m);

        var resultado = await CrearHandler().HandleAsync(new RegistrarCompraProveedor.Command(
            proveedorId,
            usuarioId,
            null,
            null,
            [new RegistrarCompraProveedor.Linea(productoId, 5, 6.00m, ActualizarCostoReferencia: false)]));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        Assert.Equal(4.50m, (await verificacion.Articulos.SingleAsync(a => a.Id == productoId)).CostoUnitario);
        Assert.Equal(6.00m, (await verificacion.DetallesMovimientoInventario.SingleAsync()).CostoUnitario);
    }

    [Fact]
    public async Task RegistrarCompra_RechazaProveedorInactivoOInexistente()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var proveedorId = await CrearProveedorAsync(activo: false);
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");

        var inactivo = await CrearHandler().HandleAsync(Command(proveedorId, usuarioId, productoId));
        var inexistente = await CrearHandler().HandleAsync(Command(9999, usuarioId, productoId));

        Assert.False(inactivo.Exitoso);
        Assert.Contains(inactivo.Errores, e => e.Contains("proveedor", StringComparison.OrdinalIgnoreCase));
        Assert.False(inexistente.Exitoso);
        Assert.Contains(inexistente.Errores, e => e.Contains("proveedor", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await _db.MovimientosInventario.CountAsync());
    }

    [Fact]
    public async Task RegistrarCompra_RechazaCantidadNoPositivaOCostoNegativo()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var proveedorId = await CrearProveedorAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");

        var cantidad = await CrearHandler().HandleAsync(new RegistrarCompraProveedor.Command(
            proveedorId, usuarioId, null, null,
            [new RegistrarCompraProveedor.Linea(productoId, 0, 4.50m, false)]));
        var costo = await CrearHandler().HandleAsync(new RegistrarCompraProveedor.Command(
            proveedorId, usuarioId, null, null,
            [new RegistrarCompraProveedor.Linea(productoId, 1, -1m, false)]));

        Assert.False(cantidad.Exitoso);
        Assert.False(costo.Exitoso);
        Assert.Equal(0, await _db.MovimientosInventario.CountAsync());
    }

    [Fact]
    public async Task RegistrarCompra_ConArticuloRepetido_SumaCantidadesEnUnaSolaLinea()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var proveedorId = await CrearProveedorAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");

        var resultado = await CrearHandler().HandleAsync(new RegistrarCompraProveedor.Command(
            proveedorId,
            usuarioId,
            null,
            null,
            [
                new RegistrarCompraProveedor.Linea(productoId, 2, 4.50m, false),
                new RegistrarCompraProveedor.Linea(productoId, 3, 4.50m, false)
            ]));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        var detalle = await verificacion.DetallesMovimientoInventario.SingleAsync();
        Assert.Equal(5m, detalle.Cantidad);
    }

    private RegistrarCompraProveedor.Handler CrearHandler() => new(_db, new AccesoPublicApi(_acceso));

    private static RegistrarCompraProveedor.Command Command(int proveedorId, int usuarioId, int articuloId) =>
        new(proveedorId, usuarioId, null, null,
            [new RegistrarCompraProveedor.Linea(articuloId, 1, 4.50m, false)]);

    private async Task<int> CrearProveedorAsync(bool activo = true)
    {
        var proveedor = new Proveedor { RazonSocial = $"Proveedor {Guid.NewGuid():N}", Activo = activo };
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();
        return proveedor.Id;
    }

    private async Task<int> CrearArticuloAsync(ArticuloTipo tipo, string nombre, decimal costoUnitario = 4.50m)
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
            CostoUnitario = costoUnitario,
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
