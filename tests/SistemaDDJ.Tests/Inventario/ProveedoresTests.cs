using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Tests.Infrastructure;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ActualizarProveedor;
using SistemaDDJ.Web.Modules.Inventario.Features.CambiarEstadoProveedor;
using SistemaDDJ.Web.Modules.Inventario.Features.ListarProveedores;
using SistemaDDJ.Web.Modules.Inventario.Features.Proveedores;
using SistemaDDJ.Web.Modules.Inventario.Features.RegistrarProveedor;

namespace SistemaDDJ.Tests.Inventario;

[Collection(ColeccionInventario.Nombre)]
public sealed class ProveedoresTests(BaseDatosPruebaInventario bd) : IAsyncLifetime
{
    private InventarioDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _db = bd.CrearContexto();
        await bd.LimpiarAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task RegistrarProveedor_ConVariosTelefonos_GuardaProveedorYTelefonos()
    {
        var resultado = await new RegistrarProveedor.Handler(_db).HandleAsync(
            new RegistrarProveedor.Command(
                "Textiles del Centro C.A.",
                "J-30111222-1",
                "ventas@textiles.example",
                [
                    new TelefonoProveedorDto("0412-5550101", "Ventas", EsPrincipal: true),
                    new TelefonoProveedorDto("0414-5550202", "Administración")
                ]));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        var proveedor = await verificacion.Proveedores
            .Include(p => p.Telefonos)
            .SingleAsync(p => p.Id == resultado.ProveedorId);
        Assert.Equal("Textiles del Centro C.A.", proveedor.RazonSocial);
        Assert.Equal("J-30111222-1", proveedor.IdentificacionFiscal);
        Assert.True(proveedor.Activo);
        Assert.Equal(2, proveedor.Telefonos.Count);
        Assert.Contains(proveedor.Telefonos, t => t.Numero == "0412-5550101" && t.EsPrincipal);
    }

    [Fact]
    public async Task RegistrarProveedor_RechazaIdentificacionFiscalDuplicada()
    {
        var handler = new RegistrarProveedor.Handler(_db);
        await handler.HandleAsync(new RegistrarProveedor.Command("Proveedor uno", "J-12345678-9", null, []));

        var resultado = await handler.HandleAsync(
            new RegistrarProveedor.Command("Proveedor dos", " j-12345678-9 ", null, []));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("fiscal", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, await _db.Proveedores.CountAsync());
    }

    [Fact]
    public async Task RegistrarProveedor_RechazaTelefonosDuplicados()
    {
        var resultado = await new RegistrarProveedor.Handler(_db).HandleAsync(
            new RegistrarProveedor.Command(
                "Proveedor con duplicados",
                null,
                null,
                [
                    new TelefonoProveedorDto("0412-5550101"),
                    new TelefonoProveedorDto(" 0412-5550101 ")
                ]));

        Assert.False(resultado.Exitoso);
        Assert.Contains(resultado.Errores, e => e.Contains("teléfono", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await _db.Proveedores.CountAsync());
    }

    [Fact]
    public async Task ActualizarProveedor_ModificaDatosYTelefonos()
    {
        var registro = await new RegistrarProveedor.Handler(_db).HandleAsync(
            new RegistrarProveedor.Command("Proveedor inicial", "J-11111111-1", "inicial@example.com",
                [new TelefonoProveedorDto("0412-0000001", EsPrincipal: true)]));

        var actualizacion = await new ActualizarProveedor.Handler(_db).HandleAsync(
            new ActualizarProveedor.Command(
                registro.ProveedorId!.Value,
                "Proveedor actualizado",
                "J-22222222-2",
                "nuevo@example.com",
                [new TelefonoProveedorDto("0414-0000002", "Compras", EsPrincipal: true)]));

        Assert.True(actualizacion.Exitoso, string.Join(" ", actualizacion.Errores));
        await using var verificacion = bd.CrearContexto();
        var proveedor = await verificacion.Proveedores.Include(p => p.Telefonos).SingleAsync();
        Assert.Equal("Proveedor actualizado", proveedor.RazonSocial);
        Assert.Equal("J-22222222-2", proveedor.IdentificacionFiscal);
        Assert.Equal("nuevo@example.com", proveedor.Correo);
        var telefono = Assert.Single(proveedor.Telefonos);
        Assert.Equal("0414-0000002", telefono.Numero);
        Assert.Equal("Compras", telefono.Etiqueta);
    }

    [Fact]
    public async Task DesactivarProveedor_ConservaMovimientosDeCompraHistoricos()
    {
        var proveedorId = await CrearProveedorAsync("Proveedor histórico");
        var usuarioId = await bd.CrearUsuarioAsync();
        var movimiento = new MovimientoInventario
        {
            TipoMovimiento = TipoMovimientoInventario.ENTRADA,
            Motivo = MotivoMovimientoInventario.COMPRA,
            OcurridoEn = DateTimeOffset.UtcNow,
            ProveedorId = proveedorId,
            RegistradoPorUsuarioId = usuarioId
        };
        _db.MovimientosInventario.Add(movimiento);
        await _db.SaveChangesAsync();

        var resultado = await new CambiarEstadoProveedor.Handler(_db)
            .HandleAsync(new CambiarEstadoProveedor.Command(proveedorId, Activo: false));

        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        await using var verificacion = bd.CrearContexto();
        Assert.False((await verificacion.Proveedores.SingleAsync(p => p.Id == proveedorId)).Activo);
        var movimientoPersistido = await verificacion.MovimientosInventario.SingleAsync(m => m.Id == movimiento.Id);
        Assert.Equal(proveedorId, movimientoPersistido.ProveedorId);
    }

    [Fact]
    public async Task ListarProveedoresActivos_ExcluyeInactivos()
    {
        var activoId = await CrearProveedorAsync("Proveedor activo");
        var inactivoId = await CrearProveedorAsync("Proveedor inactivo");
        await new CambiarEstadoProveedor.Handler(_db)
            .HandleAsync(new CambiarEstadoProveedor.Command(inactivoId, Activo: false));

        var activos = await new ListarProveedores.Handler(_db)
            .HandleAsync(new ListarProveedores.Consulta(SoloActivos: true));
        var todos = await new ListarProveedores.Handler(_db)
            .HandleAsync(new ListarProveedores.Consulta(SoloActivos: false));

        Assert.Single(activos);
        Assert.Equal(activoId, activos[0].Id);
        Assert.Equal(2, todos.Count);
    }

    private async Task<int> CrearProveedorAsync(string razonSocial)
    {
        var resultado = await new RegistrarProveedor.Handler(_db)
            .HandleAsync(new RegistrarProveedor.Command(razonSocial, null, null, []));
        Assert.True(resultado.Exitoso, string.Join(" ", resultado.Errores));
        return resultado.ProveedorId!.Value;
    }
}
