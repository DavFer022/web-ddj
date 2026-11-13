using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Tests.Infrastructure;
using SistemaDDJ.Web.Modules.Acceso.Data;
using SistemaDDJ.Web.Modules.Acceso.Features;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;
using SistemaDDJ.Web.Modules.Clientes.Data;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarAjusteOMerma;
using SistemaDDJ.Web.Modules.Personalizacion.Data;
using SistemaDDJ.Web.Modules.Personalizacion.Features;

namespace SistemaDDJ.Tests.Inventario;

[Collection(ColeccionInventario.Nombre)]
public sealed class AjustesYMermasTests(BaseDatosPruebaInventario bd) : IAsyncLifetime
{
    private InventarioDbContext _db = null!;
    private AccesoDbContext _acceso = null!;
    private PersonalizacionDbContext _personalizacion = null!;
    private ClientesDbContext _clientes = null!;

    public async Task InitializeAsync()
    {
        _db = bd.CrearContexto();
        _acceso = bd.CrearContextoAcceso();
        _personalizacion = bd.CrearContextoPersonalizacion();
        _clientes = bd.CrearContextoClientes();
        await bd.LimpiarAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _acceso.DisposeAsync();
        await _personalizacion.DisposeAsync();
        await _clientes.DisposeAsync();
    }

    [Fact]
    public async Task RegistrarAjuste_SinNotas_EsRechazado()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");

        var resultado = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, usuarioId,
            notas: "  ", [new RegistrarAjusteOMerma.Linea(productoId, 1)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("notas", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await _db.MovimientosInventario.CountAsync());
    }

    [Fact]
    public async Task RegistrarAjuste_ConUsuarioSinRolAutorizadoOInactivo_EsRechazado()
    {
        var usuarioSinRol = await bd.CrearUsuarioAsync(rol: null);
        var usuarioInactivo = await bd.CrearUsuarioAsync(activo: false);
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");

        var sinRol = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, usuarioSinRol,
            "Conteo físico", [new RegistrarAjusteOMerma.Linea(productoId, 1)]));
        var inactivo = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, usuarioInactivo,
            "Conteo físico", [new RegistrarAjusteOMerma.Linea(productoId, 1)]));

        Assert.False(sinRol.Exitoso);
        Assert.Contains(sinRol.Errores, e => e.Contains("rol", StringComparison.OrdinalIgnoreCase));
        Assert.False(inactivo.Exitoso);
        Assert.Contains(inactivo.Errores, e => e.Contains("rol", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await _db.MovimientosInventario.CountAsync());
    }

    [Fact]
    public async Task RegistrarAjusteSalida_QueDejariaStockNegativo_EsRechazado()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearStockAsync(usuarioId, productoId, 2);

        var resultado = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.AJUSTE, usuarioId,
            "Faltante de conteo", [new RegistrarAjusteOMerma.Linea(productoId, 3)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("stock", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2m, (await SaldosInventario.ObtenerSaldosAsync(_db))[productoId]);
    }

    [Fact]
    public async Task RegistrarAjusteEntradaYSalida_CuadraElStockCalculado()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");

        var entrada = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, usuarioId,
            "Sobrante de conteo", [new RegistrarAjusteOMerma.Linea(productoId, 10)]));
        var salida = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.AJUSTE, usuarioId,
            "Faltante de conteo", [new RegistrarAjusteOMerma.Linea(productoId, 3)]));

        Assert.True(entrada.Exitoso, string.Join(" ", entrada.Errores));
        Assert.True(salida.Exitoso, string.Join(" ", salida.Errores));
        Assert.Equal(7m, (await SaldosInventario.ObtenerSaldosAsync(_db))[productoId]);
    }

    [Fact]
    public async Task RegistrarMerma_VinculadaAOrdenDeServicio_DescuentaStockSinAlterarTotalesDeLaOrden()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var clienteId = await bd.CrearClienteAsync();
        var ordenId = await bd.CrearOrdenServicioAsync(clienteId, usuarioId, totalCotizado: 100m);
        var insumoId = await CrearArticuloAsync(ArticuloTipo.INSUMO, "Papel DTF");
        await CrearStockAsync(usuarioId, insumoId, 10);

        var resultado = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.MERMA, usuarioId,
            "Prueba de estampado dañada", [new RegistrarAjusteOMerma.Linea(insumoId, 2.5m)],
            ordenServicioId: ordenId));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        var movimiento = await verificacion.MovimientosInventario.SingleAsync(m => m.Id == resultado.MovimientoId);
        Assert.Equal(MotivoMovimientoInventario.MERMA, movimiento.Motivo);
        Assert.Equal(ordenId, movimiento.OrdenServicioId);
        Assert.Equal(7.5m, (await SaldosInventario.ObtenerSaldosAsync(verificacion))[insumoId]);

        var orden = await _personalizacion.OrdenesServicio.SingleAsync(o => o.Id == ordenId);
        Assert.Equal(100m, orden.TotalCotizado);
        Assert.Null(orden.TotalFinal);
        Assert.Empty(await _clientes.MovimientosCuentaCliente.Where(m => m.OrdenServicioId == ordenId).ToListAsync());
    }

    [Fact]
    public async Task RegistrarMerma_ConProducto_EsRechazado()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");
        await CrearStockAsync(usuarioId, productoId, 10);

        var resultado = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.MERMA, usuarioId,
            "Producto dañado", [new RegistrarAjusteOMerma.Linea(productoId, 1)]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("insumo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegistrarAjuste_ConOrdenServicio_EsRechazado()
    {
        var usuarioId = await bd.CrearUsuarioAsync();
        var clienteId = await bd.CrearClienteAsync();
        var ordenId = await bd.CrearOrdenServicioAsync(clienteId, usuarioId);
        var productoId = await CrearArticuloAsync(ArticuloTipo.PRODUCTO, "Franela blanca");

        var resultado = await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, usuarioId,
            "Sobrante", [new RegistrarAjusteOMerma.Linea(productoId, 1)], ordenServicioId: ordenId));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("orden", StringComparison.OrdinalIgnoreCase));
    }

    private RegistrarAjusteOMerma.Handler CrearHandler() =>
        new(_db, new AccesoPublicApi(_acceso), new PersonalizacionPublicApi(_personalizacion));

    private static RegistrarAjusteOMerma.Command Command(
        TipoMovimientoInventario tipo,
        MotivoMovimientoInventario motivo,
        int usuarioId,
        string? notas,
        IReadOnlyList<RegistrarAjusteOMerma.Linea> lineas,
        Guid? ordenServicioId = null) =>
        new(tipo, motivo, notas, usuarioId, ordenServicioId, null, lineas);

    private async Task CrearStockAsync(int usuarioId, int articuloId, decimal cantidad) =>
        await CrearHandler().HandleAsync(Command(
            TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.AJUSTE, usuarioId,
            "Carga inicial de existencias", [new RegistrarAjusteOMerma.Linea(articuloId, cantidad)]));

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
