using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;
using SistemaDDJ.Web.Modules.Clientes.PublicApi;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;

namespace SistemaDDJ.Web.Modules.Inventario.Features.RegistrarVentaProductos;

public static class RegistrarVentaProductos
{
    public sealed record Linea(int ArticuloId, decimal Cantidad);

    public sealed record Command(
        int RegistradoPorUsuarioId,
        Guid? ClienteId,
        bool EsCredito,
        string? Referencia,
        string? Notas,
        IReadOnlyList<Linea> Lineas);

    public sealed record Resultado(Guid? MovimientoId, IReadOnlyList<string> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
    }

    public sealed class Handler(
        InventarioDbContext db,
        IAccesoPublicApi acceso,
        IClientesPublicApi clientes)
    {
        public async Task<Resultado> HandleAsync(Command comando, CancellationToken cancellationToken = default)
        {
            var errores = new List<string>();
            if (comando.Lineas is null || comando.Lineas.Count == 0)
            {
                return new Resultado(null, ["La venta debe tener al menos una línea."]);
            }

            foreach (var linea in comando.Lineas)
            {
                if (linea.Cantidad <= 0) errores.Add("La cantidad de cada línea debe ser mayor que 0.");
            }

            if (!await acceso.EstaActivoAsync(comando.RegistradoPorUsuarioId, cancellationToken))
            {
                errores.Add("El usuario que registra la venta no existe o está inactivo.");
            }

            if (comando.EsCredito && comando.ClienteId is null)
            {
                errores.Add("La venta a crédito exige un cliente.");
            }

            if (comando.ClienteId is not null
                && await clientes.ObtenerActivoAsync(comando.ClienteId.Value, cancellationToken) is null)
            {
                errores.Add("El cliente no existe o está inactivo.");
            }

            var consolidado = comando.Lineas
                .GroupBy(l => l.ArticuloId)
                .Select(g => new { ArticuloId = g.Key, Cantidad = g.Sum(l => l.Cantidad) })
                .ToList();
            var articuloIds = consolidado.Select(l => l.ArticuloId).ToList();

            var articulos = await db.Articulos
                .Where(a => articuloIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken);
            var productos = await db.Productos
                .Where(p => articuloIds.Contains(p.ArticuloId))
                .ToDictionaryAsync(p => p.ArticuloId, cancellationToken);
            var saldos = await SaldosInventario.ObtenerSaldosAsync(db, articuloIds, cancellationToken);

            foreach (var linea in consolidado)
            {
                if (!articulos.TryGetValue(linea.ArticuloId, out var articulo) || !articulo.Activo)
                {
                    errores.Add($"El artículo {linea.ArticuloId} no existe o está inactivo.");
                    continue;
                }

                if (articulo.Tipo != ArticuloTipo.PRODUCTO || !productos.ContainsKey(articulo.Id))
                {
                    errores.Add($"El artículo '{articulo.Nombre}' no es un producto de venta.");
                    continue;
                }

                var disponible = saldos.GetValueOrDefault(articulo.Id);
                if (linea.Cantidad > disponible)
                {
                    errores.Add(
                        $"Stock insuficiente para '{articulo.Nombre}': solicitado {linea.Cantidad:0.###}, disponible {disponible:0.###}.");
                }
            }

            if (errores.Count > 0) return new Resultado(null, errores);

            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            var movimiento = new MovimientoInventario
            {
                TipoMovimiento = TipoMovimientoInventario.SALIDA,
                Motivo = MotivoMovimientoInventario.VENTA,
                OcurridoEn = DateTimeOffset.UtcNow,
                Referencia = string.IsNullOrWhiteSpace(comando.Referencia) ? null : comando.Referencia.Trim(),
                Notas = string.IsNullOrWhiteSpace(comando.Notas) ? null : comando.Notas.Trim(),
                ProveedorId = null,
                ClienteId = comando.ClienteId,
                OrdenServicioId = null,
                RegistradoPorUsuarioId = comando.RegistradoPorUsuarioId,
                Detalles = consolidado.Select(l => new DetalleMovimientoInventario
                {
                    ArticuloId = l.ArticuloId,
                    Cantidad = l.Cantidad,
                    CostoUnitario = null,
                    PrecioUnitario = productos[l.ArticuloId].PrecioVenta
                }).ToList()
            };
            db.MovimientosInventario.Add(movimiento);
            await db.SaveChangesAsync(cancellationToken);

            await transaccion.CommitAsync(cancellationToken);
            return new Resultado(movimiento.Id, []);
        }
    }
}
